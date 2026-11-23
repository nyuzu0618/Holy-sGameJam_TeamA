using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TCA_GameJam.Scripts.Managers;
using TCA_GameJam.Scripts.Player;

namespace OutGame
{
    /// <summary>
    /// リザルトの「画像 ×値」1つ分の設定。
    /// </summary>
    [System.Serializable]
    public class ResultItem
    {
        [Tooltip("アイコンを表示する Image")]
        public Image iconImage;

        [Tooltip("アイコンに使う画像（空ならImageに設定済みのものをそのまま使う）")]
        public Sprite iconSprite;

        [Tooltip("「×99」を表示する TextMeshPro")]
        public TMP_Text countText;

        [Tooltip("表示する値（Show() やセッターで上書きされる。これは仮の値）")]
        public int value = 99;

        [Tooltip("数字の前に付ける文字")]
        public string prefix = "×";

        [SerializeField] private SheepHerd _sheepHerd;

        public void Apply()
        {
            if (iconImage != null && iconSprite != null) iconImage.sprite = iconSprite;
            if (countText != null) countText.text = prefix + value;
        }
    }

    /// <summary>
    /// リザルト画面（本編シーンの中に置くCanvas）。
    /// ・普段は非表示。Show(値1, 値2) で表示する
    /// ・TITLE ボタン：タイトルシーンへ
    /// ・RETRY ボタン：今のシーンを読み込み直してやり直し
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ResultManager : MonoBehaviour
    {
        [Header("背景")]
        [SerializeField, Tooltip("背景用の Image（画面全体に広げておく）")]
        private Image backgroundImage;

        [SerializeField, Tooltip("背景に使う画像（空なら下の色で画面を暗くする）")]
        private Sprite backgroundSprite;

        [SerializeField, Tooltip("背景画像が無いときの色")]
        private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        [Header("結果表示（画像 ×値）")]
        [SerializeField] private ResultItem value1Item = new ResultItem();
        [SerializeField] private ResultItem value2Item = new ResultItem();

        [Header("ボタン")]
        [SerializeField] private Button titleButton;
        [SerializeField] private Button retryButton;

        [Header("遷移先")]
        [SerializeField, Tooltip("TITLE ボタンで移動するシーン名")]
        private string titleSceneName = "title";

        [Header("表示")]
        [SerializeField, Tooltip("ゲーム開始時は隠しておく")]
        private bool hideOnStart = true;

        [SerializeField, Tooltip("表示するときのフェード秒数")]
        private float fadeDuration = 0.3f;

        [SerializeField] private SheepHerd _sheepHerd;

        private CanvasGroup canvasGroup;
        private Coroutine fadeRoutine;

        /// <summary>リザルトを表示中なら true</summary>
        public bool IsShowing { get; private set; }

        /// <summary>1つ目の「画像 ×値」の値</summary>
        public int Value1
        {
            get => value1Item.value;
            set { value1Item.value = value; value1Item.Apply(); }
        }

        /// <summary>2つ目の「画像 ×値」の値</summary>
        public int Value2
        {
            get => value2Item.value;
            set { value2Item.value = value; value2Item.Apply(); }
        }

        public void SetValue1(int value) => Value1 = value;
        public void SetValue2(int value) => Value2 = value;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            ApplyAll();

            GameManager.Instance.OnPhaseChanged += Show;

            if (titleButton != null) titleButton.onClick.AddListener(GoToTitle);
            if (retryButton != null) retryButton.onClick.AddListener(Retry);

            SetVisibleImmediate(!hideOnStart);
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPhaseChanged -= Show;
                Debug.LogWarning("購読解除完了");
            }
        }

        /// <summary>値をセットしてリザルトを表示する</summary>
        public void Show(GamePhase phase)
        {
            if (phase == GamePhase.Clear ||
            phase == GamePhase.GameOver)
            {
                Value1 = _sheepHerd.FollowerCount;
                Value2 = GameManager.Instance.Score;
                Show();

            }
        }

        /// <summary>今の値のままリザルトを表示する</summary>
        public void Show()
        {
            IsShowing = true;
            ApplyAll();
            StartFade(1f);
        }

        /// <summary>リザルトを隠す</summary>
        public void Hide()
        {
            IsShowing = false;
            StartFade(0f);
        }

        public void GoToTitle() => SceneFader.LoadScene(titleSceneName);

        /// <summary>今のシーンを読み込み直してリトライ</summary>
        public void Retry() => SceneFader.LoadScene(SceneManager.GetActiveScene().name);

        private void StartFade(float target)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeRoutine(target));
        }

        private IEnumerator FadeRoutine(float target)
        {
            // フェード中は押せない。表示し終わったらボタンを押せるようにする
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = target > 0f;

            float from = canvasGroup.alpha;
            float time = 0f;
            while (time < fadeDuration)
            {
                time += Time.unscaledDeltaTime; // timeScale = 0 でも動く
                canvasGroup.alpha = Mathf.Lerp(from, target, time / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = target;
            canvasGroup.interactable = target > 0f;
            fadeRoutine = null;
        }

        private void SetVisibleImmediate(bool visible)
        {
            IsShowing = visible;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void ApplyAll()
        {
            if (backgroundImage != null)
            {
                if (backgroundSprite != null)
                {
                    backgroundImage.sprite = backgroundSprite;
                    backgroundImage.color = Color.white;

                    var fitter = backgroundImage.GetComponent<AspectRatioFitter>();
                    if (fitter != null) fitter.aspectRatio = backgroundSprite.rect.width / backgroundSprite.rect.height;
                }
                else
                {
                    backgroundImage.sprite = null;
                    backgroundImage.color = backgroundColor;
                }
            }

            value1Item?.Apply();
            value2Item?.Apply();
        }

    #if UNITY_EDITOR
        // Inspectorで値を変えたらシーンビューにもすぐ反映する
        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) ApplyAll();
            };
        }
    #endif
    }
}
