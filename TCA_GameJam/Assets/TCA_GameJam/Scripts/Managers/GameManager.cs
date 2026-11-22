using System;
using UnityEngine;

namespace TCA_GameJam.Scripts.Managers
{
    /// <summary>
    /// フェーズの切り替え、スコア、ゲームオーバーを管理する
    /// </summary>
    [DefaultExecutionOrder(-100)]   // 他のスクリプトより先に Instance を用意する
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private float _scrollSpeed = 10.0f;        // 地面が流れる速さ
        [SerializeField] private float _collectDistance = 200.0f;   // この距離を走ったら合体する

        private GamePhase _currentPhase;
        private float _distance;
        private int _score;

        public event Action<GamePhase> OnPhaseChanged;
        public event Action<int> OnScoreChanged;

        public GamePhase CurrentPhase => _currentPhase;
        public int Score => _score;
        public float Distance => _distance;
        public bool IsPlaying => _currentPhase != GamePhase.GameOver;
        public float ScrollSpeed => IsPlaying ? _scrollSpeed : 0.0f;

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            StartCollectPhase();
        }

        private void Update()
        {
            if (_currentPhase != GamePhase.Collect)
            {
                return;
            }

            _distance += _scrollSpeed * Time.deltaTime;
            if (_distance >= _collectDistance)
            {
                StartBowlingPhase();
            }

            Debug.Log($"Score: {_score}");
        }

        /// <summary>
        /// 集めるフェーズを始める
        /// </summary>
        public void StartCollectPhase()
        {
            _distance = 0.0f;
            ChangePhase(GamePhase.Collect);
        }

        /// <summary>
        /// 合体して、なぎ倒しフェーズを始める
        /// </summary>
        public void StartBowlingPhase()
        {
            ChangePhase(GamePhase.Bowling);
        }

        /// <summary>
        /// なぎ倒しフェーズを終えて、集めるフェーズに戻る
        /// </summary>
        public void FinishBowling()
        {
            if (_currentPhase != GamePhase.Bowling)
            {
                return;
            }

            StartCollectPhase();
        }

        public void AddScore(int value)
        {
            _score += value;
            OnScoreChanged?.Invoke(_score);
        }

        public void EndGame()
        {
            if (!IsPlaying)
            {
                return;
            }

            ChangePhase(GamePhase.GameOver);
        }

        private void ChangePhase(GamePhase phase)
        {
            _currentPhase = phase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
