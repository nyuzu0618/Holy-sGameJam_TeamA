using System;
using System.Collections.Generic;
using TCA_GameJam.Scripts.Managers;
using TCA_GameJam.Scripts.Stage;
using UnityEngine;

namespace TCA_GameJam.Scripts.Player
{
    /// <summary>
    /// 仲間の羊の数の管理と、毛玉への合体
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class SheepHerd : MonoBehaviour
    {
        [SerializeField] private ObjectSpawner _objectSpawner;
        [SerializeField] private GameObject _sheepModel;            // 先頭の羊(自分)の見た目
        [SerializeField] private GameObject _followerPrefab;        // 後ろについてくる仲間の見た目(Colliderは付けない)
        [SerializeField] private Transform _woolBall;               // 合体後の毛玉(半径0.5の球を想定)
        [SerializeField] private int _lostCountOnWolf = 1;          // 狼に当たったときに減る仲間の数
        [SerializeField] private int _followersPerRow = 3;          // 仲間を横に何匹ずつ並べるか
        [SerializeField] private float _followerSpacing = 1.2f;
        [SerializeField] private float _woolBallBaseScale = 1.0f;
        [SerializeField] private float _woolBallScalePerSheep = 0.1f;

        private GameManager _gameManager;
        private readonly List<GameObject> _followers = new List<GameObject>();
        private int _mergedCount;
        private bool _isMerged;

        public event Action<IReadOnlyList<Vector3>> OnMerged;  // 合体した(引数は仲間がいた localPosition)

        public int FollowerCount => _followers.Count;
        public int MergedCount => _mergedCount;
        public bool IsMerged => _isMerged;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
            GetComponent<Rigidbody>().isKinematic = true;
            _woolBall.gameObject.SetActive(false);
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
            if (!_isMerged)
            {
                return;
            }

            // 地面の流れる速さに合わせて毛玉を転がす
            float radius = _woolBall.localScale.x * 0.5f;
            float angle = _gameManager.ScrollSpeed / radius * Mathf.Rad2Deg * Time.deltaTime;
            _woolBall.Rotate(Vector3.right, angle, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_gameManager.IsPlaying)
            {
                return;
            }

            // ぶつかったものの処理は各アイテムに任せる(子のColliderでもOK)
            IItem item = other.GetComponentInParent<IItem>();
            if (item != null)
            {
                item.Hit(this);
            }
        }

        /// <summary>
        /// 仲間を全部まとめて毛玉にする
        /// </summary>
        public void Merge()
        {
            _mergedCount = _followers.Count;

            // 消す前に仲間がいた場所を覚えておく(SheepMergeAnimator が集まる動きに使う)
            List<Vector3> followerLocalPositions = new List<Vector3>();
            foreach (GameObject follower in _followers)
            {
                followerLocalPositions.Add(follower.transform.localPosition);
            }
            ClearFollowers();

            float scale = _woolBallBaseScale + _woolBallScalePerSheep * _mergedCount;
            _woolBall.localScale = Vector3.one * scale;
            _woolBall.gameObject.SetActive(true);
            _sheepModel.SetActive(false);
            _isMerged = true;

            OnMerged?.Invoke(followerLocalPositions);
        }

        /// <summary>
        /// 毛玉を解いて、仲間0の羊1匹に戻す
        /// </summary>
        public void ResetHerd()
        {
            ClearFollowers();
            _mergedCount = 0;
            _woolBall.gameObject.SetActive(false);
            _sheepModel.SetActive(true);
            _isMerged = false;
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Collect:
                    ResetHerd();
                    break;

                case GamePhase.Bowling:
                    Merge();
                    // 合体できた数 = 倒せる人数
                    _objectSpawner.SpawnHumans(_mergedCount);
                    break;
            }
        }

        /// <summary>
        /// 仲間を1匹増やす(SheepItem から呼ばれる)
        /// </summary>
        public void AddFollower()
        {
            int index = _followers.Count;
            int row = index / _followersPerRow;
            int column = index % _followersPerRow;

            Vector3 localPos = new Vector3(
                (column - (_followersPerRow - 1) * 0.5f) * _followerSpacing,
                0.0f,
                -(row + 1) * _followerSpacing);

            GameObject follower = Instantiate(_followerPrefab, transform);
            follower.transform.localPosition = localPos;
            _followers.Add(follower);
        }

        /// <summary>
        /// 狼に当たったときの処理(WolfItem から呼ばれる)
        /// </summary>
        public void HitWolf()
        {
            // 仲間0で狼に当たったらゲームオーバー
            if (_followers.Count == 0)
            {
                _gameManager.EndGame();
                return;
            }

            int lostCount = Mathf.Min(_lostCountOnWolf, _followers.Count);
            for (int i = 0; i < lostCount; i++)
            {
                int lastIndex = _followers.Count - 1;
                Destroy(_followers[lastIndex]);
                _followers.RemoveAt(lastIndex);
            }
        }

        private void ClearFollowers()
        {
            foreach (GameObject follower in _followers)
            {
                Destroy(follower);
            }
            _followers.Clear();
        }
    }
}
