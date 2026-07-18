using System.Collections.Generic;
using UnityEngine;

public class kms99400_EnemySpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField]
    [Tooltip("스폰할 적 프리팹입니다. 루트에 EnemyMovement와 EnemyFallDeath가 있어야 합니다.")]
    private GameObject enemyPrefab;

    [SerializeField]
    [Tooltip("스폰된 적이 추적할 Player 루트 Transform입니다.")]
    private Transform playerTarget;

    [SerializeField]
    [Tooltip("적을 스폰할 지점들입니다. 배열 순서대로 순차 스폰됩니다.")]
    private Transform[] spawnPoints;

    [Header("스폰 설정")]
    [SerializeField]
    [Min(1)]
    [Tooltip("이번 프로토타입에서 총 생성할 적의 수입니다.")]
    private int totalEnemyCount = 8;

    [SerializeField]
    [Min(1)]
    [Tooltip("동시에 살아있을 수 있는 최대 적 수입니다.")]
    private int maxAliveEnemies = 3;

    [SerializeField]
    [Min(0f)]
    [Tooltip("첫 스폰까지 대기하는 시간(초)입니다.")]
    private float initialSpawnDelay = 1f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("스폰 성공 후 다음 스폰까지의 간격(초)입니다.")]
    private float spawnInterval = 1f;

    public int SpawnedEnemyCount { get; private set; }
    public int AliveEnemyCount => activeEnemies.Count;
    public bool IsCompleted { get; private set; }

    public event System.Action AllEnemiesDefeated;

    private readonly HashSet<kms99400_EnemyFallDeath> activeEnemies = new HashSet<kms99400_EnemyFallDeath>();
    private float spawnTimer;
    private int nextSpawnPointIndex;

    private void Awake()
    {
        bool isEnemyPrefabAssigned = enemyPrefab != null;
        bool isPlayerTargetAssigned = playerTarget != null;
        bool areSpawnPointsValid = AreSpawnPointsValid();

        bool hasPrefabMovement = false;
        bool hasPrefabFallDeath = false;

        if (isEnemyPrefabAssigned)
        {
            hasPrefabMovement = enemyPrefab.TryGetComponent(out kms99400_EnemyMovement prefabMovement);
            hasPrefabFallDeath = enemyPrefab.TryGetComponent(out kms99400_EnemyFallDeath prefabFallDeath);
        }

        if (!isEnemyPrefabAssigned || !isPlayerTargetAssigned || !areSpawnPointsValid || !hasPrefabMovement || !hasPrefabFallDeath)
        {
            Debug.LogError(
                $"{name}: kms99400_EnemySpawner 설정이 올바르지 않습니다. " +
                $"(Enemy Prefab 할당: {isEnemyPrefabAssigned}, Player Target 할당: {isPlayerTargetAssigned}, " +
                $"Spawn Points 유효성: {areSpawnPointsValid}, Prefab EnemyMovement 존재: {hasPrefabMovement}, " +
                $"Prefab EnemyFallDeath 존재: {hasPrefabFallDeath})",
                this);
            enabled = false;
            return;
        }

        spawnTimer = initialSpawnDelay;
    }

    private bool AreSpawnPointsValid()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (IsCompleted)
        {
            return;
        }

        if (SpawnedEnemyCount >= totalEnemyCount)
        {
            return;
        }

        spawnTimer = Mathf.Max(0f, spawnTimer - Time.deltaTime);

        if (AliveEnemyCount >= maxAliveEnemies)
        {
            return;
        }

        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        Transform spawnPoint = spawnPoints[nextSpawnPointIndex];
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        bool hasMovement = enemyInstance.TryGetComponent(out kms99400_EnemyMovement enemyMovement);
        bool hasFallDeath = enemyInstance.TryGetComponent(out kms99400_EnemyFallDeath enemyFallDeath);

        if (!hasMovement || !hasFallDeath)
        {
            Debug.LogError(
                $"{name}: 스폰된 적 인스턴스의 루트에 필수 컴포넌트가 없습니다. " +
                $"(EnemyMovement 존재: {hasMovement}, EnemyFallDeath 존재: {hasFallDeath})",
                this);
            Destroy(enemyInstance);
            enabled = false;
            return;
        }

        enemyMovement.SetTarget(playerTarget);

        bool wasAdded = activeEnemies.Add(enemyFallDeath);
        if (!wasAdded)
        {
            Debug.LogError($"{name}: 새로 생성된 적이 이미 activeEnemies에 등록되어 있습니다. 예기치 않은 런타임 상태입니다.", this);
            Destroy(enemyInstance);
            enabled = false;
            return;
        }

        enemyFallDeath.Died += HandleEnemyDied;

        SpawnedEnemyCount++;
        nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Length;
        spawnTimer = spawnInterval;
    }

    private void HandleEnemyDied(kms99400_EnemyFallDeath enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.Died -= HandleEnemyDied;

        if (!activeEnemies.Remove(enemy))
        {
            return;
        }

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        if (IsCompleted)
        {
            return;
        }

        if (SpawnedEnemyCount < totalEnemyCount || activeEnemies.Count > 0)
        {
            return;
        }

        IsCompleted = true;
        AllEnemiesDefeated?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (kms99400_EnemyFallDeath enemy in activeEnemies)
        {
            if (enemy != null)
            {
                enemy.Died -= HandleEnemyDied;
            }
        }

        activeEnemies.Clear();
    }

    private void OnValidate()
    {
        totalEnemyCount = Mathf.Max(1, totalEnemyCount);
        maxAliveEnemies = Mathf.Clamp(maxAliveEnemies, 1, totalEnemyCount);
        initialSpawnDelay = Mathf.Max(0f, initialSpawnDelay);
        spawnInterval = Mathf.Max(0f, spawnInterval);
    }
}
