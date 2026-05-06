using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sneakSpeed = 2.5f;
    public float slideBoostSpeed = 10f;
    public float jumpForce = 5f;
    
    [Header("Slippery Settings")]
    public float acceleration = 10f;
    public float slipperyAcceleration = 2f;
    public float deceleration = 10f;
    public float slipperyDeceleration = 0.5f;

    [Header("Slide Settings")]
    public float slideDuration = 0.5f;
    public float slideCooldown = 1f;
    private float slideTimer;
    private float slideCooldownTimer;
    private bool isSliding;
    private Vector3 slideDirection;

    [Header("Sneak Settings")]
    public float normalHeight = 2f;
    public float sneakHeight = 1f;
    public float crouchTransitionSpeed = 10f;
    private bool isSneaking;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    private bool isGrounded;
    private bool isOnSlipperySurface;

    [Header("Interaction Settings")]
    public float interactionRadius = 2f;
    public LayerMask interactableMask;
    public Transform cameraTransform; // Assign main camera here for raycasts
    
    [Header("Item Holding (Rule Book)")]
    public Vector3 equipOffset = new Vector3(0.7f, -0.7f, 0.4f); // Tucked further right and back
    public Vector3 aimOffset = new Vector3(0f, -0.1f, 1.2f); // Further away and centered
    public Vector3 bookRotationOffset = new Vector3(0, 0, 0); // Change to (0,180,0) if text is backwards
    
    [Header("Book Idle Animation")]
    public Vector3 bookIdleTilt = new Vector3(0f, 90f, -20f); // Tucked but pointing outwards
    public float bobSpeed = 3f;
    public float bobAmount = 0.05f;
    
    [Header("Head Bob Settings")]
    public bool enableHeadBob = true;
    public float headBobFrequency = 1.5f;
    public float headBobAmplitude = 0.1f;
    private float headBobTimer;
    private float defaultCameraY;
    
    public float aimTransitionSpeed = 10f;
    public RuleBook heldBook;
    
    // State to lock movement when using a RuleBoard
    public bool isInteractingWithBoard = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Components
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    // Respawn
    private Vector3 currentSpawnPoint;

    // Input state
    private float horizontalInput;
    private float verticalInput;

    // Slowdown state
    private float slowdownTimer;
    private float currentSlowdownMultiplier = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        rb.freezeRotation = true;
        currentSpawnPoint = transform.position;

        capsuleCollider.height = normalHeight;
        capsuleCollider.center = Vector3.zero;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main != null ? Camera.main.transform : transform;
        }

        if (cameraTransform != null)
        {
            defaultCameraY = cameraTransform.localPosition.y;
        }
    }

    void Update()
    {
        if (Keyboard.current == null || Mouse.current == null) return;

        if (slowdownTimer > 0)
        {
            slowdownTimer -= Time.deltaTime;
            if (slowdownTimer <= 0) currentSlowdownMultiplier = 1f;
        }

        if (isInteractingWithBoard)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            return;
        }

        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) moveInput.y += 1;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1;
        
        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;

        CheckGrounded();
        HandleJump();
        HandleSneak();
        HandleSlide();
        HandleInteraction();
        HandleHeldBook();
        HandleHeadBob();
        
        UpdateColliderHeight();
    }

    public void ApplySlowdown(float duration, float multiplier)
    {
        slowdownTimer = duration;
        currentSlowdownMultiplier = multiplier;
    }

    void FixedUpdate()
    {
        if (!isInteractingWithBoard)
        {
            MovePlayer();
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null) return;

        // Calculate horizontal speed to determine if we are moving
        float speed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

        // Only bob when moving on the ground, not sliding, and not interacting with a board
        if (isGrounded && speed > 0.1f && !isSliding && !isOnSlipperySurface)
        {
            // Increase frequency slightly when sneaking so the steps feel quicker
            float currentFreq = isSneaking ? headBobFrequency * 1.5f : headBobFrequency;
            
            headBobTimer += Time.deltaTime * (speed / walkSpeed) * currentFreq;
            float newY = defaultCameraY + Mathf.Sin(headBobTimer * Mathf.PI * 2f) * headBobAmplitude;
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, newY, cameraTransform.localPosition.z);
        }
        else
        {
            // Smoothly return the camera to default height when stopping
            headBobTimer = 0f;
            float newY = Mathf.Lerp(cameraTransform.localPosition.y, defaultCameraY, Time.deltaTime * 5f);
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, newY, cameraTransform.localPosition.z);
        }
    }

    private void CheckGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
            
            isOnSlipperySurface = false;
            Collider[] colliders = Physics.OverlapSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);
            foreach (var col in colliders)
            {
                if (col.sharedMaterial != null && (col.sharedMaterial.staticFriction < 0.1f || col.sharedMaterial.dynamicFriction < 0.1f))
                {
                    isOnSlipperySurface = true;
                    break;
                }
            }
        }
        else
        {
            Vector3 spherePos = transform.position + (Vector3.up * capsuleCollider.center.y) + Vector3.down * (capsuleCollider.height * 0.5f - 0.1f);
            isGrounded = Physics.CheckSphere(spherePos, capsuleCollider.radius, groundMask, QueryTriggerInteraction.Ignore);
            isOnSlipperySurface = false;
        }
    }

    private void MovePlayer()
    {
        float currentSpeed = walkSpeed * currentSlowdownMultiplier;
        if (isSneaking) currentSpeed = sneakSpeed * currentSlowdownMultiplier;

        Vector3 moveDirection = (transform.right * horizontalInput + transform.forward * verticalInput).normalized;

        if (isSliding)
        {
            Vector3 targetVelocity = slideDirection * slideBoostSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
        else if (isOnSlipperySurface)
        {
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 currentHorizontalVel = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            if (currentHorizontalVel.magnitude > 0.1f)
            {
                // FORCE velocity to be constant so they NEVER stop sliding until they hit a wall
                Vector3 slideDir = currentHorizontalVel.normalized;
                Vector3 constantSlideVelocity = slideDir * currentSpeed;
                rb.linearVelocity = new Vector3(constantSlideVelocity.x, currentVelocity.y, constantSlideVelocity.z);
            }
            else if (moveDirection.magnitude > 0.1f)
            {
                // We are completely stopped. Start sliding in the pressed direction
                Vector3 pushVelocity = moveDirection * currentSpeed;
                rb.linearVelocity = new Vector3(pushVelocity.x, currentVelocity.y, pushVelocity.z);
            }
            else
            {
                rb.linearVelocity = new Vector3(0, currentVelocity.y, 0);
            }
        }
        else
        {
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 targetVelocity = moveDirection * currentSpeed;

            float currentAccel = (moveDirection.magnitude > 0.1f) ? acceleration : deceleration;
            Vector3 newVelocity = Vector3.Lerp(currentVelocity, new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z), currentAccel * Time.fixedDeltaTime);
            
            rb.linearVelocity = new Vector3(newVelocity.x, currentVelocity.y, newVelocity.z);
        }
    }

    private void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (RuleManager.Instance != null && RuleManager.Instance.IsRuleErased(RuleType.CanJump))
            {
                return;
            }

            if (!isGrounded) return;
            if (isSliding) return;
            
            if (CanStandUp())
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                
                if (isSneaking && !Keyboard.current.leftCtrlKey.isPressed)
                {
                    isSneaking = false;
                }
            }
        }
    }

    private void HandleSneak()
    {
        if (isSliding) return;

        bool ruleAllowsCrouch = true;
        if (RuleManager.Instance != null && RuleManager.Instance.IsRuleErased(RuleType.CanCrouch))
        {
            ruleAllowsCrouch = false;
        }

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            if (ruleAllowsCrouch)
            {
                isSneaking = true;
            }
        }
        
        bool wantsToStopSneaking = !Keyboard.current.leftCtrlKey.isPressed;
        if (isSneaking && (wantsToStopSneaking || !ruleAllowsCrouch))
        {
            if (CanStandUp())
            {
                isSneaking = false;
            }
        }
    }

    private void HandleSlide()
    {
        if (slideCooldownTimer > 0)
        {
            slideCooldownTimer -= Time.deltaTime;
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                isSliding = false;
                if (Keyboard.current.leftCtrlKey.isPressed || !CanStandUp())
                {
                    isSneaking = true;
                }
                else
                {
                    isSneaking = false;
                }
            }
        }
        else if (Keyboard.current.leftShiftKey.wasPressedThisFrame && isGrounded && slideCooldownTimer <= 0)
        {
            if (RuleManager.Instance != null && RuleManager.Instance.IsRuleErased(RuleType.CanSlide))
            {
                return;
            }

            isSliding = true;
            slideTimer = slideDuration;
            slideCooldownTimer = slideCooldown;
            
            slideDirection = (transform.right * horizontalInput + transform.forward * verticalInput).normalized;
            if (slideDirection == Vector3.zero) slideDirection = transform.forward;
        }
    }

    private void UpdateColliderHeight()
    {
        float targetHeight = (isSneaking || isSliding) ? sneakHeight : normalHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        
        float newCenterY = (capsuleCollider.height - normalHeight) / 2f;
        capsuleCollider.center = new Vector3(0, newCenterY, 0);
    }

    private bool CanStandUp()
    {
        float radius = capsuleCollider.radius * 0.8f; 
        Vector3 headPosition = transform.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height * 0.5f - radius);
        float distanceToCheck = normalHeight - capsuleCollider.height;
        
        if (distanceToCheck <= 0.1f) return true; 
        
        return !Physics.SphereCast(headPosition, radius, Vector3.up, out RaycastHit hit, distanceToCheck, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleInteraction()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            bool interacted = false;
            
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactionRadius, interactableMask))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact(this.gameObject);
                    interacted = true;
                }
            }
            else
            {
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableMask);
                foreach (var col in hitColliders)
                {
                    IInteractable interactable = col.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        interactable.Interact(this.gameObject);
                        interacted = true;
                        break;
                    }
                }
            }

            if (!interacted && heldBook != null)
            {
                DropBook();
            }
        }
    }

    private void HandleHeldBook()
    {
        if (heldBook == null) return;

        // 1. Aiming / Reading the book
        Vector3 targetLocalPosition = equipOffset;
        Vector3 targetRotationOffset = bookRotationOffset + bookIdleTilt;

        if (Mouse.current.leftButton.isPressed)
        {
            targetLocalPosition = aimOffset;
            targetRotationOffset = bookRotationOffset; // Straighten out to read
        }
        else
        {
            // Apply idle bob
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            targetLocalPosition.y += bob;
        }

        Vector3 targetWorldPosition = cameraTransform.TransformPoint(targetLocalPosition);
        Quaternion targetWorldRotation = cameraTransform.rotation * Quaternion.Euler(0, 180, 0) * Quaternion.Euler(targetRotationOffset);

        heldBook.transform.position = Vector3.Lerp(heldBook.transform.position, targetWorldPosition, Time.deltaTime * aimTransitionSpeed);
        heldBook.transform.rotation = Quaternion.Lerp(heldBook.transform.rotation, targetWorldRotation, Time.deltaTime * aimTransitionSpeed);

        // 2. Left Click to Copy Rules from Board
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hitInfo, interactionRadius * 2f, interactableMask))
            {
                RuleBoard board = hitInfo.collider.GetComponent<RuleBoard>();
                if (board != null)
                {
                    heldBook.CopyRulesFromBoard(board);
                }
            }
        }

        // 3. Numbers 1-9 to erase rule AND throw the book
        var keys = new[] {
            Keyboard.current.digit1Key, Keyboard.current.digit2Key, Keyboard.current.digit3Key,
            Keyboard.current.digit4Key, Keyboard.current.digit5Key, Keyboard.current.digit6Key,
            Keyboard.current.digit7Key, Keyboard.current.digit8Key, Keyboard.current.digit9Key
        };

        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i].wasPressedThisFrame)
            {
                if (i < heldBook.containedRules.Count)
                {
                    RuleType ruleToErase = heldBook.containedRules[i].ruleType;
                    
                    if (RuleManager.Instance != null)
                    {
                        RuleManager.Instance.ToggleRule(ruleToErase);
                    }

                    RuleBook bookToThrow = heldBook;
                    heldBook = null; 
                    bookToThrow.Throw(cameraTransform.forward);
                    break; 
                }
            }
        }
    }

    public void PickUpBook(RuleBook book, bool wasReturned = false)
    {
        if (heldBook != null) DropBook();
        
        heldBook = book;
        heldBook.OnPickedUp(cameraTransform);

        if (wasReturned)
        {
            Debug.Log("[PlayerController] THUD! Returning book caught.");
            TriggerScreenShake(0.15f, 0.15f);
        }
    }

    public void TriggerScreenShake(float duration, float magnitude)
    {
        StartCoroutine(ScreenShake(duration, magnitude));
    }

    private System.Collections.IEnumerator ScreenShake(float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = new Vector3(x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }

    public void DropBook()
    {
        if (heldBook != null)
        {
            heldBook.OnDropped();
            heldBook = null;
        }
    }

    public void SetSpawnPoint(Vector3 spawnPosition)
    {
        currentSpawnPoint = spawnPosition;
    }

    public void Die()
    {
        rb.linearVelocity = Vector3.zero;
        transform.position = currentSpawnPoint;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);

        if (capsuleCollider != null)
        {
            Gizmos.color = isGrounded ? (isOnSlipperySurface ? Color.cyan : Color.green) : Color.red;
            if (groundCheck != null) Gizmos.DrawWireSphere(groundCheck.position, groundDistance);
        }
    }
}
