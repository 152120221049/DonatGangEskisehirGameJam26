using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PassThroughGroundRule : GroundRule
{
    private Collider col;
    private MeshRenderer meshRenderer;

    [Tooltip("If true, the object will also become semi-transparent when the rule is erased.")]
    public bool makeTransparent = true;
    public Material transparentMaterial;
    private Material originalMaterial;

    private void Awake()
    {
        col = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (meshRenderer != null)
        {
            originalMaterial = meshRenderer.material;
        }

        UpdateState();
    }

    private void UpdateState()
    {
        if (col != null)
        {
            // When rule is active, ground is solid (collider enabled). When erased, it is disabled so you fall through.
            col.enabled = isRuleActive;
        }

        if (makeTransparent && meshRenderer != null && transparentMaterial != null)
        {
            meshRenderer.material = isRuleActive ? originalMaterial : transparentMaterial;
        }
    }

    protected override void OnRuleErased()
    {
        UpdateState();
    }

    protected override void OnRuleRestored()
    {
        UpdateState();
    }
}
