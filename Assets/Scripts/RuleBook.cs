using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class RuleBook : MonoBehaviour, IInteractable
{
    [Header("Book Settings")]
    public TMP_Text textDisplay;
    public float throwForce = 15f;
    public float lifeTimeAfterThrow = 5f;
    
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

    public void OnPickedUp(Transform equipParent)
    {
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
        col.enabled = true;
        col.isTrigger = false;
        
        rb.linearVelocity = Vector3.zero;
    }

    public void Throw(Vector3 direction)
    {
        isHeld = false;
        isThrown = true;
        
        transform.SetParent(null);
        rb.isKinematic = false;
        col.enabled = true;
        col.isTrigger = false;
        
        rb.linearVelocity = direction * throwForce;

        // The book will now only return if it hits a RuleBoard or Button 
        // that triggers its TriggerReturn() method.
        // If it doesn't hit anything, it destroys itself after the lifetime.
        Destroy(gameObject, lifeTimeAfterThrow);
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
        float returnSpeed = 15f;
        float curveHeight = Random.Range(0.8f, 1.5f); // Smaller, randomized height
        float sideCurve = Random.Range(-1.5f, 1.5f);   // Randomized horizontal arch
        float journeyTime = Vector3.Distance(startPos, player.transform.position) / returnSpeed;
        float startTime = Time.time;

        while (true)
        {
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < 1.5f) break;

            float fraction = (Time.time - startTime) / journeyTime;
            if (fraction > 1f) fraction = 1f;

            Vector3 currentTarget = Vector3.Lerp(startPos, player.transform.position + Vector3.up, fraction);
            
            // Arch upwards
            Vector3 verticalOffset = Vector3.up * Mathf.Sin(fraction * Mathf.PI) * curveHeight;
            
            // Arch to the side (relative to player-book direction)
            Vector3 directionToPlayer = (player.transform.position - startPos).normalized;
            Vector3 sideDir = Vector3.Cross(directionToPlayer, Vector3.up).normalized;
            Vector3 horizontalOffset = sideDir * Mathf.Sin(fraction * Mathf.PI) * sideCurve;

            transform.position = currentTarget + verticalOffset + horizontalOffset;
            
            transform.Rotate(Vector3.forward, 1080f * Time.deltaTime);
            
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
            if (RuleManager.RuleDescriptions.ContainsKey(type))
            {
                ruleText = RuleManager.RuleDescriptions[type];
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
