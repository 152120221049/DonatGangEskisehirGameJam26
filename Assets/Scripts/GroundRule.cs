using UnityEngine;

public abstract class GroundRule : MonoBehaviour
{
    [Tooltip("If true, the rule is currently active.")]
    public bool isRuleActive = true;

    public virtual void EraseRule()
    {
        if (!isRuleActive) return;
        
        isRuleActive = false;
        OnRuleErased();
        Debug.Log($"Rule erased for {gameObject.name}!");
    }

    public virtual void RestoreRule()
    {
        if (isRuleActive) return;
        
        isRuleActive = true;
        OnRuleRestored();
        Debug.Log($"Rule restored for {gameObject.name}!");
    }

    // Called when the rule is removed (e.g. ice stops being slippery)
    protected abstract void OnRuleErased();

    // Called when the rule is reapplied
    protected abstract void OnRuleRestored();
}
