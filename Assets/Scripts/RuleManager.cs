using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RuleDescriptionEntry
{
    public RuleType type;
    [TextArea(2, 5)]
    public string description;
}

public class RuleManager : MonoBehaviour
{
    public static RuleManager Instance { get; private set; }

    [Header("Rule Text Settings")]
    public List<RuleDescriptionEntry> ruleDescriptions = new List<RuleDescriptionEntry>()
    {
        new RuleDescriptionEntry { type = RuleType.IceFriction, description = "Zemin buz gibi kaygandır" },
        new RuleDescriptionEntry { type = RuleType.PassThrough, description = "Mavi renkli yerlerin içinden geçilir" },
        new RuleDescriptionEntry { type = RuleType.CanJump, description = "Karakter zıplayabilir" },
        new RuleDescriptionEntry { type = RuleType.CanCrouch, description = "Karakter eğilebilir" },
        new RuleDescriptionEntry { type = RuleType.CanSlide, description = "Karakter kayabilir" },
        new RuleDescriptionEntry { type = RuleType.BookReturn, description = "Atılan kitap geri döner" },
        new RuleDescriptionEntry { type = RuleType.HotGround, description = "Kırmızı zemin can yakar" },
        new RuleDescriptionEntry { type = RuleType.InvisibleGround, description = "Zemin görünmezdir" }
    };

    // Helper to get description at runtime
    public string GetDescription(RuleType type)
    {
        var entry = ruleDescriptions.Find(e => e.type == type);
        return entry != null ? entry.description : "No description found.";
    }

    private HashSet<RuleType> erasedRules = new HashSet<RuleType>();

    // Event fired whenever a rule's state changes. 
    // Passes the RuleType that changed, and a bool (true = erased, false = active)
    public event Action<RuleType, bool> OnRuleStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Checks if a specific rule is currently erased.
    /// </summary>
    public bool IsRuleErased(RuleType rule)
    {
        return erasedRules.Contains(rule);
    }

    /// <summary>
    /// Toggles the erased state of a rule. If it's active, it becomes erased. If erased, it becomes active.
    /// </summary>
    public void ToggleRule(RuleType rule)
    {
        bool isErasing = !erasedRules.Contains(rule);
        
        if (erasedRules.Contains(rule))
        {
            RestoreRule(rule);
        }
        else
        {
            EraseRule(rule);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayRuleChanged(isErasing);
    }

    public void EraseRule(RuleType rule)
    {
        if (!erasedRules.Contains(rule))
        {
            erasedRules.Add(rule);
            Debug.Log($"[RuleManager] Rule Erased: {rule}");
            OnRuleStateChanged?.Invoke(rule, true);
        }
    }

    public void RestoreRule(RuleType rule)
    {
        if (erasedRules.Contains(rule))
        {
            erasedRules.Remove(rule);
            Debug.Log($"[RuleManager] Rule Restored: {rule}");
            OnRuleStateChanged?.Invoke(rule, false);
        }
    }

    /// <summary>
    /// Restores all rules back to their default active state.
    /// </summary>
    public void ResetAllRules()
    {
        // Create a copy of the list to avoid modifying the collection while iterating
        List<RuleType> rulesToReset = new List<RuleType>(erasedRules);
        
        foreach (RuleType rule in rulesToReset)
        {
            RestoreRule(rule);
        }
        
        erasedRules.Clear();
        Debug.Log("[RuleManager] All rules have been reset to default.");
    }
}
