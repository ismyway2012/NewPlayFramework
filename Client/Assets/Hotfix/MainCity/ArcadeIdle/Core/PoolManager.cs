using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NewPlay.ArcadeIdle
{
    public class PoolManager : MonoBehaviour
    {
        public static PoolManager Instance { get; private set; }

        [System.Serializable]
        public class Pool
        {
            [SerializeField, Tooltip("The prefab to pool")]
            private GameObject prefab;

            [SerializeField, Tooltip("The number of objects to pool")]
            private int size;

            public GameObject Prefab => prefab; // The prefab to instantiate for the pool
            public int Size => size; // The number of objects in the pool
        }

        [SerializeField] private List<Pool> pools; // List of pools to initialize
        private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>(); // Dictionary to store object pools by prefab name

        private void Awake()
        {
            Instance = this;
            InitializePools();
        }

        private void Start()
        {
        }

        /// <summary>
        /// Creates and initializes object pools for each pool defined in the pools list.
        /// Objects are instantiated, deactivated, and added to the pool.
        /// </summary>
        private void InitializePools()
        {
            foreach (Pool pool in pools)
            {
                Queue<GameObject> objectPool = new Queue<GameObject>();

                for (int i = 0; i < pool.Size; i++)
                {
                    GameObject obj = Instantiate(pool.Prefab, transform);
                    obj.name = pool.Prefab.name;
                    obj.SetActive(false);
                    objectPool.Enqueue(obj);
                }

                poolDictionary.Add(pool.Prefab.name, objectPool);
            }
        }

        /// <summary>
        /// Spawns an object from the pool by its prefab name. If the pool is empty, a new object is instantiated.
        /// </summary>
        /// <param name="prefabName">The name of the prefab to spawn.</param>
        /// <returns>The spawned GameObject or null if the pool does not exist.</returns>
        public GameObject SpawnObject(string prefabName)
        {
            if (!poolDictionary.ContainsKey(prefabName))
            {
                Debug.LogWarning("Pool with name " + prefabName + " doesn't exist!");
                return null;
            }

            Queue<GameObject> objectPool = poolDictionary[prefabName];

            if (objectPool.Count == 0)
            {
                // Debugging message for empty pool, consider increasing pool size.
                Debug.LogWarning("Instantiating " + prefabName + " because pool is empty! Consider increasing initial pool size.");

                // Instantiate a new object if the pool is empty
                GameObject newObj = Instantiate(pools.FirstOrDefault(x => x.Prefab.name == prefabName).Prefab, transform);
                newObj.name = prefabName;
                return newObj;
            }

            // Get an object from the pool and activate it
            GameObject obj = objectPool.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Returns an object back to the pool, deactivating it before adding it back.
        /// </summary>
        /// <param name="obj">The object to return to the pool.</param>
        public void ReturnObject(GameObject obj)
        {
            obj.SetActive(false); // Deactivate the object before returning it to the pool
            string prefabName = obj.name;

            // Add the object back to the pool if the pool exists
            if (poolDictionary.ContainsKey(prefabName))
            {
                poolDictionary[prefabName].Enqueue(obj);
            }
            else
            {
                Debug.LogWarning("No pool found for object: " + prefabName);
            }
        }
    }
}
