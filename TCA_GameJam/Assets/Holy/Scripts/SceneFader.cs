using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OutGame
{
    /// <summary>
    /// シーン開始時にフェードイン、シーン遷移前にフェードアウトを行う。
    /// 各シーンに1つ空のGameObjectを作ってアタッチするだけで動く
    /// （フェード用のCanvasとImageは実行時に自動生成される）。
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        [SerializeField, Tooltip("フェードの色")]
        private Color fadeColor = Color.black;

        [SerializeField, Tooltip("フェードインにかかる秒数")]
        private float fadeInDuration = 0.5f;

        [SerializeField, Tooltip("フェードアウトにかかる秒数")]
        private float fadeOutDuration = 0.5f;

        [SerializeField, Tooltip("フェード用Canvasの描画順（他のUIより大きくする）")]
        private int sortingOrder = 1000;

        public static SceneFader Instance { get; private set; }

        /// <summary>フェード中ならtrue</summary>
        public bool IsFading { get; private set; }

        private Image fadeImage;
        private bool isTransitioning;

        private void Awake()
        {
            Instance = this;
            CreateOverlay();
            SetAlpha(1f); // 最初は真っ黒から始める
        }

        private void Start()
        {
            StartCoroutine(Fade(1f, 0f, fadeInDuration));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// フェードアウトしてからシーンを読み込む。
        /// SceneFaderがシーンに無い場合はそのまま読み込む。
        /// </summary>
        public static void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneFader] シーン名が空です。Inspectorで遷移先を設定してください。");
                return;
            }

            if (Instance != null)
            {
                Instance.FadeOutAndLoad(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }

        public void FadeOutAndLoad(string sceneName)
        {
            if (isTransitioning) return; // 連打防止
            isTransitioning = true;
            StopAllCoroutines(); // フェードイン中に押された場合も途中から暗転させる
            StartCoroutine(FadeOutRoutine(sceneName));
        }

        private IEnumerator FadeOutRoutine(string sceneName)
        {
            yield return Fade(fadeImage.color.a, 1f, fadeOutDuration);
            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            IsFading = true;
            fadeImage.raycastTarget = true; // フェード中はクリックを受け付けない

            float time = 0f;
            while (time < duration)
            {
                // Time.timeScale = 0 でも動くように unscaled を使う
                time += Time.unscaledDeltaTime;
                SetAlpha(Mathf.Lerp(from, to, time / duration));
                yield return null;
            }
            SetAlpha(to);

            fadeImage.raycastTarget = to > 0f;
            IsFading = false;
        }

        private void CreateOverlay()
        {
            var canvasObj = new GameObject("FadeCanvas");
            canvasObj.transform.SetParent(transform, false);

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvasObj.AddComponent<GraphicRaycaster>();

            var imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);

            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = fadeColor;

            var rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void SetAlpha(float alpha)
        {
            var c = fadeColor;
            c.a = Mathf.Clamp01(alpha);
            fadeImage.color = c;
        }
    }
}
