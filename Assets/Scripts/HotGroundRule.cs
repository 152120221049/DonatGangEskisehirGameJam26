using UnityEngine;

public class HotGroundRule : GroundRule
{
    [Header("Damage Settings")]
    public float damagePerSecond = 20f;
    public float damageJumpForce = 4f;

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
        if (isRuleActive)
        {
            PlayerHealth health = collision.gameObject.GetComponent<PlayerHealth>();
            if (health != null)
            {
                if (health.TakeDamage(damagePerSecond * Time.deltaTime))
                {
                    // If damage was actually dealt (not invincible), make them jump a little
                    PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
                    if (pc != null) pc.ApplyKnockback(Vector3.up * damageJumpForce);
                }
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
                if (health.TakeDamage(damagePerSecond * Time.deltaTime))
                {
                    PlayerController pc = other.gameObject.GetComponent<PlayerController>();
                    if (pc != null) pc.ApplyKnockback(Vector3.up * damageJumpForce);
                }
            }
        }
    }
}
