using TCA_GameJam.Scripts.Managers;
using TMPro;
using UnityEngine;

namespace TCA_GameJam.Scripts.UI
{
    /// <summary>
    /// クリアしたら結果パネルを出して、スコア(倒した人数)を表示する
    /// </summary>
    public class ResultUI : MonoBehaviour
    {
        [SerializeField] private GameObject _resultPanel;      // 最初は非表示にしておくパネル
        [SerializeField] private TMP_Text _scoreText;

        private GameManager _gameManager;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
            _resultPanel.SetActive(false);
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
            if (phase != GamePhase.Clear)
            {
                return;
            }

            _scoreText.text = $"SCORE: {_gameManager.Score}";
            _resultPanel.SetActive(true);
        }
    }
}
