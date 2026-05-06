using System;
using System.Collections.Generic;
using UnityEngine;

public class RuleManager : MonoBehaviour
{
    public static RuleManager Instance { get; private set; }

    // Central repository for rule descriptions
    public static readonly Dictionary<RuleType, string> RuleDescriptions = new Dictionary<RuleType, string>
    {
        { RuleType.PassThrough, "Mavi renkli yerlerin içinden geçilir" },
        { RuleType.CanJump, "Karakter zıplayabilir" },
        { RuleType.CanCrouch, "Karakter eğilebilir" },
        { RuleType.CanSlide, "Karakter kayabilir" },
        { RuleType.BookReturn, "Atılan kitap geri döner" },
        { RuleType.HotGround, "Kırmızı zemin can yakar" },
        { RuleType.InvisibleGround, "Zemin görünmezdir" }
    };

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
        if (erasedRules.Contains(rule))
        {
            RestoreRule(rule);
        }
        else
        {
            EraseRule(rule);
        }
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
