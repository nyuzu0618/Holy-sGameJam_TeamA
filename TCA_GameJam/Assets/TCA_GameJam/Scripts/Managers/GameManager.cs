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
        [SerializeField] private float _collectTime = 60.0f;        // 集めるフェーズの秒数(この時間が経ったら合体する)

        private GamePhase _currentPhase;
        private float _collectTimer;
        public int _score { get; private set; }

        public event Action<GamePhase> OnPhaseChanged;
        public event Action<int> OnScoreChanged;

        public GamePhase CurrentPhase => _currentPhase;
        public int Score => _score;
        public bool IsPlaying => _currentPhase == GamePhase.Collect || _currentPhase == GamePhase.Bowling;
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

            _collectTimer += Time.deltaTime;
            if (_collectTimer >= _collectTime)
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
            _collectTimer = 0.0f;
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
        /// なぎ倒しフェーズを終えて、クリアにする(フェーズは1回で終わり)
        /// </summary>
        public void FinishBowling()
        {
            if (_currentPhase != GamePhase.Bowling)
            {
                return;
            }

            ChangePhase(GamePhase.Clear);
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
