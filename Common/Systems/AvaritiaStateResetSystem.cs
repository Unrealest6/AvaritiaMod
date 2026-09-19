namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 统一清理本模组的静态状态。
    /// <para>静态字段不会随程序集卸载自动失效：跨世界或热重载后会残留上一份世界 / 上一轮加载的数据
    /// （方块坐标、缓存、按类型号索引的表），因此在这里集中复位。</para>
    /// </summary>
    public sealed class AvaritiaStateResetSystem : ModSystem
    {
        /// <summary>换世界时只清“按坐标 / 按世界”的数据（配方注册表要留给下一次加载使用）。</summary>
        public override void OnWorldUnload() => CraftingTablePending.Clear();
        public override void OnWorldLoad() => CraftingTablePending.Clear();
        public override void Unload()
        {
            CraftingTablePending.Clear();
            AvaritiaRecipe.ResetStatics();
            AvaritiaBreakHelper.ClearCache();
        }
    }
}
