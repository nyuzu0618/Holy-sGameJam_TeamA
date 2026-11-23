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
        [SerializeField] private float _followerSpacing = 1.2f;
        [SerializeField] private float _woolBallBaseScale = 1.0f;
        [SerializeField] private float _woolBallScalePerSheep = 0.1f;

        // 後ろに見た目を出す仲間の数(これより多く集めても見た目は増えず、数だけ増える)
        private const int FOLLOWER_POOL_SIZE = 14;

        private GameManager _gameManager;
        private readonly List<GameObject> _followers = new List<GameObject>();     // 最初に作っておく仲間の見た目(FOLLOWER_POOL_SIZE 個)
        private int _followerCount;
        private int _mergedCount;
        private bool _isMerged;

        public event Action<IReadOnlyList<Vector3>> OnMerged;  // 合体した(引数は仲間がいた localPosition)

        public int FollowerCount => _followerCount;
        public int MergedCount => _mergedCount;
        public bool IsMerged => _isMerged;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
            GetComponent<Rigidbody>().isKinematic = true;
            _woolBall.gameObject.SetActive(false);
            CreateFollowers();
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
            _mergedCount = _followerCount;

            // 消す前に見えている仲間がいた場所を覚えておく(SheepMergeAnimator が集まる動きに使う)
            List<Vector3> followerLocalPositions = new List<Vector3>();
            foreach (GameObject follower in _followers)
            {
                if (follower.activeSelf)
                {
                    followerLocalPositions.Add(follower.transform.localPosition);
                }
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
            _followerCount++;
            UpdateFollowerVisuals();
        }

        /// <summary>
        /// 狼に当たったときの処理(WolfItem から呼ばれる)
        /// </summary>
        public void HitWolf()
        {
            // 仲間0で狼に当たったらゲームオーバー
            if (_followerCount == 0)
            {
                _gameManager.EndGame();
                return;
            }

            int lostCount = Mathf.Min(_lostCountOnWolf, _followerCount);
            _followerCount -= lostCount;
            UpdateFollowerVisuals();
        }

        /// <summary>
        /// 仲間の見た目を最初に全部作って、並ぶ位置に置いて非表示にしておく
        /// 自分を先頭のピンにして、後ろに 2匹→3匹→4匹 とボーリングのピンのような三角形に並べる
        /// </summary>
        private void CreateFollowers()
        {
            int totalCount = FOLLOWER_POOL_SIZE + 1;   // 先頭の自分も入れた数
            int row = 1;                               // 0列目は自分なので1列目から
            int rowStartIndex = 1;                     // 今の列の先頭が何匹目か(自分が0匹目)

            for (int i = 0; i < FOLLOWER_POOL_SIZE; i++)
            {
                int index = i + 1;

                // 今の列(row 列目は row + 1 匹)が埋まったら次の列へ
                if (index >= rowStartIndex + row + 1)
                {
                    rowStartIndex += row + 1;
                    row++;
                }

                int column = index - rowStartIndex;
                int rowSize = Mathf.Min(row + 1, totalCount - rowStartIndex);

                Vector3 localPos = new Vector3(
                    (column - (rowSize - 1) * 0.5f) * _followerSpacing,
                    _followerPrefab.transform.position.y,
                    -row * _followerSpacing);

                GameObject follower = Instantiate(_followerPrefab, transform);
                follower.transform.localPosition = localPos;
                follower.SetActive(false);
                _followers.Add(follower);
            }
        }

        /// <summary>
        /// 仲間の数に合わせて、前から順に見た目を出す(FOLLOWER_POOL_SIZE より多い分は出さない)
        /// </summary>
        private void UpdateFollowerVisuals()
        {
            for (int i = 0; i < _followers.Count; i++)
            {
                _followers[i].SetActive(i < _followerCount);
            }
        }

        private void ClearFollowers()
        {
            _followerCount = 0;
            UpdateFollowerVisuals();
        }
    }
}
