using UnityEngine;

namespace OutGame
{
    /// <summary>
    /// オブジェクトを拡大・縮小し続ける（タイトルの「スタート」テキスト用）。
    /// TextMeshPro - Text (UI) のオブジェクトにアタッチする。
    /// </summary>
    public class PulseScale : MonoBehaviour
    {
        [SerializeField, Tooltip("一番小さいときの倍率")]
        private float minScale = 0.9f;

        [SerializeField, Tooltip("一番大きいときの倍率")]
        private float maxScale = 1.1f;

        [SerializeField, Tooltip("1秒あたりの拡大縮小の回数")]
        private float cyclesPerSecond = 1f;

        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void Update()
        {
            // sin波で 0～1 を往復させ、min～max の倍率に変換する
            float t = (Mathf.Sin(Time.unscaledTime * cyclesPerSecond * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, t);
        }
    }
}
