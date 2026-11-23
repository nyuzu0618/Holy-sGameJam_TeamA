using System.Collections.Generic;
using TCA_GameJam.Scripts.Managers;
using UnityEngine;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// 仲間の羊・狼・人間を出して、地面と一緒に流す(ぶつかったときの処理は各 IItem が持つ)
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
        [SerializeField] private float _humanSpacing = 1.5f;
        [SerializeField] private float _finishWaitTime = 1.5f;      // 最後の人間を倒してから集めるフェーズに戻るまでの秒数

        private GameManager _gameManager;
        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();
        private readonly List<HumanItem> _humans = new List<HumanItem>();     // まだ倒されていない人間
        private float _spawnTimer;
        private float _finishTimer;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
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
        /// なぎ倒す人間を、羊の正面に一列に並べる
        /// </summary>
        public void SpawnHumans(int count)
        {
            ClearAll();

            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3(_playerTransform.position.x, 0.0f, _humanStartZ + i * _humanSpacing);
                GameObject spawned = Spawn(_humanPrefab, position);

                HumanItem human = spawned.GetComponent<HumanItem>();
                if (human == null)
                {
                    Debug.LogWarning("Human Prefab に HumanItem が付いていません", _humanPrefab);
                    continue;
                }
                _humans.Add(human);
            }
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
            GameObject prefab = Random.value < _wolfRate ? _wolfPrefab : _sheepPrefab;
            Vector3 position = new Vector3(Random.Range(-_spawnRange, _spawnRange), 0.0f, _spawnZ);
            Spawn(prefab, position);
        }

        private GameObject Spawn(GameObject prefab, Vector3 position)
        {
            GameObject spawned = Instantiate(prefab, position, prefab.transform.rotation, transform);
            _spawnedObjects.Add(spawned);
            return spawned;
        }

        /// <summary>
        /// 倒された人間・消えた人間を数から外す(倒された人間も地面と一緒に流し続ける)
        /// </summary>
        private void RemoveFinishedHumans()
        {
            for (int i = _humans.Count - 1; i >= 0; i--)
            {
                HumanItem human = _humans[i];
                if (human == null)
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

                // ぶつかって自分で消えたものはリストから外すだけ
                if (target == null)
                {
                    _spawnedObjects.RemoveAt(i);
                    continue;
                }

                Vector3 position = target.transform.position;
                position.z -= move;
                target.transform.position = position;

                if (position.z < _despawnZ)
                {
                    _spawnedObjects.RemoveAt(i);
                    Destroy(target);
                }
            }
        }

        private void ClearAll()
        {
            foreach (GameObject target in _spawnedObjects)
            {
                if (target != null)
                {
                    Destroy(target);
                }
            }
            _spawnedObjects.Clear();
            _humans.Clear();
        }
    }
}
