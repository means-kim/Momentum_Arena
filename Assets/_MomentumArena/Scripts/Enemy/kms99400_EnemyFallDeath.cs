using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class kms99400_EnemyFallDeath : MonoBehaviour
{
    [Header("낙사 설정")]
    [SerializeField]
    [Tooltip("이 월드 Y 좌표 이하로 내려가면 적을 낙사 처리합니다.")]
    private float deathHeight = -5f;

    public bool IsDead { get; private set; }

    public event System.Action<kms99400_EnemyFallDeath> Died;

    private Rigidbody enemyRigidbody;
    private kms99400_EnemyMovement enemyMovement;

    private void Awake()
    {
        enemyRigidbody = GetComponent<Rigidbody>();
        if (enemyRigidbody == null)
        {
            Debug.LogError($"{name}: Rigidbody를 찾을 수 없습니다. kms99400_EnemyFallDeath에는 Rigidbody가 필요합니다.", this);
            enabled = false;
            return;
        }

        enemyMovement = GetComponent<kms99400_EnemyMovement>();
    }

    private void FixedUpdate()
    {
        if (IsDead)
        {
            return;
        }

        if (enemyRigidbody.position.y <= deathHeight)
        {
            DieByFalling();
        }
    }

    private void DieByFalling()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;

        if (enemyMovement != null)
        {
            enemyMovement.SetMovementEnabled(false);
        }

        Died?.Invoke(this);
        Destroy(gameObject);
    }
}
