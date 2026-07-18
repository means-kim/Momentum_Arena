using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class kms99400_PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("초당 이동 속도입니다.")]
    private float moveSpeed = 6f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("초당 회전 속도(도)입니다.")]
    private float rotationSpeed = 720f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("IsMoving이 true로 판정되기 위한 최소 실제 이동 속도입니다.")]
    private float minimumMoveSpeed = 0.1f;

    [Header("중력 설정")]
    [SerializeField]
    [Tooltip("초당 적용되는 중력 가속도입니다.")]
    private float gravity = -20f;

    [SerializeField]
    [Tooltip("바닥에 붙어 있도록 유지하는 아래쪽 속도입니다.")]
    private float groundedVerticalSpeed = -2f;

    [Header("참조")]
    [SerializeField]
    [Tooltip("이동 방향 계산의 기준이 되는 카메라 Transform입니다. 비워두면 Camera.main을 사용합니다.")]
    private Transform cameraTransform;

    public Vector2 MoveInput { get; private set; }
    public Vector3 MoveDirection { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public bool IsMoving { get; private set; }
    public bool CanMove { get; private set; } = true;
    public float MoveSpeed => moveSpeed;
    public bool IsGrounded => characterController.isGrounded;

    private CharacterController characterController;
    private kms99400_PlayerActions playerActions;
    private float verticalVelocity;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerActions = new kms99400_PlayerActions();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null)
        {
            Debug.LogError($"{name}: cameraTransform이 할당되지 않았고 Camera.main도 찾을 수 없습니다.", this);
        }
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

    private void Update()
    {
        ReadMoveInput();
        CalculateMoveDirection();
        UpdateRotation();
        UpdateGravity();

        Vector3 previousPosition = transform.position;
        MoveCharacter();
        UpdateMovementState(previousPosition);
    }

    private void ReadMoveInput()
    {
        if (!CanMove)
        {
            MoveInput = Vector2.zero;
            return;
        }

        MoveInput = playerActions.Player.Move.ReadValue<Vector2>();
    }

    private void CalculateMoveDirection()
    {
        if (!CanMove || cameraTransform == null)
        {
            MoveDirection = Vector3.zero;
            return;
        }

        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // 카메라의 상하 기울임이 이동에 영향을 주지 않도록 Y 성분을 제거한다
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 direction = cameraForward * MoveInput.y + cameraRight * MoveInput.x;
        MoveDirection = Vector3.ClampMagnitude(direction, 1f);
    }

    private void UpdateRotation()
    {
        if (!CanMove || MoveDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(MoveDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void UpdateGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedVerticalSpeed;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void MoveCharacter()
    {
        Vector3 velocity = MoveDirection * moveSpeed + Vector3.up * verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void UpdateMovementState(Vector3 previousPosition)
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            return;
        }

        // 실제 이동 여부는 입력값이 아니라 실제 위치 변화량으로 판단한다
        Vector3 displacement = transform.position - previousPosition;
        displacement.y = 0f;

        CurrentMoveSpeed = displacement.magnitude / deltaTime;
        IsMoving = CurrentMoveSpeed >= minimumMoveSpeed;
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        CanMove = isEnabled;

        if (!CanMove)
        {
            MoveInput = Vector2.zero;
            MoveDirection = Vector3.zero;
        }
    }
}
