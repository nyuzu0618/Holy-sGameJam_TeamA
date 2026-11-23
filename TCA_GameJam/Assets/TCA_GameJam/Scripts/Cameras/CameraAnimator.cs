using DG.Tweening;
using TCA_GameJam.Scripts.Managers;
using UnityEngine;

namespace TCA_GameJam.Scripts.Cameras
{
    /// <summary>
    /// フェーズが変わったらカメラを後ろ⇔横へスーッと回り込ませる。人間を倒したら小さく揺らす
    /// (このスクリプトはカメラの親に付け、揺らすのは子のカメラだけにする)
    /// </summary>
    public class CameraAnimator : MonoBehaviour
    {
        [Header("回り込み")]
        [SerializeField] private Transform _backPoint;              // 集めるフェーズのカメラ位置・向き
        [SerializeField] private Transform _sidePoint;              // なぎ倒しフェーズのカメラ位置・向き
        [SerializeField] private float _moveDuration = 1.0f;
        [SerializeField] private Ease _moveEase = Ease.InOutSine;

        [Header("揺れ")]
        [SerializeField] private Transform _cameraTransform;        // 揺らすカメラ(このオブジェクトの子)
        [SerializeField] private float _shakeDuration = 0.2f;
        [SerializeField] private float _shakeStrength = 0.3f;
        [SerializeField] private int _shakeVibrato = 15;

        private GameManager _gameManager;
        private Sequence _moveSequence;
        private Tween _shakeTween;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            _gameManager.OnPhaseChanged += HandlePhaseChanged;
            _gameManager.OnScoreChanged += HandleScoreChanged;
        }

        private void OnDisable()
        {
            _gameManager.OnPhaseChanged -= HandlePhaseChanged;
            _gameManager.OnScoreChanged -= HandleScoreChanged;
        }

        private void Start()
        {
            // 最初は後ろから映す
            transform.SetPositionAndRotation(_backPoint.position, _backPoint.rotation);
        }

        private void OnDestroy()
        {
            _moveSequence?.Kill();
            _shakeTween?.Kill();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            // ゲームオーバーのときはそのままのカメラにしておく
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            Transform target = phase == GamePhase.Bowling ? _sidePoint : _backPoint;
            MoveTo(target);
        }

        /// <summary>
        /// スコアが増えた = 人間を倒したので小さく揺らす
        /// </summary>
        private void HandleScoreChanged(int score)
        {
            // 揺れている途中なら元の位置に戻してから揺らし直す
            _shakeTween?.Complete();
            _shakeTween = _cameraTransform.DOShakePosition(_shakeDuration, _shakeStrength, _shakeVibrato);
        }

        private void MoveTo(Transform target)
        {
            _moveSequence?.Kill();
            _moveSequence = DOTween.Sequence()
                .Join(transform.DOMove(target.position, _moveDuration).SetEase(_moveEase))
                .Join(transform.DORotateQuaternion(target.rotation, _moveDuration).SetEase(_moveEase));
        }
    }
}
