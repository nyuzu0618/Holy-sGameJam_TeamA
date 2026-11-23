namespace TCA_GameJam.Scripts.Managers
{
    /// <summary>
    /// ゲームの進行フェーズ
    /// </summary>
    public enum GamePhase
    {
        Collect,    // 仲間の羊を集めるフェーズ(後ろから見る)
        Bowling,    // 毛玉で人類をなぎ倒すフェーズ(横から見る)
        Clear,
        GameOver,
    }
}
