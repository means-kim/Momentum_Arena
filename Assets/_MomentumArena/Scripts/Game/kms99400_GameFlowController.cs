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
    [Tooltip("모든 적 처치 완료 이벤트를 수신할 스포너입니다.")]
    private kms99400_EnemySpawner enemySpawner;

    [SerializeField]
    [Tooltip("결과 확정 시 플레이어 이동을 막을 컴포넌트입니다.")]
    private kms99400_PlayerController playerController;

    [SerializeField]
    [Tooltip("결과 확정 시 플레이어 공격을 막을 컴포넌트입니다.")]
    private kms99400_PlayerAttack playerAttack;

    [SerializeField]
    [Tooltip("화면 전체를 덮는 실패 UI 루트입니다.")]
    private GameObject failPanel;

    [SerializeField]
    [Tooltip("실패 후 현재 씬을 재시작하는 버튼입니다.")]
    private Button restartButton;

    [SerializeField]
    [Tooltip("화면 전체를 덮는 클리어 UI 루트입니다.")]
    private GameObject clearPanel;

    [SerializeField]
    [Tooltip("클리어 후 현재 씬을 재시작하는 버튼입니다.")]
    private Button clearRestartButton;

    [SerializeField]
    [Tooltip("실패 시 비활성화할 3인칭 카메라 조작 스크립트입니다.")]
    private kms99400_ThirdPersonCamera thirdPersonCamera;

    private enum GameState
    {
        Playing,
        Cleared,
        Failed
    }

    private GameState currentState = GameState.Playing;

    public bool IsGameOver => currentState != GameState.Playing;
    public bool IsCleared => currentState == GameState.Cleared;
    public bool IsFailed => currentState == GameState.Failed;

    private bool isRestarting;

    private void Awake()
    {
        Time.timeScale = 1f;

        bool isPlayerFallDeathAssigned = playerFallDeath != null;
        bool isEnemySpawnerAssigned = enemySpawner != null;
        bool isPlayerControllerAssigned = playerController != null;
        bool isPlayerAttackAssigned = playerAttack != null;
        bool isFailPanelAssigned = failPanel != null;
        bool isRestartButtonAssigned = restartButton != null;
        bool isClearPanelAssigned = clearPanel != null;
        bool isClearRestartButtonAssigned = clearRestartButton != null;
        bool isThirdPersonCameraAssigned = thirdPersonCamera != null;

        if (!isPlayerFallDeathAssigned || !isEnemySpawnerAssigned || !isPlayerControllerAssigned ||
            !isPlayerAttackAssigned || !isFailPanelAssigned || !isRestartButtonAssigned ||
            !isClearPanelAssigned || !isClearRestartButtonAssigned || !isThirdPersonCameraAssigned)
        {
            Debug.LogError(
                $"{name}: kms99400_GameFlowController 참조가 올바르게 할당되지 않았습니다. " +
                $"(PlayerFallDeath: {isPlayerFallDeathAssigned}, EnemySpawner: {isEnemySpawnerAssigned}, " +
                $"PlayerController: {isPlayerControllerAssigned}, PlayerAttack: {isPlayerAttackAssigned}, " +
                $"FailPanel: {isFailPanelAssigned}, RestartButton: {isRestartButtonAssigned}, " +
                $"ClearPanel: {isClearPanelAssigned}, ClearRestartButton: {isClearRestartButtonAssigned}, " +
                $"ThirdPersonCamera: {isThirdPersonCameraAssigned})",
                this);
            enabled = false;
            return;
        }

        currentState = GameState.Playing;
        isRestarting = false;

        failPanel.SetActive(false);
        clearPanel.SetActive(false);

        restartButton.interactable = true;
        clearRestartButton.interactable = true;
    }

    private void OnEnable()
    {
        if (playerFallDeath != null)
        {
            playerFallDeath.Died += HandlePlayerDied;
        }

        if (enemySpawner != null)
        {
            enemySpawner.AllEnemiesDefeated += HandleAllEnemiesDefeated;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }

        if (clearRestartButton != null)
        {
            clearRestartButton.onClick.AddListener(RestartGame);
        }
    }

    private void OnDisable()
    {
        if (playerFallDeath != null)
        {
            playerFallDeath.Died -= HandlePlayerDied;
        }

        if (enemySpawner != null)
        {
            enemySpawner.AllEnemiesDefeated -= HandleAllEnemiesDefeated;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(RestartGame);
        }

        if (clearRestartButton != null)
        {
            clearRestartButton.onClick.RemoveListener(RestartGame);
        }
    }

    private void HandlePlayerDied(kms99400_PlayerFallDeath source)
    {
        FinishGame(GameState.Failed, failPanel, restartButton);
    }

    private void HandleAllEnemiesDefeated()
    {
        FinishGame(GameState.Cleared, clearPanel, clearRestartButton);
    }

    private void FinishGame(GameState result, GameObject resultPanel, Button resultButton)
    {
        if (currentState != GameState.Playing)
        {
            return;
        }

        currentState = result;

        playerController.SetMovementEnabled(false);
        playerAttack.SetAttackEnabled(false);

        resultPanel.SetActive(true);
        resultButton.interactable = true;

        thirdPersonCamera.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 0f;
    }

    private void RestartGame()
    {
        if (currentState == GameState.Playing || isRestarting)
        {
            return;
        }

        isRestarting = true;
        restartButton.interactable = false;
        clearRestartButton.interactable = false;

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex < 0)
        {
            Debug.LogError(
                $"{name}: 현재 씬이 활성 Build Profile의 Scene List에 등록되어 있지 않아 재시작할 수 없습니다.",
                this);

            isRestarting = false;
            restartButton.interactable = true;
            clearRestartButton.interactable = true;
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
