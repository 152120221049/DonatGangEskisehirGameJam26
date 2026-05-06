using UnityEngine;

public abstract class GroundRule : MonoBehaviour
{
    [Header("Rule Settings")]
    public RuleType ruleType;
    protected bool isRuleActive = true;

    protected virtual void Start()
    {
        // Subscribe to global rule changes
        if (RuleManager.Instance != null)
        {
            RuleManager.Instance.OnRuleStateChanged += HandleRuleStateChanged;
            
            // Sync initial state just in case it was spawned late
            if (RuleManager.Instance.IsRuleErased(ruleType))
            {
                ApplyErasedState();
                isRuleActive = false;
            }
            else
            {
                ApplyActiveState();
                isRuleActive = true;
            }
        }
    }

    protected virtual void OnDestroy()
    {
        if (RuleManager.Instance != null)
        {
            RuleManager.Instance.OnRuleStateChanged -= HandleRuleStateChanged;
        }
    }

    private void HandleRuleStateChanged(RuleType changedRule, bool isErased)
    {
        if (changedRule == ruleType)
        {
            isRuleActive = !isErased;
            if (isErased)
            {
                ApplyErasedState();
            }
            else
            {
                ApplyActiveState();
            }
        }
    }

    // Children must implement what happens when the rule is active or erased
    protected abstract void ApplyActiveState();
    protected abstract void ApplyErasedState();
}
