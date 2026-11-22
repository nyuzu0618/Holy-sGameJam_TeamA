using TCA_GameJam.Scripts.Player;

namespace TCA_GameJam.Scripts.Stage
{
    /// <summary>
    /// 羊(プレイヤー)がぶつかったときに処理するもの
    /// </summary>
    public interface IItem
    {
        /// <summary>
        /// 羊がぶつかったときに呼ばれる
        /// </summary>
        void Hit(SheepHerd herd);
    }
}
