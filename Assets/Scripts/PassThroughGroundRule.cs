using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PassThroughGroundRule : GroundRule
{
    [Header("Visuals (Optional)")]
    public Material solidVisual;
    public Material passThroughVisual;

    private Collider col;
    private MeshRenderer meshRenderer;

    protected override void Start()
    {
        col = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Ensure default RuleType is set if forgotten
        if (ruleType == default) ruleType = RuleType.PassThrough;

        base.Start(); // Subscribes to RuleManager
    }

    protected override void ApplyActiveState()
    {
        if (col != null)
        {
            col.enabled = false; // Player passes through by default
        }
        if (meshRenderer != null && passThroughVisual != null)
        {
            meshRenderer.material = passThroughVisual;
        }
    }

    protected override void ApplyErasedState()
    {
        if (col != null)
        {
            col.enabled = true; // Player can walk on it when rule is erased
        }
        if (meshRenderer != null && solidVisual != null)
        {
            meshRenderer.material = solidVisual;
        }
    }
}
