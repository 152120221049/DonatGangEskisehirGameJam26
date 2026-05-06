using UnityEngine;

public class HotGroundRule : GroundRule
{
    [Header("Damage Settings")]
    public float damagePerSecond = 20f;

    protected override void ApplyActiveState()
    {
        // Ground is hot (damage active)
        Debug.Log("[HotGroundRule] Surface is now HOT");
    }

    protected override void ApplyErasedState()
    {
        // Ground is safe (damage inactive)
        Debug.Log("[HotGroundRule] Surface is now SAFE");
    }

    private void OnCollisionStay(Collision collision)
    {
        // isRuleActive is managed by the base GroundRule class
        if (isRuleActive)
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isRuleActive)
        {
            PlayerHealth health = other.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
    }
}
