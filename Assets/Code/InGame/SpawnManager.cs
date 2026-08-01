using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Limit")]
    [SerializeField] private int maxEnemyCount = 30; // 필드 내 최대 적 개수

    [Header("Spawn Settings")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Vector2 spawnAreaMin;
    [SerializeField] private Vector2 spawnAreaMax;
    public Vector2 SpawnAreaMin => spawnAreaMin;
    public Vector2 SpawnAreaMax => spawnAreaMax;
    [Header("Spawn Overlap Check")]
    [SerializeField] private float checkRadius = 0.8f;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private int maxAttemptCount = 15;

    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnDelay = 1.0f;
    [SerializeField] private float maxSpawnDelay = 3.0f;

    [Header("Spawn Count")]
    [SerializeField] private int minSpawnAmount = 1;
    [SerializeField] private int maxSpawnAmount = 5;

    private float timer;
    private float nextSpawnTime;

    void Start()
    {
        SetNextSpawnTime();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextSpawnTime)
        {
            SpawnEnemies();
            timer = 0f;
            SetNextSpawnTime();
        }
    }

    private void SetNextSpawnTime()
    {
        nextSpawnTime = Random.Range(minSpawnDelay, maxSpawnDelay);
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        // 현재 필드에 존재하는 적의 개수를 확인 (태그 기준)
        int currentEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

        // 이미 30마리 이상이면 스폰하지 않고 종료
        if (currentEnemyCount >= maxEnemyCount) return;

        // 남은 스폰 가능한 여유 개수 계산
        int availableSlots = maxEnemyCount - currentEnemyCount;
        int targetSpawnCount = Random.Range(minSpawnAmount, maxSpawnAmount + 1);

        // 스폰하려는 개수가 남은 슬롯보다 많으면 남은 슬롯 수만큼만 스폰
        int spawnCount = Mathf.Min(targetSpawnCount, availableSlots);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 validSpawnPosition;
            if (TryGetValidSpawnPosition(out validSpawnPosition))
            {
                int randomEnemyIndex = Random.Range(0, enemyPrefabs.Length);
                GameObject selectedPrefab = enemyPrefabs[randomEnemyIndex];

                Instantiate(selectedPrefab, validSpawnPosition, Quaternion.identity);
            }
        }
    }

    private bool TryGetValidSpawnPosition(out Vector3 spawnPosition)
    {
        for (int attempt = 0; attempt < maxAttemptCount; attempt++)
        {
            float randomX = Random.Range(spawnAreaMin.x, spawnAreaMax.x);
            float randomY = Random.Range(spawnAreaMin.y, spawnAreaMax.y);
            Vector2 randomPoint = new Vector2(randomX, randomY);

            Collider2D hit = Physics2D.OverlapCircle(randomPoint, checkRadius, obstacleLayer);

            if (hit == null)
            {
                spawnPosition = new Vector3(randomPoint.x, randomPoint.y, 0f);
                return true;
            }
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = new Vector3((spawnAreaMin.x + spawnAreaMax.x) / 2f, (spawnAreaMin.y + spawnAreaMax.y) / 2f, 0f);
        Vector3 size = new Vector3(Mathf.Abs(spawnAreaMax.x - spawnAreaMin.x), Mathf.Abs(spawnAreaMax.y - spawnAreaMin.y), 1f);
        Gizmos.DrawWireCube(center, size);
    }
}