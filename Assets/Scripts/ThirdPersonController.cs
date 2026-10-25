using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed while walking/running")]
    public float moveSpeed = 5f;

    [Tooltip("How fast the character rotates to face movement direction")]
    public float rotationSmoothTime = 0.12f;

    [Header("Jump & Gravity")]
    [Tooltip("Height character can jump in units")]
    public float jumpHeight = 1.5f;

    [Tooltip("Downward gravity force")]
    public float gravity = -20f;

    [Tooltip("Small extra buffer for ground detection")]
    public float groundCheckDistance = 0.15f;

    [Tooltip("Layers recognized as ground")]
    public LayerMask groundLayers = ~0;

    [Header("Falling & Game Over")]
    [Tooltip("Max seconds player can be continuously falling/airborne before Game Over triggers")]
    public float maxFallDuration = 6.0f;

    private float fallTimer = 0f;

    [Header("Jump Animation Tuning")]
    [Tooltip("Minimum duration (in seconds) the jump animation stays active so it doesn't get cancelled immediately")]
    public float minJumpAnimationDuration = 0.4f;

    [Header("References")]
    [Tooltip("Camera Transform to calculate direction relative to camera view")]
    public Transform cameraTransform;

    [Tooltip("Animator with IsWalking and IsJumping bool parameters")]
    public Animator animator;

    [Header("Animator Parameters")]
    public string isWalkingParam = "IsWalking";
    public string isJumpingParam = "IsJumping";

    [Header("Platform Spawner Integration")]
    [Tooltip("Optional reference to PlatformSpawner. If left empty, will look on this GameObject or in the scene.")]
    public PlatformSpawner platformSpawner;

    [Header("Audio")]
    [Tooltip("Audio clip for walking footsteps")]
    public AudioClip walkSound;

    [Tooltip("Audio clip for jumping")]
    public AudioClip jumpSound;

    [Tooltip("Volume for walking sound (0 to 1)")]
    [Range(0f, 1f)] public float walkVolume = 0.6f;

    [Tooltip("Volume for jump sound (0 to 1)")]
    [Range(0f, 1f)] public float jumpVolume = 0.8f;

    [Tooltip("AudioSource component used to play sounds")]
    public AudioSource audioSource;

    // Internal state
    private CharacterController controller;
    private float verticalVelocity;
    private float rotationVelocity;
    private bool isGrounded;
    private float jumpTimer = 0f;
    private AudioSource walkAudioSource; // dedicated looped channel for smooth walk footsteps

    private int isWalkingHash;
    private int isJumpingHash;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (platformSpawner == null)
        {
            platformSpawner = GetComponent<PlatformSpawner>();
            if (platformSpawner == null)
            {
                platformSpawner = FindObjectOfType<PlatformSpawner>();
            }
        }

        // Setup audio sources
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.playOnAwake = false;

        // Create a secondary child audio source dedicated to looping walk footsteps cleanly
        GameObject walkAudioObj = new GameObject("WalkAudioSource");
        walkAudioObj.transform.SetParent(transform, false);
        walkAudioSource = walkAudioObj.AddComponent<AudioSource>();
        walkAudioSource.playOnAwake = false;
        walkAudioSource.loop = true;
        walkAudioSource.clip = walkSound;
        walkAudioSource.volume = walkVolume;

        isWalkingHash = Animator.StringToHash(isWalkingParam);
        isJumpingHash = Animator.StringToHash(isJumpingParam);
    }

    private void Update()
    {
        CheckGrounded();
        HandleMovementAndJump();
    }

    private void CheckGrounded()
    {
        if (controller.isGrounded)
        {
            isGrounded = true;
        }
        else
        {
            // Spherecast at the bottom of the capsule
            Vector3 bottomCenter = transform.position + controller.center - new Vector3(0f, (controller.height / 2f) - controller.radius, 0f);
            isGrounded = Physics.SphereCast(bottomCenter, controller.radius * 0.9f, Vector3.down, out _, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore);
        }

        if (isGrounded)
        {
            fallTimer = 0f; // Reset fall timer when safely on ground/platform

            if (verticalVelocity < 0f)
            {
                verticalVelocity = -2f; // Keep pinned to ground
            }
        }
        else
        {
            // Player is airborne / falling
            fallTimer += Time.deltaTime;
            if (fallTimer >= maxFallDuration)
            {
                if (GameOverManager.Instance != null)
                {
                    GameOverManager.Instance.TriggerGameOver();
                }
            }
        }
    }

    private void HandleMovementAndJump()
    {
        // 1. Read input
        Vector2 input = GetMoveInput();
        bool jumpPressed = GetJumpInput();

        // 2. Movement direction relative to camera
        Vector3 moveDirection = Vector3.zero;

        if (input.sqrMagnitude > 0.001f)
        {
            Vector3 inputDirection = new Vector3(input.x, 0f, input.y).normalized;

            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg;
            if (cameraTransform != null)
            {
                targetAngle += cameraTransform.eulerAngles.y;
            }

            float smoothedAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothedAngle, 0f);

            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }

        // 3. Jump logic
        if (jumpTimer > 0f)
        {
            jumpTimer -= Time.deltaTime;
        }

        if (jumpPressed && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpTimer = minJumpAnimationDuration;
            isGrounded = false;

            // Stop walking audio immediately when jumping
            if (walkAudioSource != null && walkAudioSource.isPlaying)
            {
                walkAudioSource.Stop();
            }

            // Play jump sound
            if (jumpSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(jumpSound, jumpVolume);
            }

            if (platformSpawner != null)
            {
                platformSpawner.OnPlayerJump();
            }
        }

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;

        // 4. Move Controller
        Vector3 velocity = (moveDirection * moveSpeed) + (Vector3.up * verticalVelocity);
        controller.Move(velocity * Time.deltaTime);

        // 5. Update Animator & Walking Audio
        bool isMoving = input.sqrMagnitude > 0.01f;
        if (animator != null)
        {
            animator.SetBool(isWalkingHash, isMoving);

            // IsJumping stays true in loop as long as the player is not grounded (in air / jumping / falling)
            // It only becomes false when player lands on a platform/ground
            bool inAir = !isGrounded;
            animator.SetBool(isJumpingHash, inAir);
        }

        // Handle footsteps audio: play loop only when moving AND grounded
        if (walkAudioSource != null)
        {
            if (walkSound != null && walkAudioSource.clip != walkSound)
            {
                walkAudioSource.clip = walkSound;
            }
            walkAudioSource.volume = walkVolume;

            if (isMoving && isGrounded)
            {
                if (!walkAudioSource.isPlaying)
                {
                    walkAudioSource.Play();
                }
            }
            else
            {
                if (walkAudioSource.isPlaying)
                {
                    walkAudioSource.Stop();
                }
            }
        }
    }

    private Vector2 GetMoveInput()
    {
        Vector2 move = Vector2.zero;

        try
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) move.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) move.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) move.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) move.x += 1f;
        }
        catch { }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) move.y += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) move.y -= 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) move.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) move.x += 1f;
        }
#endif

        return move.normalized;
    }

    private bool GetJumpInput()
    {
        bool jump = false;

        try
        {
            if (Input.GetKeyDown(KeyCode.Space)) jump = true;
        }
        catch { }

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            jump = true;
        }
#endif

        return jump;
    }
}
