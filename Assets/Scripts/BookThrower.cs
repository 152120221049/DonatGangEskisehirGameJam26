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
        if (hitSource == null) return;
        
        RuleBook hitBook = hitSource.GetComponentInParent<RuleBook>();
        bool newBookShouldReturn = true;

        if (hitBook != null)
        {
            // If the book DOESN'T have the rule, or HAS it but it's erased... it's "Broken"
            if (!hitBook.HasRule(RuleType.BookReturn) || hitBook.IsRuleErased(RuleType.BookReturn))
            {
                Debug.Log($"[BookThrower] Sacrifice accepted ({hitSource.name}). Consuming and dispensing chaser.");
                newBookShouldReturn = true;
                
                // Use a tiny delay to let physics finish before destroying
                Destroy(hitBook.gameObject, 0.02f);
            }
            else
            {
                Debug.Log($"[BookThrower] Returning book hit ({hitSource.name}). New book stays.");
                hitBook.TriggerReturn();
                newBookShouldReturn = false;
            }
        }
        else
        {
            Debug.Log($"[BookThrower] Non-book hit ({hitSource.name}). Dispensing default.");
            newBookShouldReturn = true;
        }

        DispenseNewBook(newBookShouldReturn);
    }

    public void DispenseNewBook(bool shouldChasePlayer = false)
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
        
        if (shouldChasePlayer)
        {
            newBook.TriggerReturn();
        }
        
        Debug.Log("[BookThrower] Dispensed new book.");
    }
}
