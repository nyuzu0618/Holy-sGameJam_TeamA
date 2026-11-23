using System.Collections.Generic;
using TCA_GameJam.Scripts.Managers;
using UnityEngine;
using UnityEngine.Pool;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// 仲間の羊・狼・人間をプールから出して、地面と一緒に流す(ぶつかったときの処理は各 IItem が持つ)
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private GameObject _sheepPrefab;           // SheepItem と Collider(Is Trigger)付きの2D素材
        [SerializeField] private GameObject _wolfPrefab;            // WolfItem 付き
        [SerializeField] private GameObject _humanPrefab;           // HumanItem 付き
        [SerializeField] private float _spawnInterval = 1.0f;
        [SerializeField, Range(0.0f, 1.0f)] private float _wolfRate = 0.3f;    // 出てくるものが狼になる割合
        [SerializeField] private float _spawnRange = 4.0f;          // 中央から左右に出す幅
        [SerializeField] private float _spawnZ = 60.0f;
        [SerializeField] private float _despawnZ = -10.0f;
        [SerializeField] private float _humanStartZ = 30.0f;
        [SerializeField] private float _humanSpacing = 1.5f;        // 人間の三角形の、奥行きの間隔
        [SerializeField] private float _humanSpacingX = 1.2f;       // 人間の三角形の、横の間隔
        [SerializeField] private float _finishWaitTime = 1.5f;      // 最後の人間を倒してから集めるフェーズに戻るまでの秒数

        [Header("各アイテムのプールする数")]
        [SerializeField] private int _sheepPoolSize = 10;
        [SerializeField] private int _wolfPoolSize = 10;
        [SerializeField] private int _humanPoolSize = 50;           // 一度に出せる人間の上限にもなる

        private GameManager _gameManager;
        private ObjectPool<GameObject> _sheepPool;
        private ObjectPool<GameObject> _wolfPool;
        private ObjectPool<GameObject> _humanPool;
        private readonly Dictionary<GameObject, ObjectPool<GameObject>> _poolOfObject = new Dictionary<GameObject, ObjectPool<GameObject>>();   // 出したものをどのプールに返すか
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private readonly List<HumanItem> _humans = new List<HumanItem>();     // まだ倒されていない人間
        private float _spawnTimer;
        private float _finishTimer;

        private void Awake()
        {
            _gameManager = GameManager.Instance;

            _sheepPool = CreatePool(_sheepPrefab, _sheepPoolSize);
            _wolfPool = CreatePool(_wolfPrefab, _wolfPoolSize);
            _humanPool = CreatePool(_humanPrefab, _humanPoolSize);
        }

        private void OnEnable()
        {
            _gameManager.OnPhaseChanged += HandlePhaseChanged;
        }

        private void OnDisable()
        {
            _gameManager.OnPhaseChanged -= HandlePhaseChanged;
        }

        private void Update()
        {
            RemoveFinishedHumans();
            ScrollObjects();

            if (_gameManager.CurrentPhase == GamePhase.Collect)
            {
                _spawnTimer += Time.deltaTime;
                if (_spawnTimer >= _spawnInterval)
                {
                    _spawnTimer -= _spawnInterval;
                    SpawnCollectObject();
                }
            }
            else if (_gameManager.CurrentPhase == GamePhase.Bowling && _humans.Count == 0)
            {
                // 人間が全員倒れたか後ろに抜けたら、倒れる動きを見せるため少し待ってから終わる(0人のときもここで終わる)
                _finishTimer += Time.deltaTime;
                if (_finishTimer >= _finishWaitTime)
                {
                    _gameManager.FinishBowling();
                }
            }
        }

        /// <summary>
        /// なぎ倒す人間を、羊の正面にボーリングのピンのような三角形に並べる(プールの数より多いときは出せる分だけ)
        /// 羊に一番近い先頭が1人で、奥へ 2人→3人→… と増える。人数が足りない最後の列は真ん中寄せ
        /// </summary>
        public void SpawnHumans(int count)
        {
            ClearAll();

            int spawnCount = Mathf.Min(count, _humanPoolSize);
            int row = 0;
            int rowStartIndex = 0;     // 今の列の先頭が何人目か

            for (int i = 0; i < spawnCount; i++)
            {
                // 今の列(row 列目は row + 1 人)が埋まったら次の列へ
                if (i >= rowStartIndex + row + 1)
                {
                    rowStartIndex += row + 1;
                    row++;
                }

                int column = i - rowStartIndex;
                int rowSize = Mathf.Min(row + 1, spawnCount - rowStartIndex);
                float x = _playerTransform.position.x + (column - (rowSize - 1) * 0.5f) * _humanSpacingX;
                float z = _humanStartZ + row * _humanSpacing;

                Vector3 position = new Vector3(x, _humanPrefab.transform.position.y, z);
                GameObject spawned = Spawn(_humanPool, position);
                if (spawned == null)
                {
                    break;
                }

                HumanItem human = spawned.GetComponent<HumanItem>();
                if (human == null)
                {
                    Debug.LogWarning("Human Prefab に HumanItem が付いていません", _humanPrefab);
                    continue;
                }
                _humans.Add(human);
            }
        }

        /// <summary>
        /// プールを作って、最初に size 個ぜんぶ生成して非表示にしておく
        /// </summary>
        private ObjectPool<GameObject> CreatePool(GameObject prefab, int size)
        {
            ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                () => Instantiate(prefab, transform),
                null,                                   // 出すときの表示は位置を決めてから Spawn で行う
                target => target.SetActive(false),
                target => Destroy(target),
                true,
                size,
                size);

            List<GameObject> created = new List<GameObject>();
            for (int i = 0; i < size; i++)
            {
                GameObject target = pool.Get();
                _poolOfObject.Add(target, pool);
                created.Add(target);
            }
            foreach (GameObject target in created)
            {
                pool.Release(target);
            }
            return pool;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Collect)
            {
                ClearAll();
                _spawnTimer = 0.0f;
                _finishTimer = 0.0f;
            }
        }

        private void SpawnCollectObject()
        {
            bool isWolf = Random.value < _wolfRate;
            ObjectPool<GameObject> pool = isWolf ? _wolfPool : _sheepPool;
            GameObject prefab = isWolf ? _wolfPrefab : _sheepPrefab;
            Vector3 position = new Vector3(Random.Range(-_spawnRange, _spawnRange), prefab.transform.position.y, _spawnZ);
            Spawn(pool, position);
        }

        /// <summary>
        /// プールから1個出して置く。空いているものが無いときは出さずに null を返す
        /// </summary>
        private GameObject Spawn(ObjectPool<GameObject> pool, Vector3 position)
        {
            if (pool.CountInactive == 0)
            {
                return null;
            }

            GameObject spawned = pool.Get();
            spawned.transform.position = position;
            spawned.SetActive(true);
            _spawnedObjects.Add(spawned);
            return spawned;
        }

        /// <summary>
        /// 倒された人間・プールに戻った人間を数から外す(倒された人間も地面と一緒に流し続ける)
        /// </summary>
        private void RemoveFinishedHumans()
        {
            for (int i = _humans.Count - 1; i >= 0; i--)
            {
                HumanItem human = _humans[i];
                if (!human.gameObject.activeSelf)
                {
                    _humans.RemoveAt(i);
                }
                else if (human.IsKnockedDown)
                {
                    _humans.RemoveAt(i);
                }
            }
        }

        private void ScrollObjects()
        {
            float move = _gameManager.ScrollSpeed * Time.deltaTime;

            for (int i = _spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject target = _spawnedObjects[i];

                // ぶつかって自分で非表示になったものはプールに返す
                if (!target.activeSelf)
                {
                    _spawnedObjects.RemoveAt(i);
                    ReleaseToPool(target);
                    continue;
                }

                Vector3 position = target.transform.position;
                position.z -= move;
                target.transform.position = position;

                if (position.z < _despawnZ)
                {
                    _spawnedObjects.RemoveAt(i);
                    ReleaseToPool(target);
                }
            }
        }

        private void ReleaseToPool(GameObject target)
        {
            _poolOfObject[target].Release(target);
        }

        private void ClearAll()
        {
            foreach (GameObject target in _spawnedObjects)
            {
                ReleaseToPool(target);
            }
            _spawnedObjects.Clear();
            _humans.Clear();
        }
    }
}
