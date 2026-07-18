using System.Collections.Generic;
using UnityEngine;

public class kms99400_ShockwaveEmitter : MonoBehaviour
{
    [Header("범위 설정")]
    [SerializeField]
    [Tooltip("Player 루트(발밑 기준) 로컬 공간에서 충격파 중심까지의 오프셋입니다.")]
    private Vector3 centerOffset = new Vector3(0f, 1f, 0f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("에너지 비율이 0일 때 사용되는 충격파 반경입니다.")]
    private float minimumRadius = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("에너지 비율이 1일 때 사용되는 충격파 반경입니다.")]
    private float maximumRadius = 4.5f;

    [Header("넉백 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("에너지 비율이 0일 때 적용되는 수평 넉백 힘입니다.")]
    private float minimumKnockbackForce = 4f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("에너지 비율이 1일 때 적용되는 수평 넉백 힘입니다.")]
    private float maximumKnockbackForce = 16f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("모든 피격 대상에게 추가로 적용되는 수직 상승 힘입니다.")]
    private float upwardForce = 1.5f;

    [Header("감지 설정")]
    [SerializeField]
    [Tooltip("충격파가 감지할 Enemy 레이어입니다. Inspector에서 직접 지정해야 합니다.")]
    private LayerMask enemyLayerMask;

    public event System.Action<Vector3, float, float> ShockwaveEmitted;

    private readonly HashSet<Rigidbody> affectedRigidbodies = new();

    private void Awake()
    {
        if (enemyLayerMask.value == 0)
        {
            Debug.LogWarning($"{name}: Enemy 레이어마스크가 지정되지 않았습니다. 레이어를 지정하기 전까지 충격파가 대상을 감지할 수 없습니다.", this);
        }
    }

    public void EmitShockwave(float energyRatio)
    {
        float clampedRatio = Mathf.Clamp01(energyRatio);

        float radius = Mathf.Lerp(minimumRadius, maximumRadius, clampedRatio);
        float knockbackForce = Mathf.Lerp(minimumKnockbackForce, maximumKnockbackForce, clampedRatio);
        Vector3 center = transform.TransformPoint(centerOffset);

        affectedRigidbodies.Clear();

        Collider[] hitColliders = Physics.OverlapSphere(center, radius, enemyLayerMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            ApplyKnockback(hitColliders[i], center, knockbackForce);
        }

        ShockwaveEmitted?.Invoke(center, radius, clampedRatio);
    }

    private void ApplyKnockback(Collider hitCollider, Vector3 center, float knockbackForce)
    {
        Rigidbody targetRigidbody = hitCollider.attachedRigidbody;

        if (targetRigidbody == null || targetRigidbody.isKinematic)
        {
            return;
        }

        if (!affectedRigidbodies.Add(targetRigidbody))
        {
            return;
        }

        Vector3 direction = CalculateKnockbackDirection(targetRigidbody, center);
        Vector3 impulse = direction * knockbackForce + Vector3.up * upwardForce;
        targetRigidbody.AddForce(impulse, ForceMode.Impulse);
    }

    private Vector3 CalculateKnockbackDirection(Rigidbody targetRigidbody, Vector3 center)
    {
        Vector3 direction = targetRigidbody.worldCenterOfMass - center;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction.normalized;
        }

        // 대상이 충격파 중심에 거의 겹쳐 있을 때를 위한 대체 방향
        Vector3 fallback = transform.forward;
        fallback.y = 0f;

        if (fallback.sqrMagnitude > 0.0001f)
        {
            return fallback.normalized;
        }

        return Vector3.forward;
    }

    private void OnValidate()
    {
        minimumRadius = Mathf.Max(0f, minimumRadius);
        maximumRadius = Mathf.Max(minimumRadius, maximumRadius);

        minimumKnockbackForce = Mathf.Max(0f, minimumKnockbackForce);
        maximumKnockbackForce = Mathf.Max(minimumKnockbackForce, maximumKnockbackForce);

        upwardForce = Mathf.Max(0f, upwardForce);
    }
}
