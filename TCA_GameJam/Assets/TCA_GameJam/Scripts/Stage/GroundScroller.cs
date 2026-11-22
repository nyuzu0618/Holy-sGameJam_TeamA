using TCA_GameJam.Scripts.Managers;
using UnityEngine;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// 2Dの地面を手前(-Z)に流して、後ろに抜けたら奥に戻す
    /// </summary>
    public class GroundScroller : MonoBehaviour
    {
        [SerializeField] private Transform[] _groundTiles;  // Z方向に _tileLength ずつ並べた地面
        [SerializeField] private float _tileLength = 20.0f;

        private GameManager _gameManager;

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        private void Update()
        {
            float move = _gameManager.ScrollSpeed * Time.deltaTime;
            float loopLength = _tileLength * _groundTiles.Length;

            foreach (Transform tile in _groundTiles)
            {
                Vector3 position = tile.position;
                position.z -= move;
                if (position.z < -_tileLength)
                {
                    position.z += loopLength;
                }
                tile.position = position;
            }
        }
    }
}
