namespace AvaritiaMod.Content.Items
{
    /// <summary>
    /// 把“形态”绑定到<b>物品实例</b>上（世界崩解之镐 / 星球吞噬之铲）。
    /// <para>为什么不能用 <c>ModItem.Mode</c>：ModItem 每个类型只有一个实例，写在上面等于全局共享，
    /// 背包里两把同款工具会互相影响，多人游戏下还会被别人的形态覆盖。</para>
    /// <para><see cref="InstancePerEntity"/> 让 tModLoader 为每个适用的物品实例创建一个 GlobalItem 实例，
    /// 于是形态天然跟随物品实例：换格子、放进箱子、丢到地上都保持一致，
    /// 并通过 <see cref="SaveData"/> / <see cref="NetSend"/> 参与存档与同步。</para>
    /// </summary>
    public sealed class AvaritiaModeGlobalItem : GlobalItem
    {
        /// <summary>该物品实例当前的形态。</summary>
        public byte Mode { get; set; }
        /// <summary>
        /// 每个物品实例独立持有一份数据。
        /// <para>注意：本版 tModLoader 的属性名是 <c>InstancePerEntity</c>；
        /// 在它返回 false 时给 GlobalItem 加实例字段会在加载期直接抛异常。</para>
        /// </summary>
        public override bool InstancePerEntity => true;
        /// <summary>只挂在支持形态切换的物品上。</summary>
        public override bool AppliesToEntity(Item entity, bool lateInstantiation) => entity.ModItem is AvaritiaModeItem;
        public override void SaveData(Item item, TagCompound tag)
        {
            if (Mode != 0)
            {
                tag["AvaritiaMode"] = Mode;
            }
        }
        public override void LoadData(Item item, TagCompound tag)
        {
            Mode = tag.ContainsKey("AvaritiaMode") ? tag.GetByte("AvaritiaMode") : (byte)0;
        }
        public override void NetSend(Item item, BinaryWriter writer) => writer.Write(Mode);
        public override void NetReceive(Item item, BinaryReader reader) => Mode = reader.ReadByte();
        /// <summary>读取某个物品实例的形态（该实例没有形态数据时返回 0）。</summary>
        public static byte GetMode(Item? item)
            => item is not null && item.TryGetGlobalItem(out AvaritiaModeGlobalItem global) ? global.Mode : (byte)0;
        /// <summary>写入某个物品实例的形态。</summary>
        public static void SetMode(Item? item, byte mode)
        {
            if (item is not null && item.TryGetGlobalItem(out AvaritiaModeGlobalItem global))
            {
                global.Mode = mode;
            }
        }
    }
}
