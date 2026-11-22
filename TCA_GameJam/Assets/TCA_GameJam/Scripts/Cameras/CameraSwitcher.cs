using TCA_GameJam.Scripts.Managers;
using UnityEngine;

namespace TCA_GameJam.Scripts.Cameras
{
    /// <summary>
    /// 集めるフェーズは後ろから、なぎ倒しフェーズは横から映す
    /// </summary>
    public class CameraSwitcher : MonoBehaviour
    {
        [SerializeField] private GameObject _backCamera;
        [SerializeField] private GameObject _sideCamera;

        private GameManager _gameManager;

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

        private void HandlePhaseChanged(GamePhase phase)
        {
            // ゲームオーバーのときはそのままのカメラにしておく
            if (phase == GamePhase.GameOver)
            {
                return;
            }

            bool isBowling = phase == GamePhase.Bowling;
            _backCamera.SetActive(!isBowling);
            _sideCamera.SetActive(isBowling);
        }
    }
}
