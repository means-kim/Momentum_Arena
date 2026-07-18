using UnityEngine;

public class kms99400_ThirdPersonCamera : MonoBehaviour
{
    [Header("추적 설정")]
    [SerializeField]
    [Tooltip("카메라가 따라갈 Player 루트 Transform입니다.")]
    private Transform target;

    [SerializeField]
    [Tooltip("Player 루트로부터 카메라가 바라보는 지점까지의 월드 공간 오프셋입니다.")]
    private Vector3 targetOffset = new Vector3(0f, 1.2f, 0f);

    [SerializeField]
    [Tooltip("오빗 회전이 적용되기 전, 목표 지점으로부터의 카메라 상대 위치입니다. Z가 음수일수록 Player 뒤쪽에 배치됩니다.")]
    private Vector3 cameraOffset = new Vector3(0f, 0f, -7f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("카메라 위치 이동에 사용되는 SmoothDamp 시간입니다.")]
    private float positionSmoothTime = 0.05f;

    [Header("회전 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("마우스 delta 값에 곱해지는 감도입니다.")]
    private float mouseSensitivity = 0.15f;

    [SerializeField]
    [Tooltip("Player의 시작 Y 회전값에 추가되는 초기 수평 각도입니다.")]
    private float initialYaw = 0f;

    [SerializeField]
    [Tooltip("초기 수직 오빗 각도입니다.")]
    private float initialPitch = 25f;

    [SerializeField]
    [Tooltip("수직 오빗 각도의 최소값입니다.")]
    private float minimumPitch = -15f;

    [SerializeField]
    [Tooltip("수직 오빗 각도의 최대값입니다.")]
    private float maximumPitch = 65f;

    [Header("커서 설정")]
    [SerializeField]
    [Tooltip("시작 시 커서를 잠그고 숨길지 여부입니다.")]
    private bool lockCursorOnStart = true;

    public Vector2 LookInput { get; private set; }
    public bool CanLook { get; private set; } = true;

    private kms99400_PlayerActions playerActions;
    private float yaw;
    private float pitch;
    private Vector3 smoothVelocity;

    private void Awake()
    {
        playerActions = new kms99400_PlayerActions();

        if (target == null)
        {
            Debug.LogError($"{name}: target이 할당되지 않았습니다. Player 루트 Transform을 Inspector에서 지정해야 합니다.", this);
            enabled = false;
            return;
        }

        yaw = target.eulerAngles.y + initialYaw;
        pitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
    }

    private void OnEnable()
    {
        if (playerActions != null)
        {
            playerActions.Player.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerActions != null)
        {
            playerActions.Player.Disable();
        }
    }

    private void Start()
    {
        InitializeCursor();
        UpdateCameraTransform(true);
    }

    private void Update()
    {
        ReadLookInput();
        UpdateOrbitAngles();
    }

    private void LateUpdate()
    {
        UpdateCameraTransform(false);
    }

    private void OnDestroy()
    {
        if (playerActions != null)
        {
            playerActions.Dispose();
            playerActions = null;
        }
    }

    private void ReadLookInput()
    {
        if (!CanLook)
        {
            LookInput = Vector2.zero;
            return;
        }

        LookInput = playerActions.Player.Look.ReadValue<Vector2>();
    }

    private void UpdateOrbitAngles()
    {
        if (!CanLook)
        {
            return;
        }

        // <Mouse>/delta는 이미 프레임당 이동량이므로 Time.deltaTime을 곱하지 않는다
        yaw += LookInput.x * mouseSensitivity;
        pitch -= LookInput.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minimumPitch, maximumPitch);
    }

    private void UpdateCameraTransform(bool snapImmediately)
    {
        if (target == null)
        {
            return;
        }

        Vector3 targetPoint = target.position + targetOffset;
        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = targetPoint + orbitRotation * cameraOffset;

        if (snapImmediately || positionSmoothTime <= 0f)
        {
            transform.position = desiredPosition;
            smoothVelocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref smoothVelocity, positionSmoothTime);
        }

        Vector3 lookDirection = targetPoint - transform.position;
        if (lookDirection.sqrMagnitude > 0f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }

    private void InitializeCursor()
    {
        if (!lockCursorOnStart)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void SetCameraControlEnabled(bool isEnabled)
    {
        CanLook = isEnabled;

        if (!CanLook)
        {
            LookInput = Vector2.zero;
        }
    }

    private void OnValidate()
    {
        positionSmoothTime = Mathf.Max(0f, positionSmoothTime);
        mouseSensitivity = Mathf.Max(0f, mouseSensitivity);

        if (maximumPitch < minimumPitch)
        {
            maximumPitch = minimumPitch;
        }

        initialPitch = Mathf.Clamp(initialPitch, minimumPitch, maximumPitch);
    }
}
