using DG.Tweening;
using TCA_GameJam.Scripts.Managers;
using UnityEngine;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// なぎ倒しフェーズの間は震えて、ぶつかったらその場でパタンと横たわる
    /// </summary>
    [RequireComponent(typeof(HumanItem))]
    public class HumanAnimator : MonoBehaviour
    {
        [SerializeField] private Transform _visual;                 // 動かす絵(スプライトの子オブジェクト。本体だと流す動きが消えるので震えない)

        [Header("震え")]
        [SerializeField] private float _shakeStrength = 0.05f;
        [SerializeField] private int _shakeVibrato = 30;
        [SerializeField] private float _shakeLoopDuration = 0.5f;   // 1回分の長さ(これをずっと繰り返す)

        [Header("横たわる")]
        [SerializeField] private Vector3 _lieDownAngle = new Vector3(90.0f, 0.0f, 0.0f);   // 倒れたときの回転(今の向きに足す)
        [SerializeField] private float _lieDownDuration = 0.3f;
        [SerializeField] private Ease _lieDownEase = Ease.OutBounce;

        private GameManager _gameManager;
        private HumanItem _humanItem;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private Tween _shakeTween;
        private bool _canShake;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
            _humanItem = GetComponent<HumanItem>();

            // 本体を揺らすと ObjectSpawner が流す位置を毎フレーム上書きしてしまうので、子のときだけ震える
            _canShake = _visual != null && _visual != transform;
            if (!_canShake)
            {
                Debug.LogWarning("HumanAnimator の _visual にスプライトの子オブジェクトをつないでください(震えなくなります)", this);
                _visual = transform;
            }
            _baseLocalPosition = _visual.localPosition;
            _baseLocalRotation = _visual.localRotation;
        }

        private void OnEnable()
        {
            _gameManager.OnPhaseChanged += HandlePhaseChanged;
            _humanItem.OnKnockedDown += HandleKnockedDown;

            // プールから出し直したときは、倒れた向きを戻してから始める
            _visual.localRotation = _baseLocalRotation;

            // 人間はなぎ倒しフェーズに入ってから出てくるので、出てきた時点で震え始める
            if (_gameManager.CurrentPhase == GamePhase.Bowling)
            {
                StartShake();
            }
        }

        private void OnDisable()
        {
            _gameManager.OnPhaseChanged -= HandlePhaseChanged;
            _humanItem.OnKnockedDown -= HandleKnockedDown;

            // プールに戻るときは、途中の震え・倒れる動きを止める
            StopShake();
            _visual.DOKill();
        }

        private void OnDestroy()
        {
            _visual.DOKill();
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Bowling && !_humanItem.IsKnockedDown)
            {
                StartShake();
            }
            else
            {
                StopShake();
            }
        }

        private void HandleKnockedDown()
        {
            StopShake();

            Vector3 angle = _visual.localEulerAngles + _lieDownAngle;
            _visual.DOLocalRotate(angle, _lieDownDuration).SetEase(_lieDownEase);
        }

        private void StartShake()
        {
            if (!_canShake || (_shakeTween != null && _shakeTween.IsActive()))
            {
                return;
            }

            // fadeOut を false にして、弱まらずにずっと同じ強さで震える
            Vector3 strength = new Vector3(_shakeStrength, _shakeStrength * 0.5f, 0.0f);
            _shakeTween = _visual.DOShakePosition(_shakeLoopDuration, strength, _shakeVibrato, 90.0f, false, false)
                .SetLoops(-1)
                .SetLink(gameObject);
        }

        private void StopShake()
        {
            _shakeTween?.Kill();
            _shakeTween = null;

            if (_canShake)
            {
                _visual.localPosition = _baseLocalPosition;
            }
        }
    }
}
