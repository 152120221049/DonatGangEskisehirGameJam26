using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IceGroundRule : GroundRule
{
    [Tooltip("The physics material when ice is active (slippery).")]
    public PhysicsMaterial slipperyMaterial;
    
    [Tooltip("The physics material when ice is erased (normal friction).")]
    public PhysicsMaterial normalMaterial;

    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
        ApplyMaterial();
    }

    private void ApplyMaterial()
    {
        if (col != null)
        {
            col.material = isRuleActive ? slipperyMaterial : normalMaterial;
        }
    }

    protected override void OnRuleErased()
    {
        ApplyMaterial();
    }

    protected override void OnRuleRestored()
    {
        ApplyMaterial();
    }
}
