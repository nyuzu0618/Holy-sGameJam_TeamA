using UnityEngine;
using UnityEngine.InputSystem;

namespace OutGame
{
    /// <summary>
    /// 動作確認用：スペースキーでリザルトを表示する（MainGameTest シーン専用）。
    /// 本編では使わない。
    /// </summary>
    public class MainGameTest : MonoBehaviour
    {
        [SerializeField, Tooltip("表示するリザルト（空ならシーン内から自動で探す）")]
        private ResultManager result;

        [SerializeField, Tooltip("ON：毎回ランダムな値で表示　OFF：下の値で表示")]
        private bool randomValues = true;

        [SerializeField] private int testValue1 = 12;
        [SerializeField] private int testValue2 = 34;

        private void Start()
        {
            if (result == null) result = FindAnyObjectByType<ResultManager>();
            if (result == null) Debug.LogWarning("[MainGameTest] シーン内に ResultManager が見つかりません。");
        }
    }
}
