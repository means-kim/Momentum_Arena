using UnityEngine;

[RequireComponent(typeof(kms99400_PlayerController))]
[RequireComponent(typeof(kms99400_PlayerAttack))]
public class kms99400_PlayerFallDeath : MonoBehaviour
{
    [Header("낙사 설정")]
    [SerializeField]
    [Tooltip("이 월드 Y 좌표 이하로 내려가면 플레이어를 낙사 처리합니다.")]
    private float deathHeight = -5f;

    public bool IsDead { get; private set; }

    public event System.Action<kms99400_PlayerFallDeath> Died;

    private kms99400_PlayerController playerController;
    private kms99400_PlayerAttack playerAttack;

    private void Awake()
    {
        playerController = GetComponent<kms99400_PlayerController>();
        playerAttack = GetComponent<kms99400_PlayerAttack>();

        if (playerController == null || playerAttack == null)
        {
            Debug.LogError($"{name}: kms99400_PlayerFallDeath에 필요한 컴포넌트가 없습니다. (PlayerController: {playerController != null}, PlayerAttack: {playerAttack != null})", this);
            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        if (IsDead)
        {
            return;
        }

        if (transform.position.y <= deathHeight)
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

        playerController.SetMovementEnabled(false);
        playerAttack.SetAttackEnabled(false);

        Died?.Invoke(this);
    }
}
