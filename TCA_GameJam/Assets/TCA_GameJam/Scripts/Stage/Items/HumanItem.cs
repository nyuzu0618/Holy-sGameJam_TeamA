using System;
using TCA_GameJam.Scripts.Managers;
using TCA_GameJam.Scripts.Player;
using UnityEngine;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// なぎ倒す相手。ぶつかるとスコアが入って倒れる(倒れる動きは HumanAnimator)
    /// </summary>
    public class HumanItem : MonoBehaviour, IItem
    {
        [SerializeField] private float _knockedLifeTime = 3.0f;

        private GameManager _gameManager;
        private bool _isKnockedDown;

        public event Action OnKnockedDown;

        public bool IsKnockedDown => _isKnockedDown;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        public void Hit(SheepHerd herd)
        {
            if (_isKnockedDown || _gameManager.CurrentPhase != GamePhase.Bowling)
            {
                return;
            }

            _gameManager.AddScore(1);
            KnockDown();
        }

        /// <summary>
        /// 倒れたことにする(ObjectSpawner は IsKnockedDown を見て流すのをやめる)
        /// </summary>
        private void KnockDown()
        {
            _isKnockedDown = true;
            OnKnockedDown?.Invoke();
            Destroy(gameObject, _knockedLifeTime);
        }
    }
}
