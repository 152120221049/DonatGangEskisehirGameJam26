using UnityEngine;

public class InvisibleGroundRule : GroundRule
{
    private MeshRenderer meshRenderer;

    protected override void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Ensure default RuleType
        if (ruleType == default) ruleType = RuleType.InvisibleGround;

        base.Start();
    }

    protected override void ApplyActiveState()
    {
        if (meshRenderer != null)
        {
            // Rule is Active: Ground is invisible
            meshRenderer.enabled = false;
        }
    }

    protected override void ApplyErasedState()
    {
        if (meshRenderer != null)
        {
            // Rule is Erased: Ground becomes visible
            meshRenderer.enabled = true;
        }
    }
}
