using TCA_GameJam.Scripts.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TCA_GameJam.Scripts.Player
{
    /// <summary>
    /// 羊の左右移動(前には進まない。地面のほうが流れる)
    /// </summary>
    public class SheepController : MonoBehaviour
    {
        [SerializeField] private InputActionReference _moveAction;  // InputSystem_Actions の Player/Move
        [SerializeField] private float _moveSpeed = 8.0f;
        [SerializeField] private float _moveRange = 4.0f;           // 中央から左右に動ける幅

        private GameManager _gameManager;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            _moveAction.action.Enable();
        }

        private void OnDisable()
        {
            _moveAction.action.Disable();
        }

        private void Update()
        {
            // 左右移動できるのは集めるフェーズだけ
            if (_gameManager.CurrentPhase != GamePhase.Collect)
            {
                return;
            }

            float inputX = _moveAction.action.ReadValue<Vector2>().x;
            Vector3 position = transform.position;
            position.x += inputX * _moveSpeed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, -_moveRange, _moveRange);
            transform.position = position;
        }
    }
}
