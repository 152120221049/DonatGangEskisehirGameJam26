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
    public bool canJump = true; 
    
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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        
        rb.freezeRotation = true;
        currentSpawnPoint = transform.position;

        capsuleCollider.height = normalHeight;
        capsuleCollider.center = Vector3.zero;
    }

    void Update()
    {
        if (Keyboard.current == null) return;

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
        
        UpdateColliderHeight();
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        
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

        if (showDebugLogs && wasGrounded != isGrounded)
        {
            Debug.Log("Grounded State: " + isGrounded);
        }
    }

    private void MovePlayer()
    {
        float currentSpeed = walkSpeed;
        if (isSneaking) currentSpeed = sneakSpeed;

        Vector3 moveDirection = (transform.right * horizontalInput + transform.forward * verticalInput).normalized;

        if (isSliding)
        {
            Vector3 targetVelocity = slideDirection * slideBoostSpeed;
            targetVelocity.y = rb.linearVelocity.y;
            rb.linearVelocity = targetVelocity;
        }
        else
        {
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 targetVelocity = moveDirection * currentSpeed;

            float accel = isOnSlipperySurface ? slipperyAcceleration : acceleration;
            float decel = isOnSlipperySurface ? slipperyDeceleration : deceleration;

            float currentAccel = (moveDirection.magnitude > 0.1f) ? accel : decel;
            Vector3 newVelocity = Vector3.Lerp(currentVelocity, new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z), currentAccel * Time.fixedDeltaTime);
            
            rb.linearVelocity = new Vector3(newVelocity.x, currentVelocity.y, newVelocity.z);
        }
    }

    private void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (!canJump) { if (showDebugLogs) Debug.Log("Jump failed: canJump rule is False"); return; }
            if (!isGrounded) { if (showDebugLogs) Debug.Log("Jump failed: Not Grounded"); return; }
            if (isSliding) { if (showDebugLogs) Debug.Log("Jump failed: Sliding"); return; }
            
            if (CanStandUp())
            {
                if (showDebugLogs) Debug.Log("Jumping Success!");
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                
                if (isSneaking && !Keyboard.current.leftCtrlKey.isPressed)
                {
                    isSneaking = false;
                }
            }
            else
            {
                if (showDebugLogs) Debug.Log("Jump failed: Ceiling blocked");
            }
        }
    }

    private void HandleSneak()
    {
        if (isSliding) return;

        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            isSneaking = true;
        }
        
        if (!Keyboard.current.leftCtrlKey.isPressed && isSneaking)
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
        // Check ONLY upwards from the head to avoid hitting the floor
        float radius = capsuleCollider.radius * 0.8f; 
        Vector3 headPosition = transform.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height * 0.5f - radius);
        float distanceToCheck = normalHeight - capsuleCollider.height;
        
        if (distanceToCheck <= 0.1f) return true; // Already standing
        
        return !Physics.SphereCast(headPosition, radius, Vector3.up, out RaycastHit hit, distanceToCheck, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void HandleInteraction()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, interactionRadius, interactableMask);
            foreach (var hitCollider in hitColliders)
            {
                IInteractable interactable = hitCollider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    if (showDebugLogs) Debug.Log("Interacting with: " + hitCollider.gameObject.name);
                    interactable.Interact(this.gameObject);
                    break;
                }
            }
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
            
            Gizmos.color = Color.blue;
            float radius = capsuleCollider.radius * 0.8f;
            Vector3 headPosition = transform.position + capsuleCollider.center + Vector3.up * (capsuleCollider.height * 0.5f - radius);
            float dist = normalHeight - capsuleCollider.height;
            if (dist > 0) Gizmos.DrawWireSphere(headPosition + Vector3.up * dist, radius);
        }
    }
}
