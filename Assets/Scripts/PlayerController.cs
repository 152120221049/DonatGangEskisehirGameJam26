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
    
    [Header("Gravity Settings")]
    public float customGravity = -20f; // Stronger default gravity for snappier movement
    
    [Header("Slippery Settings")]
    public float acceleration = 10f;
    public float slipperyAcceleration = 2f;
    public float deceleration = 10f;
    public float slipperyDeceleration = 0.5f;

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
    public Vector3 bookRotationOffset = new Vector3(0, 0, 0); 
    public Vector3 readRotationOffset = new Vector3(90, 0, 0); // Customizable rotation for reading mode
    
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
    
    [Header("Fall & Death Effects")]
    public float fallImpactDipAmount = 0.3f;
    public float fallImpactThreshold = 8f;
    public float deathTiltDuration = 1.5f;
    private float maxFallVelocity;
    private bool isDead;
    
    public float aimTransitionSpeed = 10f;
    public RuleBook heldBook;
    
    // State to lock movement when using a RuleBoard
    public bool isInteractingWithBoard = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    [Header("Audio")]
    public AudioSource playerAudioSource;
    public AudioClip footstepClip;
    public AudioClip jumpClip;
    public AudioClip landClip;
    public float footstepRate = 0.5f;
    private float footstepTimer;
    private bool wasGroundedLastFrame;

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

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    private float xRotation = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        rb.freezeRotation = true;
        rb.useGravity = false; // Using our custom gravity instead
        currentSpawnPoint = transform.position;

        if (playerAudioSource == null) playerAudioSource = GetComponent<AudioSource>();
        if (playerAudioSource == null) playerAudioSource = gameObject.AddComponent<AudioSource>();
        
        wasGroundedLastFrame = isGrounded;

        // Load sensitivity from settings
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", mouseSensitivity);

        // Safety: Ensure game is not paused when entering a new scene
        Time.timeScale = 1f;
        PauseMenuManager.isPaused = false;

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

        // Lock cursor by default during gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (PauseMenuManager.isPaused) return;
        if (isDead) return; // Block input while "dying"

        CheckGrounded();

        if (slowdownTimer > 0)
        {
            slowdownTimer -= Time.deltaTime;
            if (slowdownTimer <= 0) currentSlowdownMultiplier = 1f;
        }

        // Try to get New Input System devices
        var keyboard = Keyboard.current ?? InputSystem.GetDevice<Keyboard>();
        var mouse = Mouse.current ?? InputSystem.GetDevice<Mouse>();

        Vector2 moveInput = Vector2.zero;
        Vector2 lookDelta = Vector2.zero;

        // --- NEW INPUT SYSTEM PATH ---
        if (keyboard != null && mouse != null)
        {
            if (keyboard.wKey.isPressed) moveInput.y += 1;
            if (keyboard.sKey.isPressed) moveInput.y -= 1;
            if (keyboard.aKey.isPressed) moveInput.x -= 1;
            if (keyboard.dKey.isPressed) moveInput.x += 1;

            lookDelta = mouse.delta.ReadValue();
            
            // Call specific methods with new system
            HandleJump(keyboard);
            HandleSneak(keyboard);
            HandleInteraction(keyboard);
            HandleHeldBook(mouse, keyboard);
        }
        // --- OLD INPUT SYSTEM FALLBACK ---
        else
        {
            try 
            {
                moveInput.x = Input.GetAxisRaw("Horizontal");
                moveInput.y = Input.GetAxisRaw("Vertical");
                lookDelta = new Vector2(Input.GetAxisRaw("Mouse X") * 10f, Input.GetAxisRaw("Mouse Y") * 10f);

                if (Input.GetButtonDown("Jump")) rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                // Simplified fallback for other actions...
            }
            catch { /* If in 'New Only' mode, this will fail silently */ }
        }

        if (isInteractingWithBoard)
        {
            if (showDebugLogs) Debug.Log("[PlayerController] Input blocked: Interacting with Board");
            horizontalInput = 0f;
            verticalInput = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Apply Look
        HandleLookManual(lookDelta);

        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;

        CheckGrounded();
        HandleHeadBob();
        UpdateColliderHeight();
    }

    private void HandleLookManual(Vector2 delta)
    {
        float sensitivityMod = mouseSensitivity * 0.1f;
        transform.Rotate(Vector3.up * delta.x * sensitivityMod);

        xRotation -= delta.y * sensitivityMod;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    public void ApplySlowdown(float duration, float multiplier)
    {
        slowdownTimer = duration;
        currentSlowdownMultiplier = multiplier;
    }

    public void ApplyKnockback(Vector3 force)
    {
        // Cancel vertical velocity to ensure the jump force is consistent
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        rb.AddForce(force, ForceMode.Impulse);
    }

    void FixedUpdate()
    {
        if (!isInteractingWithBoard)
        {
            MovePlayer();

            // Landing sound
            if (isGrounded && !wasGroundedLastFrame)
            {
                if (landClip != null) playerAudioSource.PlayOneShot(landClip, 0.5f);
            }

            // Footstep logic
            float horizontalSpeed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
            if (isGrounded && horizontalSpeed > 0.5f && !isOnSlipperySurface)
            {
                footstepTimer -= Time.fixedDeltaTime * (horizontalSpeed / walkSpeed);
                if (footstepTimer <= 0)
                {
                    if (footstepClip != null) 
                    {
                        playerAudioSource.clip = footstepClip;
                        playerAudioSource.volume = 0.3f;
                        playerAudioSource.Play();
                    }
                    footstepTimer = footstepRate;
                }
            }
            else
            {
                // Instantly cut off the footstep sound if we stop moving
                if (playerAudioSource.clip == footstepClip && playerAudioSource.isPlaying)
                {
                    playerAudioSource.Stop();
                    playerAudioSource.clip = null; 
                }
            }
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }

        // Apply custom gravity EVERY frame to prevent floating/rising bugs
        rb.AddForce(Vector3.up * customGravity, ForceMode.Acceleration);
    }

    private void HandleHeadBob()
    {
        if (!enableHeadBob || cameraTransform == null) return;

        // Calculate horizontal speed to determine if we are moving
        float speed = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;

        // Only bob when moving on the ground and not interacting with a board
        if (isGrounded && speed > 0.1f && !isOnSlipperySurface)
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
        wasGroundedLastFrame = isGrounded;
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

        // --- NEW: Fall Impact Detection ---
        if (!isGrounded)
        {
            // Record the highest downward speed during the fall
            if (rb.linearVelocity.y < maxFallVelocity) maxFallVelocity = rb.linearVelocity.y;
        }
        else if (isGrounded && !wasGroundedLastFrame)
        {
            // Just landed. If we were falling fast enough, dip the camera
            if (Mathf.Abs(maxFallVelocity) > fallImpactThreshold)
            {
                StartCoroutine(FallImpactEffect());
            }
            maxFallVelocity = 0;
        }
    }

    private System.Collections.IEnumerator FallImpactEffect()
    {
        float elapsed = 0;
        float duration = 0.3f;
        while (elapsed < duration)
        {
            float dip = Mathf.Sin((elapsed / duration) * Mathf.PI) * fallImpactDipAmount;
            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, defaultCameraY - dip, cameraTransform.localPosition.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, defaultCameraY, cameraTransform.localPosition.z);
    }

    private void MovePlayer()
    {
        float currentSpeed = walkSpeed * currentSlowdownMultiplier;
        if (isSneaking) currentSpeed = sneakSpeed * currentSlowdownMultiplier;

        Vector3 moveDirection = (transform.right * horizontalInput + transform.forward * verticalInput).normalized;

        if (isOnSlipperySurface)
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

    private void HandleJump(Keyboard keyboard)
    {
        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            if (RuleManager.Instance != null && RuleManager.Instance.IsRuleErased(RuleType.CanJump))
            {
                return;
            }

            if (!isGrounded) return;
            if (isOnSlipperySurface) return; // Can't jump on ice!
            
            if (CanStandUp())
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                
                if (jumpClip != null) playerAudioSource.PlayOneShot(jumpClip, 0.6f);
                
                if (isSneaking && !Keyboard.current.leftCtrlKey.isPressed)
                {
                    isSneaking = false;
                }
            }
        }
    }

    private void HandleSneak(Keyboard keyboard)
    {

        bool ruleAllowsCrouch = true;
        if (RuleManager.Instance != null && RuleManager.Instance.IsRuleErased(RuleType.CanCrouch))
        {
            ruleAllowsCrouch = false;
        }

        if (keyboard.leftCtrlKey.wasPressedThisFrame)
        {
            if (ruleAllowsCrouch)
            {
                isSneaking = true;
            }
        }
        
        bool wantsToStopSneaking = !keyboard.leftCtrlKey.isPressed;
        if (isSneaking && (wantsToStopSneaking || !ruleAllowsCrouch))
        {
            if (CanStandUp())
            {
                isSneaking = false;
            }
        }
    }



    private void UpdateColliderHeight()
    {
        float targetHeight = isSneaking ? sneakHeight : normalHeight;
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

    private void HandleInteraction(Keyboard keyboard)
    {
        if (keyboard.eKey.wasPressedThisFrame)
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

    private void HandleHeldBook(Mouse mouse, Keyboard keyboard)
    {
        if (heldBook == null) return;

        // 1. Aiming / Reading the book (Right click usually, but keeping previous logic)
        Vector3 targetLocalPosition = equipOffset;
        Vector3 targetRotationOffset = bookRotationOffset + bookIdleTilt;

        // Using Right Click for Aiming as it's more standard, but user previously used Left Click for interaction
        bool isAiming = mouse.rightButton.isPressed; 

        if (isAiming)
        {
            targetLocalPosition = aimOffset;
            targetRotationOffset = readRotationOffset; // Use the specific reading rotation
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
        if (mouse.leftButton.wasPressedThisFrame)
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
            keyboard.digit1Key, keyboard.digit2Key, keyboard.digit3Key,
            keyboard.digit4Key, keyboard.digit5Key, keyboard.digit6Key,
            keyboard.digit7Key, keyboard.digit8Key, keyboard.digit9Key
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
                        // IF IT'S LOCAL: Erase it only on THIS book
                        if (ruleToErase == RuleType.BookReturn)
                        {
                            heldBook.EraseRuleLocally(ruleToErase);
                        }
                        else
                        {
                            // OTHERWISE: Erase it globally for the world
                            RuleManager.Instance.EraseRule(ruleToErase);
                        }
                    }

                    RuleBook bookToThrow = heldBook;
                    heldBook = null;

                    Transform camT = cameraTransform != null ? cameraTransform : Camera.main.transform;
                    
                    // 1. Raycast against EVERYTHING (~0) to ensure we hit walls, props, etc.
                    // We ignore triggers and the Player layer (usually layer 3 or 6, but we'll use a mask)
                    int layerMask = ~LayerMask.GetMask("Player", "Ignore Raycast"); 
                    
                    Vector3 targetPoint;
                    Ray ray = new Ray(camT.position, camT.forward);
                    
                    if (Physics.Raycast(ray, out RaycastHit hit, 200f, layerMask, QueryTriggerInteraction.Ignore))
                    {
                        targetPoint = hit.point;
                    }
                    else
                    {
                        targetPoint = camT.position + camT.forward * 50f;
                    }

                    // 2. Calculate the spawn and direction
                    Vector3 spawnPos = camT.TransformPoint(new Vector3(0f, 0f, 0.4f));
                    bookToThrow.transform.position = spawnPos;
                    bookToThrow.transform.rotation = camT.rotation;

                    Vector3 throwDir = (targetPoint - spawnPos).normalized;

                    // 3. EMERGENCY FIX: If the throw direction is pointing behind the player, flip it.
                    if (Vector3.Dot(throwDir, camT.forward) < 0) 
                    {
                        throwDir = camT.forward;
                        Debug.LogWarning("[Throw] Detected inverted camera! Reverting to raw forward.");
                    }

                    // VISUAL TARGET TEST: Spawn a temporary sphere where the book is aimed
                    GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    debugSphere.transform.position = targetPoint;
                    debugSphere.transform.localScale = Vector3.one * 0.1f;
                    Destroy(debugSphere.GetComponent<Collider>()); // No physics on debug
                    Destroy(debugSphere, 1f); // Disappear after 1s

                    Debug.DrawLine(spawnPos, targetPoint, Color.red, 5f);
                    Debug.Log($"[Throw] Hit: {(hit.collider != null ? hit.collider.name : "Sky")}. Target: {targetPoint}");

                    bookToThrow.Throw(throwDir);
                    
                    break;
                }
            }
        }
    }

    public void PickUpBook(RuleBook book, bool wasReturned = false)
    {
        // If it's a returning book but we already have one, tell it to drop
        if (wasReturned && heldBook != null)
        {
            book.OnDropped(); 
            return;
        }

        if (heldBook != null) DropBook();
        
        heldBook = book;
        heldBook.OnPickedUp(cameraTransform, wasReturned);

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

    public void Die(bool instant = false)
    {
        if (isDead) return;
        if (instant)
        {
            // Just respawn immediately for kill zones
            rb.linearVelocity = Vector3.zero;
            transform.position = currentSpawnPoint;
        }
        else
        {
            StartCoroutine(DeathSequence());
        }
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        isDead = true;
        float elapsed = 0;
        float duration = deathTiltDuration;
        
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, 70f); // Tilt 70 degrees on Z

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Respawn
        rb.linearVelocity = Vector3.zero;
        transform.position = currentSpawnPoint;
        transform.rotation = startRot; // Reset rotation
        isDead = false;
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

    private void OnGUI()
    {
        // Simple Crosshair
        int size = 24; // Increased size for easier aiming
        int thickness = 4; // Increased thickness
        float xMin = (Screen.width / 2) - (size / 2);
        float yMin = (Screen.height / 2) - (size / 2);

        // Check if looking at something interactable to change crosshair color
        bool isInteractable = false;
        if (cameraTransform != null)
        {
            // Only turn yellow if within actual interaction range
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, interactionRadius, interactableMask))
            {
                // Only shine yellow if the object actually has an interaction script
                if (hit.collider.GetComponent<IInteractable>() != null)
                {
                    isInteractable = true; 
                }
            }
        }

        GUI.color = Color.white;
        
        // Draw Outlined Crosshair
        // Draw black background (outline)
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(xMin - 1, (Screen.height / 2) - (thickness / 2) - 1, size + 2, thickness + 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect((Screen.width / 2) - (thickness / 2) - 1, yMin - 1, thickness + 2, size + 2), Texture2D.whiteTexture);
        
        // Draw white foreground
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(xMin, (Screen.height / 2) - (thickness / 2), size, thickness), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect((Screen.width / 2) - (thickness / 2), yMin, thickness, size), Texture2D.whiteTexture);
        
        // Show Interaction Text with Shadow/Outline
        if (isInteractable)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = FontStyle.Bold;
            style.fontSize = 20;
            
            Rect labelRect = new Rect(Screen.width / 2 + 30, Screen.height / 2 - 25, 300, 50);
            
            // Draw Black Outline
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(labelRect.x + 2, labelRect.y + 2, labelRect.width, labelRect.height), "E ile etkileşime geç", style);
            GUI.Label(new Rect(labelRect.x - 2, labelRect.y - 2, labelRect.width, labelRect.height), "E ile etkileşime geç", style);
            
            // Draw White Text
            style.normal.textColor = Color.white;
            GUI.Label(labelRect, "E ile etkileşime geç", style);
        }

        GUI.color = Color.white;
    }
}
