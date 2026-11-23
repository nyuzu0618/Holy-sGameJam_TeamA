using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace OutGame
{
    /// <summary>
    /// タイトル画面：何かキー（キーボード／マウス／ゲームパッド／タッチ）が押されたらゲーム本編へ。
    /// 新しい Input System を使用。
    /// </summary>
    public class TitleManager : MonoBehaviour
    {
        [SerializeField, Tooltip("スタートで移動するシーン名（Build Profiles にも登録すること）")]
        private string gameSceneName = "MainGame";

        private bool started;

        private void Update()
        {
            if (started) return;

            // フェードインが終わるまでは入力を受け付けない
            if (SceneFader.Instance != null && SceneFader.Instance.IsFading) return;

            if (AnyInputPressed())
            {
                started = true;
                SceneFader.LoadScene(gameSceneName);
            }
        }

        private static bool AnyInputPressed()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame) return true;

            var mouse = Mouse.current;
            if (mouse != null &&
                (mouse.leftButton.wasPressedThisFrame ||
                 mouse.rightButton.wasPressedThisFrame ||
                 mouse.middleButton.wasPressedThisFrame))
                return true;

            foreach (var pad in Gamepad.all)
            {
                foreach (var control in pad.allControls)
                {
                    // スティックを倒しただけでは反応しないよう synthetic を除外
                    if (control is ButtonControl button && !button.synthetic && button.wasPressedThisFrame)
                        return true;
                }
            }

            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;

            return false;
        }
    }
}
