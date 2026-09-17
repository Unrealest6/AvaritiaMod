using AvaritiaMod.Common;

namespace AvaritiaMod.Content.Items
{
    /// <summary>
    /// 带“形态切换”的序列帧物品基类（世界崩解之镐 / 星球吞噬之铲）。
    /// <para><b>形态绑定在物品实例上</b>（由 <see cref="AvaritiaModeGlobalItem"/> 保存），
    /// 所以背包里两把同款工具可以各自处于不同形态，换格子、放进箱子、丢到地上都不会串味。</para>
    /// <para>多人游戏下，其它客户端看到的是“你的手持物品”：本地读实例数据，远程则读
    /// <see cref="AvaritiaPlayer.HeldItemMode"/>（切换时通过 <see cref="AvaritiaNet.SendItemMode"/> 同步）。</para>
    /// </summary>
    public abstract class AvaritiaModeItem : FrameItem
    {
        /// <summary>形态数量（形态取值为 0 ~ ModeCount-1）。</summary>
        protected abstract byte ModeCount { get; }
        /// <summary>切换形态的按键组合（默认：Shift + 右键点按）。</summary>
        protected virtual bool ToggleModePressed
            => Main.keyState.IsKeyDown(Keys.LeftShift) && Main.mouseRight && Main.mouseRightRelease;
        /// <summary>
        /// 取某个物品实例应当使用的形态。
        /// <para>本地玩家/单人：直接读该实例自己的形态；远程玩家：本地副本不一定带同步过来的
        /// 实例数据，此时退回按玩家同步的形态。</para>
        /// </summary>
        protected override byte GetMode(Player drawPlayer, Item? item)
        {
            byte instanceMode = AvaritiaModeGlobalItem.GetMode(item);
            if (drawPlayer.whoAmI == Main.myPlayer || Main.netMode == NetmodeID.SinglePlayer || instanceMode != 0)
            {
                return instanceMode;
            }
            return drawPlayer.GetModPlayer<AvaritiaPlayer>().HeldItemMode;
        }
        /// <summary>取本地玩家手持实例的形态（判定、属性同步用）。</summary>
        protected byte GetHeldMode(Player player) => AvaritiaModeGlobalItem.GetMode(player.HeldItem);
        /// <summary>
        /// 把形态对应的属性写到实际手持的物品实例上（每 tick 调用一次，
        /// 这样重新拿出物品、重进世界后属性也不会停留在默认值）。
        /// </summary>
        protected virtual void ApplyModeStats(Item heldItem, byte mode) { }
        public override void HoldItem(Player player)
        {
            // HoldItem 只在本地玩家的 ItemCheck 流程里被调用，这里再明确一次以免将来被别的路径调用。
            if (player.whoAmI != Main.myPlayer)
            {
                return;
            }
            Item heldItem = player.HeldItem;
            if (heldItem is null || heldItem.IsAir || heldItem.type != Type)
            {
                return;
            }
            byte mode = AvaritiaModeGlobalItem.GetMode(heldItem);
            if (ModeCount > 1 && ToggleModePressed)
            {
                mode = (byte)((mode + 1) % ModeCount);
                AvaritiaModeGlobalItem.SetMode(heldItem, mode);
                SoundEngine.PlaySound(SoundID.MenuTick);
            }
            SyncHeldMode(player, heldItem, mode);
            ApplyModeStats(heldItem, mode);
        }
        /// <summary>
        /// 手持实例或其形态发生变化时补发一次同步（不是每 tick 发包）：
        /// 玩家在“形态不同的两把同款工具”之间切换时，其它客户端也要跟着换贴图。
        /// </summary>
        private void SyncHeldMode(Player player, Item heldItem, byte mode)
        {
            AvaritiaPlayer modPlayer = player.GetModPlayer<AvaritiaPlayer>();
            if (ReferenceEquals(modPlayer.LastSyncedHeldItem, heldItem) && modPlayer.HeldItemMode == mode)
            {
                return;
            }
            modPlayer.LastSyncedHeldItem = heldItem;
            modPlayer.HeldItemMode = mode;
            AvaritiaNet.SendItemMode(mode);
        }
    }
}
