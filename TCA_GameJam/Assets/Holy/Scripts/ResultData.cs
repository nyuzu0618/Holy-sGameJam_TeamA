/// <summary>
/// 本編 → リザルトへ結果を渡すための入れ物。
/// static なのでシーンをまたいでも値が残る。
///
/// 本編のゲーム終了時に：
///     ResultData.Set(1つ目の数, 2つ目の数);
///     SceneFader.LoadScene("result");
/// </summary>
public static class ResultData
{
    /// <summary>リザルトの「画像 ×99」に表示する数（[0] が1つ目、[1] が2つ目）</summary>
    public static readonly int[] Counts = new int[2];

    /// <summary>本編から値が設定されたら true。false のときはリザルトの Inspector の仮の数字を表示する</summary>
    public static bool HasData { get; private set; }

    public static void Set(int count1, int count2)
    {
        Counts[0] = count1;
        Counts[1] = count2;
        HasData = true;
    }

    /// <summary>値をリセットする（ゲーム開始時などに呼ぶ）</summary>
    public static void Clear()
    {
        Counts[0] = 0;
        Counts[1] = 0;
        HasData = false;
    }
}
