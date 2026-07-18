using UnityEngine;

[RequireComponent(typeof(kms99400_PlayerController))]
public class kms99400_PlayerEnergy : MonoBehaviour
{
    [Header("에너지 설정")]
    [SerializeField]
    [Min(0.01f)]
    [Tooltip("최대 에너지 값입니다.")]
    private float maximumEnergy = 100f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("최대 이동 속도로 이동할 때 초당 충전되는 에너지량입니다.")]
    private float chargePerSecond = 25f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("에너지 충전이 시작되기 위한 최소 실제 이동 속도입니다.")]
    private float minimumChargeSpeed = 0.1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("초기화 또는 리셋 시 사용되는 에너지 값입니다.")]
    private float startingEnergy = 0f;

    public float CurrentEnergy { get; private set; }
    public float MaximumEnergy => maximumEnergy;
    public float NormalizedEnergy => maximumEnergy > 0f ? CurrentEnergy / maximumEnergy : 0f;
    public bool IsFull => CurrentEnergy >= maximumEnergy;

    public event System.Action<float, float> EnergyChanged;

    private kms99400_PlayerController playerController;
    private bool skipChargeThisFrame;

    private void Awake()
    {
        playerController = GetComponent<kms99400_PlayerController>();
        CurrentEnergy = Mathf.Clamp(startingEnergy, 0f, maximumEnergy);
    }

    private void LateUpdate()
    {
        if (skipChargeThisFrame)
        {
            skipChargeThisFrame = false;
            return;
        }

        if (CanChargeEnergy())
        {
            ChargeEnergy();
        }
    }

    private bool CanChargeEnergy()
    {
        return playerController != null
            && playerController.CanMove
            && playerController.IsGrounded
            && playerController.MoveInput.sqrMagnitude > 0.01f
            && playerController.IsMoving
            && playerController.CurrentMoveSpeed >= minimumChargeSpeed
            && CurrentEnergy < maximumEnergy
            && playerController.MoveSpeed > Mathf.Epsilon;
    }

    private void ChargeEnergy()
    {
        float speedRatio = Mathf.Clamp01(playerController.CurrentMoveSpeed / playerController.MoveSpeed);
        float chargeAmount = chargePerSecond * speedRatio * Time.deltaTime;
        AddEnergy(chargeAmount);
    }

    private void AddEnergy(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        SetEnergy(CurrentEnergy + amount);
    }

    private void SetEnergy(float value)
    {
        float previousEnergy = CurrentEnergy;
        CurrentEnergy = Mathf.Clamp(value, 0f, maximumEnergy);

        if (!Mathf.Approximately(previousEnergy, CurrentEnergy))
        {
            EnergyChanged?.Invoke(CurrentEnergy, maximumEnergy);
        }
    }

    public float ConsumeAllEnergy()
    {
        float consumedEnergy = CurrentEnergy;
        SetEnergy(0f);
        skipChargeThisFrame = true;
        return consumedEnergy;
    }

    public void ResetEnergy()
    {
        SetEnergy(startingEnergy);
        skipChargeThisFrame = true;
    }

    private void OnValidate()
    {
        maximumEnergy = Mathf.Max(0.01f, maximumEnergy);
        chargePerSecond = Mathf.Max(0f, chargePerSecond);
        minimumChargeSpeed = Mathf.Max(0f, minimumChargeSpeed);
        startingEnergy = Mathf.Clamp(startingEnergy, 0f, maximumEnergy);
    }
}
