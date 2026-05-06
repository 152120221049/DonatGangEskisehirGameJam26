using UnityEngine;

public class RuleEraser : MonoBehaviour, IInteractable
{
    [Tooltip("The GroundRule this object will erase when interacted with.")]
    public GroundRule targetRule;

    public void Interact(GameObject interactor)
    {
        if (targetRule != null)
        {
            if (targetRule.isRuleActive)
            {
                targetRule.EraseRule();
                Debug.Log($"{gameObject.name} erased the rule on {targetRule.gameObject.name}.");
            }
            else
            {
                // Optional: Allow them to rewrite the rule by interacting again
                targetRule.RestoreRule();
                Debug.Log($"{gameObject.name} restored the rule on {targetRule.gameObject.name}.");
            }
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no Target Rule assigned to erase!");
        }
    }
}
