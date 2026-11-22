using TCA_GameJam.Scripts.Managers;
using TCA_GameJam.Scripts.Player;
using UnityEngine;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// なぎ倒す相手。ぶつかるとスコアが入って吹っ飛ぶ
    /// </summary>
    public class HumanItem : MonoBehaviour, IItem
    {
        [SerializeField] private float _knockForce = 10.0f;
        [SerializeField] private float _knockedLifeTime = 3.0f;

        private GameManager _gameManager;
        private bool _isKnockedDown;

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
        /// 吹っ飛ばす(ObjectSpawner は IsKnockedDown を見て流すのをやめる)
        /// </summary>
        private void KnockDown()
        {
            _isKnockedDown = true;

            Rigidbody body = GetComponent<Rigidbody>();
            if (body == null)
            {
                body = gameObject.AddComponent<Rigidbody>();
            }
            body.isKinematic = false;
            body.useGravity = true;

            Vector3 direction = new Vector3(Random.Range(-0.5f, 0.5f), 1.0f, 1.0f).normalized;
            body.AddForce(direction * _knockForce, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * _knockForce, ForceMode.Impulse);
            Destroy(gameObject, _knockedLifeTime);
        }
    }
}
