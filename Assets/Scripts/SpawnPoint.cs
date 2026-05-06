using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class SpawnPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Register checkpoint in PlayerController
                player.SetSpawnPoint(transform.position);

                // Check if this is a new spawn point we haven't touched recently
                // We can use a simple static reference or compare positions to avoid spamming reset
                // For a robust system, we assume touching ANY checkpoint resets the rules
                // to their default state for that section.
                if (RuleManager.Instance != null)
                {
                    RuleManager.Instance.ResetAllRules();
                }
            }
        }
    }
}
