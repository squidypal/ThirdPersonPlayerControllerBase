using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [Header("Camera")]
    public Transform cameraTarget;
    public Camera playerCamera;
    public float mouseSensitivity = 2f;
    public float cameraDistance = 5f;
    public float cameraHeight = 2f;
    public float cameraPositionSmoothing = 0.1f;
    public float cameraRotationSmoothing = 0.1f;
    public float minYAngle = -30f;
    public float maxYAngle = 60f;

    [Header("Crouch")]
    public float crouchHeight = 1f;
    public float standHeight = 2f;
    public float crouchTransitionSpeed = 10f;

    [Header("Animation")]
    public float animationSmoothTime = 0.1f;
    public float sprintAnimationMultiplier = 1.5f;

    [Header("Important Fields")]
    public Transform groundCheckTransform;
    private CharacterController controller;
    public Animator animator;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    private bool isSprinting;
    
    private float xRotation = 0f;
    private float yRotation = 0f;
    
    private Vector3 targetCameraPosition;
    private Vector3 cameraVelocity;
    private Vector3 rotationVelocity;
    private float currentHeight;

    private Vector2 animationBlend;
    private Vector2 animationVelocity;
    private float animationSpeed;
    private float speedVelocity;

    private int horizontalHash;
    private int verticalHash;
    private int speedHash;
    private int isGroundedHash;
    private int isCrouchingHash;
    private int jumpHash;

    
    [Header("Waving")]
    public KeyCode waveKey = KeyCode.Q;
    public AnimationClip waveAnimation;

    void Start()
    {
        controller = GetComponent<CharacterController>();

        horizontalHash = Animator.StringToHash("Horizontal");
        verticalHash = Animator.StringToHash("Vertical");
        speedHash = Animator.StringToHash("Speed");
        isGroundedHash = Animator.StringToHash("IsGrounded");
        isCrouchingHash = Animator.StringToHash("IsCrouching");
        jumpHash = Animator.StringToHash("Jump");
        
        currentHeight = standHeight;
        controller.height = currentHeight;
        
        if (cameraTarget == null)
        {
            GameObject target = new GameObject("CameraTarget");
            target.transform.SetParent(transform);
            target.transform.localPosition = new Vector3(0, cameraHeight, 0);
            cameraTarget = target.transform;
        }
        
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleInput();
        HandleCrouch();
        HandleAnimation();
        if (Input.GetKeyDown(waveKey))
        {
            PlayArmAnimation(waveAnimation);
        }
    }

    public void PlayArmAnimation(AnimationClip animationClip)
    {
        animator.Play(animationClip.name, 0);
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void LateUpdate()
    {
        HandleCamera();
    }

    void HandleInput()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = !isCrouching;
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }

    void HandleMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheckTransform.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        
        float currentSpeed = walkSpeed;
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isSprinting)
            currentSpeed = sprintSpeed;

        controller.Move(move * currentSpeed * Time.fixedDeltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouching)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null)
                animator.SetTrigger("Jump");
        }

        velocity.y += gravity * Time.fixedDeltaTime;
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    void HandleAnimation()
    {
        if (animator == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector2 inputVector = new Vector2(horizontal, vertical);
        
        if (inputVector.magnitude > 1f)
            inputVector = inputVector.normalized;

        animationBlend = Vector2.SmoothDamp(animationBlend, inputVector, ref animationVelocity, animationSmoothTime);
        
        float targetSpeed = inputVector.magnitude;
        
        if (isCrouching)
        {
            targetSpeed *= 0.5f; 
        }
        else if (isSprinting)
        {
            targetSpeed *= sprintAnimationMultiplier; 
        }
        
        animationSpeed = Mathf.SmoothDamp(animationSpeed, targetSpeed, ref speedVelocity, animationSmoothTime);
        
        if (animationSpeed < 0.01f)
            animationSpeed = 0f;
        
        if (Mathf.Abs(animationBlend.x) < 0.01f)
            animationBlend.x = 0f;
        if (Mathf.Abs(animationBlend.y) < 0.01f)
            animationBlend.y = 0f;

        animator.SetFloat(horizontalHash, animationBlend.x);
        animator.SetFloat(verticalHash, animationBlend.y);
        animator.SetFloat(speedHash, animationSpeed);
        animator.SetBool(isGroundedHash, isGrounded);
        animator.SetBool(isCrouchingHash, isCrouching);
    }

    void HandleCamera()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, minYAngle, maxYAngle);

            transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }

        Vector3 direction = new Vector3(0, 0, -cameraDistance);
        Quaternion rotation = Quaternion.Euler(xRotation, yRotation, 0);
        targetCameraPosition = cameraTarget.position + rotation * direction;

        RaycastHit hit;
        Vector3 raycastStart = cameraTarget.position;
        if (Physics.Linecast(raycastStart, targetCameraPosition, out hit))
        {
            targetCameraPosition = hit.point + hit.normal * 0.2f;
        }

        playerCamera.transform.position = Vector3.SmoothDamp(
            playerCamera.transform.position, 
            targetCameraPosition, 
            ref cameraVelocity, 
            cameraPositionSmoothing
        );

        Vector3 lookDirection = cameraTarget.position - playerCamera.transform.position;
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            playerCamera.transform.rotation = Quaternion.Slerp(
                playerCamera.transform.rotation, 
                targetRotation, 
                Time.deltaTime / cameraRotationSmoothing
            );
        }
    }

    void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        controller.height = currentHeight;
        if (animator != null)
        {
            animator.SetBool("IsCrouching", isCrouching);
        }
        Vector3 targetPos = cameraTarget.localPosition;
        targetPos.y = isCrouching ? cameraHeight * 0.6f : cameraHeight;
        cameraTarget.localPosition = Vector3.Lerp(cameraTarget.localPosition, targetPos, crouchTransitionSpeed * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, groundDistance);
    }

    public Vector2 GetMovementInput()
    {
        return animationBlend;
    }
    
    public float GetMovementSpeed()
    {
        return animationSpeed;
    }
    
    public bool IsMoving()
    {
        return animationSpeed > 0.01f;
    }
}