using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[System.Serializable]
public class BoardRuleInfo
{
    public RuleType ruleType;
    public string ruleDescription; // e.g., "Mavi renkli yerde karakter kayar"
    public bool startsErased;      // NEW: Should this rule start as erased in this level?
}

public class RuleBoard : MonoBehaviour, IInteractable
{
    [Header("UI")]
    public TMP_Text textDisplay;
    public string boardTitle = "RULES:\n";
    
    [Header("Rules Available Here")]
    public List<BoardRuleInfo> rulesOnThisBoard = new List<BoardRuleInfo>();

    private bool isPlayerInteracting = false;
    private PlayerController interactingPlayer;

    private void Start()
    {
        // 1. Apply any initial erased states defined on this specific board
        if (RuleManager.Instance != null)
        {
            foreach (var rule in rulesOnThisBoard)
            {
                if (rule.startsErased)
                {
                    RuleManager.Instance.EraseRule(rule.ruleType);
                }
            }
        }

        UpdateTextDisplay();

        // 2. Listen for global rule changes so the board updates if another board erases a rule
        if (RuleManager.Instance != null)
        {
            RuleManager.Instance.OnRuleStateChanged += HandleGlobalRuleChange;
        }
    }

    private void OnDestroy()
    {
        if (RuleManager.Instance != null)
        {
            RuleManager.Instance.OnRuleStateChanged -= HandleGlobalRuleChange;
        }
    }

    private void HandleGlobalRuleChange(RuleType type, bool isErased)
    {
        UpdateTextDisplay();
    }

    public void Interact(GameObject player)
    {
        // Toggle interaction state
        if (!isPlayerInteracting)
        {
            interactingPlayer = player.GetComponent<PlayerController>();
            if (interactingPlayer != null)
            {
                isPlayerInteracting = true;
                interactingPlayer.isInteractingWithBoard = true;
                UpdateTextDisplay();
            }
        }
        else
        {
            // Optional: Pressing E again cancels the interaction
            ReleasePlayer();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        RuleBook hitBook = collision.gameObject.GetComponentInParent<RuleBook>();
        if (hitBook != null)
        {
            Debug.Log("[RuleBoard] Hit by a RuleBook! Triggering return.");
            hitBook.TriggerReturn();
        }
    }

    private void Update()
    {
        if (!isPlayerInteracting) return;

        // Check for number key presses (1-9)
        CheckForNumberInput();

        // Allow backing out
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReleasePlayer();
        }
    }

    private void CheckForNumberInput()
    {
        // Array of keys to check 1 through 9
        var keys = new[] {
            Keyboard.current.digit1Key, Keyboard.current.digit2Key, Keyboard.current.digit3Key,
            Keyboard.current.digit4Key, Keyboard.current.digit5Key, Keyboard.current.digit6Key,
            Keyboard.current.digit7Key, Keyboard.current.digit8Key, Keyboard.current.digit9Key
        };

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                int ruleIndex = i;
                if (ruleIndex < rulesOnThisBoard.Count)
                {
                    // 1. Identify the rule
                    RuleType selectedRule = rulesOnThisBoard[ruleIndex].ruleType;

                    // 2. If it's a LOCAL rule (like BookReturn), only change the held book
                    if (selectedRule == RuleType.BookReturn)
                    {
                        if (interactingPlayer != null && interactingPlayer.heldBook != null)
                        {
                            interactingPlayer.heldBook.ToggleRuleLocally(selectedRule);
                        }
                    }
                    else
                    {
                        // 3. Otherwise, change it globally for the whole world
                        RuleManager.Instance.ToggleRule(selectedRule);
                    }
                    
                    // Release the player after making a choice
                    ReleasePlayer();
                }
            }
        }
    }

    private void ReleasePlayer()
    {
        isPlayerInteracting = false;
        if (interactingPlayer != null)
        {
            interactingPlayer.isInteractingWithBoard = false;
            interactingPlayer = null;
        }
        UpdateTextDisplay();
    }

    private void UpdateTextDisplay()
    {
        if (textDisplay == null) return;

        // Rich text for the title (Gold, bold, slightly larger)
        string fullText = $"<size=120%><b><color=#FFD700>{boardTitle}</color></b></size>\n<line-height=120%>";

        if (isPlayerInteracting)
        {
            fullText += "<color=#00FFFF><i>[ Silmek ve Yenilemek için Kural Seç ]</i></color>\n\n";
        }
        else
        {
            fullText += "\n"; // Just some spacing
        }

        for (int i = 0; i < rulesOnThisBoard.Count; i++)
        {
            BoardRuleInfo info = rulesOnThisBoard[i];
            bool isErased = false;

            if (RuleManager.Instance != null)
            {
                // If it's a local rule and someone is looking at the board, check their book
                if (info.ruleType == RuleType.BookReturn && interactingPlayer != null && interactingPlayer.heldBook != null)
                {
                    isErased = interactingPlayer.heldBook.IsRuleErased(info.ruleType);
                }
                else
                {
                    isErased = RuleManager.Instance.IsRuleErased(info.ruleType);
                }
            }

            string ruleText = info.ruleDescription;

            if (isErased)
            {
                // Erased State: Red when interacting, Grey when just looking
                if (isPlayerInteracting)
                {
                    fullText += $"<color=#FF5555><b>[{i + 1}]</b> <s>{ruleText}</s></color>\n";
                }
                else
                {
                    fullText += $"<color=#888888><s>- {ruleText}</s></color>\n";
                }
            }
            else
            {
                // Active State: Green when interacting, White when just looking
                if (isPlayerInteracting)
                {
                    fullText += $"<color=#55FF55><b>[{i + 1}]</b> {ruleText}</color>\n";
                }
                else
                {
                    fullText += $"<color=#FFFFFF>- {ruleText}</color>\n";
                }
            }
        }

        if (isPlayerInteracting)
        {
            fullText += "\n<size=80%><color=#AAAAAA>[ESC] or [E] to Cancel</color></size>";
        }

        textDisplay.text = fullText;
    }
}
