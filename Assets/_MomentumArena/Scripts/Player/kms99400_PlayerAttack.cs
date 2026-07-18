using UnityEngine;

[RequireComponent(typeof(kms99400_PlayerEnergy))]
[RequireComponent(typeof(kms99400_ShockwaveEmitter))]
public class kms99400_PlayerAttack : MonoBehaviour
{
    [Header("공격 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("공격이 다시 허용되기까지 필요한 최소 시간(초)입니다.")]
    private float attackCooldown = 0.5f;

    public bool CanAttack { get; private set; } = true;

    private kms99400_PlayerActions playerActions;
    private kms99400_PlayerEnergy playerEnergy;
    private kms99400_ShockwaveEmitter shockwaveEmitter;
    private float nextAttackTime;

    private void Awake()
    {
        playerEnergy = GetComponent<kms99400_PlayerEnergy>();
        shockwaveEmitter = GetComponent<kms99400_ShockwaveEmitter>();
        playerActions = new kms99400_PlayerActions();
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

    private void OnDestroy()
    {
        if (playerActions != null)
        {
            playerActions.Dispose();
            playerActions = null;
        }
    }

    private void Update()
    {
        TryAttack();
    }

    private void TryAttack()
    {
        if (!CanAttack)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        if (!playerActions.Player.Attack.WasPressedThisFrame())
        {
            return;
        }

        ExecuteAttack();
    }

    private void ExecuteAttack()
    {
        float consumedEnergy = playerEnergy.ConsumeAllEnergy();

        // NormalizedEnergy는 소비 이후 0이 되므로 소비량과 최대치로 직접 비율을 계산한다
        float energyRatio = playerEnergy.MaximumEnergy > Mathf.Epsilon
            ? consumedEnergy / playerEnergy.MaximumEnergy
            : 0f;

        energyRatio = Mathf.Clamp01(energyRatio);

        shockwaveEmitter.EmitShockwave(energyRatio);

        nextAttackTime = Time.time + attackCooldown;
    }

    public void SetAttackEnabled(bool isEnabled)
    {
        CanAttack = isEnabled;
    }

    private void OnValidate()
    {
        attackCooldown = Mathf.Max(0f, attackCooldown);
    }
}
