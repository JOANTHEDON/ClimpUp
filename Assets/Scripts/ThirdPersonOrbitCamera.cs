using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ThirdPersonOrbitCamera : MonoBehaviour
{
    [Header("Follow Target")]
    [Tooltip("The player transform to follow")]
    public Transform target;

    [Tooltip("Point on character to look at (e.g. head/chest height)")]
    public Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Orbit / Mouse Settings")]
    [Tooltip("Distance from player")]
    public float distance = 4.5f;

    [Tooltip("Horizontal mouse sensitivity")]
    public float mouseSensitivityX = 150f;

    [Tooltip("Vertical mouse sensitivity")]
    public float mouseSensitivityY = 120f;

    [Tooltip("Minimum vertical angle")]
    public float minVerticalAngle = -25f;

    [Tooltip("Maximum vertical angle")]
    public float maxVerticalAngle = 70f;

    [Tooltip("Camera rotation smoothing")]
    public float rotationSmoothTime = 0.05f;

    [Header("Cursor Settings")]
    public bool lockCursor = true;

    [Header("Camera Collision")]
    public bool enableCollision = true;
    public LayerMask collisionLayers = ~0;
    public float collisionRadius = 0.2f;
    public float minDistance = 1.0f;

    private float yaw;
    private float pitch = 15f;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.Find("Player");
            if (player != null) target = player.transform;
        }

        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        currentYaw = yaw;
        currentPitch = pitch;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        // Don't toggle cursor lock via Escape if PauseMenuManager is handling it
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused)
        {
            return;
        }

        // Toggle cursor lock with Escape when not paused
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Skip camera rotation updates while paused
        if (Time.timeScale == 0f) return;

        // 1. Mouse delta input
        Vector2 mouseDelta = GetMouseDelta();

        yaw += mouseDelta.x * mouseSensitivityX * Time.deltaTime;
        pitch -= mouseDelta.y * mouseSensitivityY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);

        currentYaw = Mathf.SmoothDampAngle(currentYaw, yaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, pitch, ref pitchVelocity, rotationSmoothTime);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // 2. Position calculation with target offset
        Vector3 targetPivot = target.position + targetOffset;

        // 3. Collision check (prevent clipping through ground/walls)
        float currentDistance = distance;
        if (enableCollision)
        {
            Vector3 desiredCamPos = targetPivot - (rotation * Vector3.forward * distance);
            RaycastHit hit;
            if (Physics.SphereCast(targetPivot, collisionRadius, (desiredCamPos - targetPivot).normalized, out hit, distance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                currentDistance = Mathf.Clamp(hit.distance, minDistance, distance);
            }
        }

        Vector3 finalPosition = targetPivot - (rotation * Vector3.forward * currentDistance);

        transform.position = finalPosition;
        transform.rotation = rotation;
    }

    private Vector2 GetMouseDelta()
    {
        Vector2 delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            delta = Mouse.current.delta.ReadValue() * 0.05f; // New Input System delta normalization
        }
#endif

        if (delta == Vector2.zero)
        {
            try
            {
                delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            }
            catch { }
        }

        return delta;
    }
}
