using UnityEngine;

public class BookThrower : MonoBehaviour
{
    [Header("Settings")]
    public RuleBook bookPrefab;
    public Transform spawnPoint;
    public float dispenseForce = 5f;

    /// <summary>
    /// Should be called by a PhysicsButton or other trigger.
    /// </summary>
    /// <param name="hitSource">The object that hit the button.</param>
    public void OnButtonHit(GameObject hitSource)
    {
        Debug.Log($"[BookThrower] Button hit by: {hitSource.name}");
        RuleBook hitBook = hitSource.GetComponentInParent<RuleBook>();

        if (hitBook != null)
        {
            // Scenario: BookReturn rule is ERASED
            if (RuleManager.Instance != null && RuleManager.Instance.IsRuleErased(RuleType.BookReturn))
            {
                Debug.Log("[BookThrower] Rule Erased! Swapping book for a returning dispenser book.");
                Destroy(hitBook.gameObject);
                
                RuleBook newBook = Instantiate(bookPrefab, spawnPoint.position, spawnPoint.rotation);
                newBook.TriggerReturn(); // Make the NEW book fly to the player
            }
            else
            {
                // Scenario: Normal hit, trigger return of the existing book
                Debug.Log("[BookThrower] Normal hit! Triggering return of the thrown book.");
                hitBook.TriggerReturn();
            }
        }
        else
        {
            // Hit by anything else (player, box, etc)
            Debug.Log("[BookThrower] Not a book. Dispensing new book...");
            DispenseNewBook();
        }
    }

    public void DispenseNewBook()
    {
        if (bookPrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("[BookThrower] Prefab or SpawnPoint missing!");
            return;
        }

        RuleBook newBook = Instantiate(bookPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Give it a little nudge forward
        Rigidbody rb = newBook.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(spawnPoint.forward * dispenseForce, ForceMode.Impulse);
        }
        
        Debug.Log("[BookThrower] Dispensed new book.");
    }
}
