using TCA_GameJam.Scripts.Managers;
using TCA_GameJam.Scripts.Player;
using UnityEngine;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// 仲間になる羊。ぶつかると群れに加わる
    /// </summary>
    public class SheepItem : MonoBehaviour, IItem
    {
        private GameManager _gameManager;
        private bool _isHit;    // Colliderが複数あっても1回だけ処理する

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            // プールから出し直したときに、また当たれるようにする
            _isHit = false;
        }

        public void Hit(SheepHerd herd)
        {
            if (_isHit || _gameManager.CurrentPhase != GamePhase.Collect)
            {
                return;
            }

            _isHit = true;
            herd.AddFollower();
            gameObject.SetActive(false);    // 非表示にすると ObjectSpawner がプールに返す
        }
    }
}
