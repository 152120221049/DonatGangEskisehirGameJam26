using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class RuleBook : MonoBehaviour, IInteractable
{
    [Header("Book Settings")]
    public TMP_Text textDisplay;
    public float throwForce = 24f; // Increased by 60% (from 15)
    public float lifeTimeAfterThrow = 5f;
    
    [Header("Throw Visuals")]
    public Vector3 throwRotationOffset = new Vector3(0, 0, 0); // Tweaked for the new model
    public Vector3 spinAxis = Vector3.up;
    public float spinSpeed = 720f;

    [Header("Return Visuals")]
    public float returnTime = 1.5f;
    public float returnCurveHeight = 2.5f; // Increased for more dramatic arc
    public float returnSideCurve = 2.0f;  // Increased for more dramatic swerve
    public Vector3 returnRotationOffset = new Vector3(0, 0, 0); // New: Separate offset for the return flight
    public Vector3 returnSpinAxis = new Vector3(1, 0, 0); // Battle Axe Flip (X-axis)
    public float returnSpinSpeed = 1080f;
    
    [Header("Contained Rules")]
    public List<BoardRuleInfo> containedRules = new List<BoardRuleInfo>();

    private Rigidbody rb;
    private BoxCollider col;
    private bool isHeld = false;
    private bool isThrown = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        UpdateTextDisplay();
        
        // Listen to global changes just in case a rule changes while we are holding it
        if (RuleManager.Instance != null)
        {
            RuleManager.Instance.OnRuleStateChanged += HandleGlobalRuleChange;
        }
    }

    private void OnDestroy()
    {
        if (RuleManager.Instance != null)
        {
            RuleManager.Instance.OnRuleStateChanged -= HandleGlobalRuleChange;
        }
    }

    private void HandleGlobalRuleChange(RuleType type, bool isErased)
    {
        UpdateTextDisplay();
    }

    public void Interact(GameObject player)
    {
        if (isHeld || isThrown) return; 

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null && pc.heldBook == null)
        {
            pc.PickUpBook(this, false); // No thud for normal pickup
        }
    }

    private Coroutine destroyCoroutine;

    public void OnPickedUp(Transform equipParent)
    {
        // Cancel the self-destruct timer if caught!
        if (destroyCoroutine != null)
        {
            StopCoroutine(destroyCoroutine);
            destroyCoroutine = null;
        }

        isHeld = true;
        isThrown = false;
        isReturning = false;
        rb.isKinematic = true; 
        col.enabled = false;   
        col.isTrigger = false;

        StopAllCoroutines(); 
        
        transform.SetParent(equipParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        UpdateTextDisplay();
    }

    public void OnDropped()
    {
        isHeld = false;
        isThrown = false;
        isReturning = false;
        transform.SetParent(null);
        
        rb.isKinematic = false;
        rb.useGravity = true; // Make sure gravity is on when dropped
        col.enabled = true;
        col.isTrigger = false;
        
        rb.linearVelocity = Vector3.zero;
    }

    private Coroutine spinCoroutine;

    private Vector3 flightDirection;
    private bool isSpinning = false;

    void Update()
    {
        // Visual frisbee spin in Update - completely separate from physics
        if (isSpinning)
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
    }

    public void Throw(Vector3 direction)
    {
        isHeld = false;
        isThrown = true;
        isSpinning = true;
        flightDirection = direction;
        
        transform.SetParent(null);
        transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        
        rb.isKinematic = false;
        rb.useGravity = false;
        
        // Reset all physics states
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.ResetCenterOfMass();
        rb.ResetInertiaTensor();
        
        rb.constraints = RigidbodyConstraints.FreezeRotation; 
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Trajectory lock and orientation
        transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(throwRotationOffset);

        col.enabled = true;
        col.isTrigger = false;

        // Ignore player colliders
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            Collider[] playerCols = pc.GetComponentsInChildren<Collider>();
            foreach (Collider pCol in playerCols)
                if (pCol != null && pCol != col) Physics.IgnoreCollision(col, pCol, true);
        }

        // TRAJECTORY LOCK: Force the velocity every frame for 0.1s to overpower deflections
        StartCoroutine(TrajectoryLock(direction));
        StartCoroutine(TemporaryCollisionSafety());
        
        if (destroyCoroutine != null) StopCoroutine(destroyCoroutine);
        destroyCoroutine = StartCoroutine(DestroyAfterDelay(lifeTimeAfterThrow));
    }

    private System.Collections.IEnumerator TrajectoryLock(Vector3 dir)
    {
        float elapsed = 0;
        while (elapsed < 0.1f)
        {
            if (rb != null) rb.linearVelocity = dir * throwForce;
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private System.Collections.IEnumerator TemporaryCollisionSafety()
    {
        // This prevents the book from exploding into the stratosphere 
        // if it clips the floor or player for a microsecond on spawn.
        Physics.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true); 
        // Wait 0.1s
        yield return new WaitForSeconds(0.1f);
        Physics.IgnoreLayerCollision(gameObject.layer, gameObject.layer, false);
    }

    private System.Collections.IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isThrown || isReturning) return;
        // Stop spinning, restore full physics, re-enable gravity so it tumbles naturally
        isSpinning = false;
        rb.constraints = RigidbodyConstraints.None;
        rb.useGravity = true;
    }

    public void TriggerReturn()
    {
        if (isReturning || isHeld) return;
        StopAllCoroutines(); 
        StartCoroutine(ReturnSequence(0f));
    }

    private bool isReturning = false;
    private System.Collections.IEnumerator ReturnSequence(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        
        isReturning = true;
        isThrown = true;
        rb.isKinematic = true; 
        col.enabled = true;
        col.isTrigger = true; 

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) yield break;

        Vector3 startPos = transform.position;
        
        // Match the "Return" orientation for the flight back
        Vector3 dirToPlayer = (player.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(dirToPlayer) * Quaternion.Euler(returnRotationOffset);

        // Use inspector-exposed settings
        float startTime = Time.time;
        float curveHeight = Random.Range(returnCurveHeight * 0.5f, returnCurveHeight);
        float sideCurve = Random.Range(-returnSideCurve, returnSideCurve);

        while (true)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < 1.5f) break;

            float fraction = (Time.time - startTime) / returnTime;
            if (fraction > 1f) fraction = 1f;

            Vector3 currentTarget = Vector3.Lerp(startPos, player.transform.position + Vector3.up, fraction);
            
            // Arch upwards
            Vector3 verticalOffset = Vector3.up * Mathf.Sin(fraction * Mathf.PI) * curveHeight;
            
            // Arch to the side
            Vector3 directionToPlayer = (player.transform.position - startPos).normalized;
            Vector3 sideDir = Vector3.Cross(directionToPlayer, Vector3.up).normalized;
            Vector3 horizontalOffset = sideDir * Mathf.Sin(fraction * Mathf.PI) * sideCurve;

            transform.position = currentTarget + verticalOffset + horizontalOffset;
            
            // BATTLE AXE SPIN: Use Local Space (Space.Self) so it flips head-over-heels
            transform.Rotate(returnSpinAxis, returnSpinSpeed * Time.deltaTime, Space.Self);
            
            yield return null;
        }

        isReturning = false;
        isThrown = false;
        col.isTrigger = false;
        
        player.PickUpBook(this, true); 
    }

    public void CopyRulesFromBoard(RuleBoard board)
    {
        if (board == null) return;

        bool addedNew = false;
        foreach (var rule in board.rulesOnThisBoard)
        {
            bool exists = false;
            foreach (var existing in containedRules)
            {
                if (existing.ruleType == rule.ruleType)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                // Only store the type, description comes from RuleManager now
                containedRules.Add(new BoardRuleInfo { ruleType = rule.ruleType });
                addedNew = true;
            }
        }

        if (addedNew) UpdateTextDisplay();
    }

    public void UpdateTextDisplay()
    {
        if (textDisplay == null) return;

        if (containedRules.Count == 0)
        {
            textDisplay.text = "<color=#AAAAAA><i>Empty Rule Book</i></color>";
            return;
        }

        string fullText = "<size=110%><b><color=#FFD700>Book Rules</color></b></size>\n<line-height=110%>";

        for (int i = 0; i < containedRules.Count; i++)
        {
            RuleType type = containedRules[i].ruleType;
            bool isErased = false;

            if (RuleManager.Instance != null)
            {
                isErased = RuleManager.Instance.IsRuleErased(type);
            }

            // Get description from central manager
            string ruleText = "Unknown Rule";
            if (RuleManager.Instance != null)
            {
                ruleText = RuleManager.Instance.GetDescription(type);
            }

            if (isErased)
            {
                fullText += $"<color=#FF5555><b>[{i + 1}]</b> <s>{ruleText}</s></color>\n";
            }
            else
            {
                fullText += $"<color=#55FF55><b>[{i + 1}]</b> {ruleText}</color>\n";
            }
        }

        textDisplay.text = fullText;
    }
}
