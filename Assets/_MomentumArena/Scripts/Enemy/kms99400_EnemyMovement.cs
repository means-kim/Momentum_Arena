using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class kms99400_EnemyMovement : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    [Tooltip("추적할 대상 Transform입니다. Player 루트를 직접 할당하거나 SetTarget으로 런타임에 지정합니다.")]
    private Transform target;

    [SerializeField]
    [Tooltip("바닥으로 인식할 레이어입니다. ArenaFloor가 포함된 Ground 레이어만 지정하세요.")]
    private LayerMask groundLayerMask;

    [Header("이동 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("최대 수평 추적 속도입니다.")]
    private float moveSpeed = 3f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("초당 수평 가속 및 감속량입니다.")]
    private float acceleration = 12f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("이 거리 이내에서는 대상에게 더 접근하지 않고 정지합니다.")]
    private float stopDistance = 1.3f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("초당 최대 Y축 회전 속도(도)입니다.")]
    private float rotationSpeed = 540f;

    [Header("넉백 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("이 값을 초과하는 수평 속도는 강한 넉백으로 간주되어 AI 이동이 일시 중단됩니다.")]
    private float knockbackRecoverySpeed = 3.5f;

    [Header("지면 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("현재 접지 판정 시 Collider 경계 아래로 추가되는 레이캐스트 거리입니다.")]
    private float groundCheckDistance = 0.15f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("전방에 바닥이 있는지 확인할 때 사용하는 전방 이동 거리입니다.")]
    private float edgeCheckForwardDistance = 0.75f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("전방 낭떠러지 확인 시 Collider 경계 아래로 추가되는 레이캐스트 거리입니다.")]
    private float edgeCheckDownDistance = 0.3f;

    public bool CanMove { get; private set; } = true;
    public bool IsGrounded { get; private set; }
    public bool IsRecoveringFromKnockback { get; private set; }
    public bool IsWithinStopDistance { get; private set; }

    private Rigidbody enemyRigidbody;
    private Collider enemyCollider;

    private void Awake()
    {
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            Debug.LogError($"{name}: Rigidbody를 찾을 수 없습니다.", this);
            enabled = false;
            return;
        }

        enemyCollider = GetComponent<Collider>();
        if (enemyCollider == null)
        {
            Debug.LogError($"{name}: EnemyMovement는 기존 Collider가 필요합니다. 오브젝트에 Collider를 추가하세요.", this);
            enabled = false;
            return;
        }

        if (enemyRigidbody.isKinematic)
        {
            Debug.LogError($"{name}: Rigidbody 기반 이동과 충격파 넉백을 사용하려면 Is Kinematic을 비활성화해야 합니다.", this);
            enabled = false;
            return;
        }

        if (enemyCollider.isTrigger)
        {
            Debug.LogWarning($"{name}: Collider가 트리거로 설정되어 있어 의도한 물리적 충돌을 제공하지 않습니다.", this);
        }

        if (groundLayerMask.value == 0)
        {
            Debug.LogWarning($"{name}: Ground Layer Mask가 지정되지 않아 지면 및 낭떠러지 감지가 동작할 수 없습니다.", this);
        }
    }

    private void FixedUpdate()
    {
        Vector3 currentVelocity = enemyRigidbody.linearVelocity;
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        IsGrounded = CheckGrounded();

        float recoverySpeedSquared = knockbackRecoverySpeed * knockbackRecoverySpeed;
        IsRecoveringFromKnockback = currentHorizontalVelocity.sqrMagnitude > recoverySpeedSquared;

        Vector3 toTarget = Vector3.zero;

        if (target != null)
        {
            toTarget = target.position - transform.position;
            toTarget.y = 0f;
            IsWithinStopDistance = toTarget.sqrMagnitude <= stopDistance * stopDistance;
        }
        else
        {
            IsWithinStopDistance = false;
        }

        // 공중, 넉백 회복 중, 대상 없음, 외부 비활성화 상태에서는 Rigidbody를 건드리지 않고
        // 충격파 임펄스나 낙하 속도 등 기존 물리 상태를 그대로 보존한다
        if (target == null || !CanMove || !IsGrounded || IsRecoveringFromKnockback)
        {
            return;
        }

        bool hasValidDirection = toTarget.sqrMagnitude > 0.0001f;
        Vector3 moveDirection = hasValidDirection ? toTarget.normalized : Vector3.zero;

        Vector3 desiredHorizontalVelocity;

        if (IsWithinStopDistance)
        {
            desiredHorizontalVelocity = Vector3.zero;

            if (hasValidDirection)
            {
                RotateTowards(moveDirection);
            }
        }
        else if (hasValidDirection && HasGroundAhead(moveDirection))
        {
            desiredHorizontalVelocity = moveDirection * moveSpeed;
            RotateTowards(moveDirection);
        }
        else
        {
            // 전방에 바닥이 없거나 방향을 계산할 수 없으면 감속만 수행하고 회전은 하지 않는다
            desiredHorizontalVelocity = Vector3.zero;
        }

        ApplyHorizontalMovement(currentHorizontalVelocity, desiredHorizontalVelocity, currentVelocity.y);
    }

    private bool CheckGrounded()
    {
        Bounds bounds = enemyCollider.bounds;
        return Physics.Raycast(
            bounds.center,
            Vector3.down,
            bounds.extents.y + groundCheckDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore);
    }

    private bool HasGroundAhead(Vector3 moveDirection)
    {
        Bounds bounds = enemyCollider.bounds;
        Vector3 edgeCheckOrigin = bounds.center + moveDirection * edgeCheckForwardDistance;

        return Physics.Raycast(
            edgeCheckOrigin,
            Vector3.down,
            bounds.extents.y + edgeCheckDownDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore);
    }

    private void ApplyHorizontalMovement(Vector3 currentHorizontalVelocity, Vector3 desiredHorizontalVelocity, float currentVerticalVelocity)
    {
        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            desiredHorizontalVelocity,
            acceleration * Time.fixedDeltaTime);

        enemyRigidbody.linearVelocity = new Vector3(newHorizontalVelocity.x, currentVerticalVelocity, newHorizontalVelocity.z);
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
        Quaternion nextRotation = Quaternion.RotateTowards(
            enemyRigidbody.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime);

        enemyRigidbody.MoveRotation(nextRotation);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        CanMove = isEnabled;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        acceleration = Mathf.Max(0f, acceleration);
        stopDistance = Mathf.Max(0f, stopDistance);
        rotationSpeed = Mathf.Max(0f, rotationSpeed);
        knockbackRecoverySpeed = Mathf.Max(moveSpeed, knockbackRecoverySpeed);
        groundCheckDistance = Mathf.Max(0f, groundCheckDistance);
        edgeCheckForwardDistance = Mathf.Max(0f, edgeCheckForwardDistance);
        edgeCheckDownDistance = Mathf.Max(0f, edgeCheckDownDistance);
    }
}
