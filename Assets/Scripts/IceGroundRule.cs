using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IceGroundRule : GroundRule
{
    [Header("Ice Settings")]
    public PhysicsMaterial slipperyMaterial;
    public PhysicsMaterial normalMaterial;
    
    // Optional: Visual change
    public Material slipperyVisual;
    public Material normalVisual;

    private Collider col;
    private MeshRenderer meshRenderer;

    protected override void Start()
    {
        col = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        // Ensure default RuleType is set if forgotten
        if (ruleType == default) ruleType = RuleType.IceFriction;

        base.Start(); // Let the base class handle subscription and initial state
    }

    protected override void ApplyActiveState()
    {
        if (col != null && slipperyMaterial != null)
        {
            col.sharedMaterial = slipperyMaterial;
        }
        if (meshRenderer != null && slipperyVisual != null)
        {
            meshRenderer.material = slipperyVisual;
        }
    }

    protected override void ApplyErasedState()
    {
        if (col != null)
        {
            col.sharedMaterial = normalMaterial; // If normalMaterial is null, this removes the slippery effect
        }
        if (meshRenderer != null && normalVisual != null)
        {
            meshRenderer.material = normalVisual;
        }
    }
}
