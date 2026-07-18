using UnityEngine;
using UnityEngine.UI;

public class kms99400_PlayerEnergyUI : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    [Tooltip("에너지 값을 제공하는 Player의 kms99400_PlayerEnergy입니다. Player 루트에서 수동으로 연결해야 합니다.")]
    private kms99400_PlayerEnergy playerEnergy;

    [SerializeField]
    [Tooltip("게이지 길이를 제어할 EnergyGauge/FillArea/Fill의 RectTransform입니다.")]
    private RectTransform fillRectTransform;

    [SerializeField]
    [Tooltip("게이지 색상을 제어할 Fill의 Image 컴포넌트입니다.")]
    private Image fillImage;

    [Header("색상 설정")]
    [SerializeField]
    [Tooltip("에너지가 0일 때의 Fill 색상입니다.")]
    private Color minimumEnergyColor = new Color(0.55f, 0.9f, 1f, 1f);

    [SerializeField]
    [Tooltip("에너지가 최대일 때의 Fill 색상입니다.")]
    private Color maximumEnergyColor = new Color(1f, 0.45f, 0.1f, 1f);

    private void Awake()
    {
        if (playerEnergy == null || fillRectTransform == null || fillImage == null)
        {
            Debug.LogError($"{name}: 참조가 누락되었습니다. playerEnergy={playerEnergy != null}, fillRectTransform={fillRectTransform != null}, fillImage={fillImage != null}", this);
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (playerEnergy == null)
        {
            return;
        }

        playerEnergy.EnergyChanged += HandleEnergyChanged;

        // PlayerEnergy는 Awake에서 초기 이벤트를 발생시키지 않으므로 현재 값으로 즉시 갱신한다
        UpdateGauge(playerEnergy.CurrentEnergy, playerEnergy.MaximumEnergy);
    }

    private void OnDisable()
    {
        if (playerEnergy != null)
        {
            playerEnergy.EnergyChanged -= HandleEnergyChanged;
        }
    }

    private void HandleEnergyChanged(float currentEnergy, float maximumEnergy)
    {
        UpdateGauge(currentEnergy, maximumEnergy);
    }

    private void UpdateGauge(float currentEnergy, float maximumEnergy)
    {
        float normalizedEnergy = maximumEnergy > Mathf.Epsilon
            ? currentEnergy / maximumEnergy
            : 0f;

        normalizedEnergy = Mathf.Clamp01(normalizedEnergy);

        Vector2 anchorMax = fillRectTransform.anchorMax;
        anchorMax.x = normalizedEnergy;
        fillRectTransform.anchorMax = anchorMax;

        fillImage.color = Color.Lerp(minimumEnergyColor, maximumEnergyColor, normalizedEnergy);
    }
}
