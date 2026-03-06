using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace NewPlay.ArcadeIdle
{
    [DisallowMultipleComponent]
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField, Min(1)]
        private int maxMonsters = 100;

        [SerializeField, Min(0)]
        private int initialSpawn = 16;

        [SerializeField, Min(1), Tooltip("How many monsters to spawn per frame during initial burst.")]
        private int initialSpawnPerFrame = 4;

        [SerializeField, Min(0.1f)]
        private float spawnInterval = 10f;

        [SerializeField, Min(1), Tooltip("How many monsters to spawn each interval.")]
        private int spawnPerInterval = 1;

        [SerializeField, Min(1), Tooltip("How many monsters to spawn per frame during interval spawn.")]
        private int spawnPerFrame = 2;

        [SerializeField]
        private CampType campType = CampType.Enemy;

        [SerializeField]
        private string[] poolPrefabs = {"Enemy_1001", "Enemy_1002", "Enemy_1003"};

        [Header("Battle Zone")]
        [SerializeField]
        private string battleAreaName = "Battle Zone";

        [SerializeField, Min(1)]
        private int maxSpawnAttempts = 20;

        private int battleAreaIndex = -1;
        private int battleAreaMask = NavMesh.AllAreas;
        private Coroutine spawnRoutine;
        private readonly HashSet<MonsterSpawnToken> activeMonsters = new HashSet<MonsterSpawnToken>();

        void Start()
        {
            CacheBattleArea();
            spawnRoutine = StartCoroutine(SpawnLoop());
        }

        void OnDisable()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
                spawnRoutine = null;
            }
        }

        private void CacheBattleArea()
        {
            battleAreaIndex = NavMesh.GetAreaFromName(battleAreaName);
            if (battleAreaIndex >= 0)
            {
                battleAreaMask = 1 << battleAreaIndex;
            }
            else
            {
                battleAreaMask = NavMesh.AllAreas;
                Debug.LogWarning($"MonsterSpawner: NavMesh area '{battleAreaName}' not found. Falling back to AllAreas.");
            }
        }

        private IEnumerator SpawnLoop()
        {
            yield return null;
            yield return null;
            yield return SpawnBatch(initialSpawn, initialSpawnPerFrame);
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);

                if (activeMonsters.Count < maxMonsters)
                {
                    yield return SpawnBatch(spawnPerInterval, spawnPerFrame);
                }
            }
        }

        private IEnumerator SpawnBatch(int targetCount, int perFrame)
        {
            if (targetCount <= 0)
            {
                yield break;
            }

            int remaining = Mathf.Min(targetCount, maxMonsters - activeMonsters.Count);
            int spawned = 0;
            int spawnedThisFrame = 0;
            int safety = remaining * 5;

            while (spawned < remaining && safety-- > 0)
            {
                if (activeMonsters.Count >= maxMonsters)
                {
                    yield break;
                }

                if (SpawnOne())
                {
                    spawned++;
                }

                spawnedThisFrame++;
                if (spawnedThisFrame >= perFrame)
                {
                    spawnedThisFrame = 0;
                    yield return null;
                }
            }
        }

        private bool SpawnOne()
        {
            if (activeMonsters.Count >= maxMonsters)
            {
                return false;
            }

            if (!TryGetSpawnPoint(out Vector3 pos))
            {
                Debug.LogWarning("MonsterSpawner: Failed to find spawn point in Battle Zone.");
                return false;
            }

            if (PoolManager.Instance == null)
            {
                Debug.LogWarning("MonsterSpawner: PoolManager not ready.");
                return false;
            }

            var go = PoolManager.Instance.SpawnObject(poolPrefabs[UnityEngine.Random.Range(0, poolPrefabs.Length)]);
            if (go == null)
            {
                return false;
            }

            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            go.layer = LayerMask.NameToLayer("Enemy");

            var fighter = go.GetComponentInChildren<FighterController>();
            if (fighter != null)
            {
                fighter.CampType = campType;

                var agent = fighter.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.areaMask = battleAreaMask;
                    agent.enabled = true;
                    agent.Warp(pos);
                    agent.isStopped = false;
                }
                fighter.Relive();
            }

            var token = go.GetComponent<MonsterSpawnToken>();
            if (token == null)
            {
                token = go.AddComponent<MonsterSpawnToken>();
            }
            token.Attach(this);
            activeMonsters.Add(token);

            return true;
        }

        internal void NotifyDespawn(MonsterSpawnToken token)
        {
            if (token == null)
            {
                return;
            }
            activeMonsters.Remove(token);
        }

        private bool TryGetSpawnPoint(out Vector3 point)
        {
            return TrySampleAround(transform.position, 20f, out point);
        }

        private bool TrySampleAround(Vector3 center, float radius, out Vector3 point)
        {
            for (int i = 0; i < maxSpawnAttempts; i++)
            {
                Vector3 random = center + Random.insideUnitSphere * radius;
                random.y = center.y;

                if (NavMesh.SamplePosition(random, out NavMeshHit hit, radius, battleAreaMask))
                {
                    point = hit.position;
                    return true;
                }
            }

            point = Vector3.zero;
            return false;
        }

    }

    public sealed class MonsterSpawnToken : MonoBehaviour
    {
        private MonsterSpawner spawner;

        public void Attach(MonsterSpawner owner)
        {
            spawner = owner;
        }

        private void OnDisable()
        {
            if (spawner != null)
            {
                spawner.NotifyDespawn(this);
            }
        }
    }
}
