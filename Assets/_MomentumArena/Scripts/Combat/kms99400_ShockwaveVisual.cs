using UnityEngine;

[RequireComponent(typeof(kms99400_ShockwaveEmitter))]
public class kms99400_ShockwaveVisual : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    [Tooltip("충격파 링을 그릴 Line Renderer입니다. Player/ShockwaveVisual에서 수동으로 연결해야 합니다.")]
    private LineRenderer lineRenderer;

    [Header("재생 설정")]
    [SerializeField]
    [Min(0.01f)]
    [Tooltip("효과가 재생되는 전체 시간(초)입니다.")]
    private float duration = 0.25f;

    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("최종 반경 대비 시작 반경의 비율입니다.")]
    private float startRadiusRatio = 0.15f;

    [SerializeField]
    [Tooltip("바닥과의 겹침을 방지하기 위해 Player 루트 위치보다 높이는 값입니다.")]
    private float verticalOffset = 0.05f;

    [SerializeField]
    [Min(3)]
    [Tooltip("원형 링을 구성하는 점의 개수입니다.")]
    private int segmentCount = 64;

    [Header("두께 설정")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("효과 시작 시점의 선 두께입니다.")]
    private float startWidth = 0.18f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("효과 종료 시점의 선 두께입니다.")]
    private float endWidth = 0.02f;

    [Header("색상 설정")]
    [SerializeField]
    [Tooltip("에너지 비율이 0일 때 사용되는 색상입니다.")]
    private Color minimumEnergyColor = new Color(0.55f, 0.9f, 1f, 1f);

    [SerializeField]
    [Tooltip("에너지 비율이 1일 때 사용되는 색상입니다.")]
    private Color maximumEnergyColor = new Color(1f, 0.45f, 0.1f, 1f);

    private kms99400_ShockwaveEmitter shockwaveEmitter;
    private Transform visualTransform;
    private Vector3[] unitCirclePoints;
    private Vector3[] scaledCirclePoints;
    private bool isPlaying;
    private float elapsedTime;
    private float targetRadius;
    private Vector3 effectWorldPosition;
    private Color effectColor;

    private void Awake()
    {
        shockwaveEmitter = GetComponent<kms99400_ShockwaveEmitter>();

        if (shockwaveEmitter == null)
        {
            Debug.LogError($"{name}: kms99400_ShockwaveEmitter를 찾을 수 없습니다.");
            enabled = false;
            return;
        }

        if (lineRenderer == null)
        {
            Debug.LogError($"{name}: lineRenderer가 Inspector에 할당되지 않았습니다.");
            enabled = false;
            return;
        }

        visualTransform = lineRenderer.transform;

        InitializeCirclePoints();

        lineRenderer.loop = true;
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = segmentCount;
        lineRenderer.enabled = false;
    }

    private void InitializeCirclePoints()
    {
        unitCirclePoints = new Vector3[segmentCount];
        scaledCirclePoints = new Vector3[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i / (float)segmentCount * Mathf.PI * 2f;
            unitCirclePoints[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }

    private void OnEnable()
    {
        if (shockwaveEmitter != null)
        {
            shockwaveEmitter.ShockwaveEmitted += HandleShockwaveEmitted;
        }
    }

    private void OnDisable()
    {
        if (shockwaveEmitter != null)
        {
            shockwaveEmitter.ShockwaveEmitted -= HandleShockwaveEmitted;
        }

        StopVisual();
    }

    private void HandleShockwaveEmitted(Vector3 center, float radius, float energyRatio)
    {
        float clampedRatio = Mathf.Clamp01(energyRatio);

        targetRadius = Mathf.Max(0f, radius);
        // 물리 중심은 캐릭터 몸통 근처이므로, 시각 효과는 Player 루트 높이 기준으로 배치한다
        effectWorldPosition = new Vector3(center.x, transform.position.y + verticalOffset, center.z);
        effectColor = Color.Lerp(minimumEnergyColor, maximumEnergyColor, clampedRatio);

        elapsedTime = 0f;
        isPlaying = true;
        lineRenderer.enabled = true;

        visualTransform.position = effectWorldPosition;

        UpdateVisual(0f);
    }

    private void LateUpdate()
    {
        if (!isPlaying)
        {
            return;
        }

        visualTransform.position = effectWorldPosition;

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / duration);

        UpdateVisual(progress);

        if (elapsedTime >= duration)
        {
            StopVisual();
        }
    }

    private void UpdateVisual(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        float easedProgress = 1f - Mathf.Pow(1f - clampedProgress, 2f);

        float startRadius = targetRadius * startRadiusRatio;
        float currentRadius = Mathf.Lerp(startRadius, targetRadius, easedProgress);
        UpdateCircleRadius(currentRadius);

        float currentWidth = Mathf.Lerp(startWidth, endWidth, clampedProgress);
        lineRenderer.startWidth = currentWidth;
        lineRenderer.endWidth = currentWidth;

        Color currentColor = effectColor;
        float fadeAlpha = 1f - clampedProgress;
        currentColor.a = effectColor.a * fadeAlpha;
        lineRenderer.startColor = currentColor;
        lineRenderer.endColor = currentColor;
    }

    private void UpdateCircleRadius(float radius)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            scaledCirclePoints[i] = unitCirclePoints[i] * radius;
        }

        lineRenderer.SetPositions(scaledCirclePoints);
    }

    private void StopVisual()
    {
        isPlaying = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void OnValidate()
    {
        duration = Mathf.Max(0.01f, duration);
        startRadiusRatio = Mathf.Clamp01(startRadiusRatio);
        startWidth = Mathf.Max(0f, startWidth);
        endWidth = Mathf.Max(0f, endWidth);
        segmentCount = Mathf.Max(3, segmentCount);
    }
}
