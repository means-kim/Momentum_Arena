using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class kms99400_GameFlowController : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    [Tooltip("플레이어 낙사 이벤트를 수신할 컴포넌트입니다.")]
    private kms99400_PlayerFallDeath playerFallDeath;

    [SerializeField]
    [Tooltip("화면 전체를 덮는 실패 UI 루트입니다.")]
    private GameObject failPanel;

    [SerializeField]
    [Tooltip("실패 후 현재 씬을 재시작하는 버튼입니다.")]
    private Button restartButton;

    [SerializeField]
    [Tooltip("실패 시 비활성화할 3인칭 카메라 조작 스크립트입니다.")]
    private kms99400_ThirdPersonCamera thirdPersonCamera;

    public bool IsGameOver { get; private set; }

    private bool isRestarting;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (playerFallDeath == null || failPanel == null || restartButton == null || thirdPersonCamera == null)
        {
            Debug.LogError(
                $"{name}: kms99400_GameFlowController 참조가 올바르게 할당되지 않았습니다. " +
                $"(PlayerFallDeath: {playerFallDeath != null}, FailPanel: {failPanel != null}, " +
                $"RestartButton: {restartButton != null}, ThirdPersonCamera: {thirdPersonCamera != null})",
                this);
            enabled = false;
            return;
        }

        failPanel.SetActive(false);
        restartButton.interactable = true;
    }

    private void OnEnable()
    {
        if (playerFallDeath != null)
        {
            playerFallDeath.Died += HandlePlayerDied;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
    }

    private void OnDisable()
    {
        if (playerFallDeath != null)
        {
            playerFallDeath.Died -= HandlePlayerDied;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }
    }

    private void HandlePlayerDied(kms99400_PlayerFallDeath source)
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;

        failPanel.SetActive(true);
        restartButton.interactable = true;

        thirdPersonCamera.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        if (!IsGameOver || isRestarting)
        {
            return;
        }

        isRestarting = true;
        restartButton.interactable = false;

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex < 0)
        {
            Debug.LogError(
                $"{name}: 현재 씬이 활성 Build Profile의 Scene List에 등록되어 있지 않아 재시작할 수 없습니다.",
                this);

            isRestarting = false;
            restartButton.interactable = true;
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
