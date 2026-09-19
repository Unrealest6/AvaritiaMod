namespace AvaritiaMod.Content.Items.Tools
{
    /// <summary>
    /// 无尽贪婪的范围挖掘工具（世界崩解之镐、星球吞噬之铲）的公共部分。
    /// <para>本模组专属的规则全部写在这里，库只认 <see cref="IAoeMiningTool"/>：形态 1 才算范围挖掘、
    /// 矿石额外产出 ×4~40、掉落合并成物质团。</para>
    /// </summary>
    public abstract class AoeToolItem : FrameItem, IAoeMiningTool
    {
        /// <summary>范围挖掘的作用半径：准心 ±该值格（28x28）。</summary>
        protected virtual int AoeRadius => 14;
        /// <summary>旧存档里保存形态用的键（本模组的工具以前用 <c>AvaritiaMode</c>）。</summary>
        protected override string[] LegacyModeTagKeys => ["AvaritiaMode"];
        /// <summary>
        /// 额外掉落表的复用实例：每次读取只改这一条，避免逐格新建字典（一次挥动上千格）。
        /// <para>表里的键是按物块类型索引的开关表（复用 tML 的 <c>TileID.Sets.Ore</c>），值是这一格的掉落加成；
        /// 想给别的物块也加倍，在这里加一条即可（如 <c>[TileID.Sets.CanBeDugByShovel] = new(2)</c>）。</para>
        /// </summary>
        private static readonly Dictionary<bool[], Int32Modifier> ExtraDrops = new() { [TileID.Sets.Ore] = new(1) };
        /// <inheritdoc/>
        public IReadOnlyDictionary<bool[], Int32Modifier> ExtraDropModifier
        {
            get
            {
                //每次读取现掷一次倍率（与“逐个掉落现掷”等价）
                ExtraDrops[TileID.Sets.Ore] = new Int32Modifier(Main.rand.Next(4, 41));
                return ExtraDrops;
            }
        }
        /// <summary>
        /// 掉落交给物质团。
        /// <para><b>形态不是范围挖掘时照原样散落</b>：形态 0 挖罐子之类“现算随机奖励”的方块也会走到这里，此时应保持原版行为。</para>
        /// </summary>
        public void DeliverDrops(Player? player, List<Item> drops, Vector2 position)
        {
            if (player is not null && GetModeFor(player, player.HeldItem) != 1)
            {
                BreakHelper.ScatterDrops(drops, position);
                return;
            }
            AvaritiaBreakHelper.SpawnAsClusters(drops, position);
        }
        /// <summary>准心处能否开始一次范围挖掘（形态对、在交互距离内、那一格有方块），能则同时给出本次挥动的结算状态。</summary>
        protected bool TryBeginAoeSwing(Player player, out BreakHelper.AoeSwing swing)
        {
            int baseX = (int)(Main.MouseWorld.X / 16f);
            int baseY = (int)(Main.MouseWorld.Y / 16f);
            swing = new BreakHelper.AoeSwing(player);
            return GetHeldMode(player) == 1
                && player.IsInTileInteractionRange(baseX, baseY, TileReachCheckSettings.Simple)
                && BreakHelper.InBounds(baseX, baseY)
                && Framing.GetTileSafely(baseX, baseY).HasTile;
        }
        /// <summary>扫描准心周围 28x28 的每一格（越界自动跳过）——墙体也在这套格子里处理。</summary>
        protected void ForEachAoeTile(Action<Tile, int, int> action)
        {
            int baseX = (int)(Main.MouseWorld.X / 16f);
            int baseY = (int)(Main.MouseWorld.Y / 16f);
            for (int x = baseX - AoeRadius; x < baseX + AoeRadius; x++)
            {
                for (int y = baseY - AoeRadius; y < baseY + AoeRadius; y++)
                {
                    if (BreakHelper.InBounds(x, y))
                    {
                        action(Framing.GetTileSafely(x, y), x, y);
                    }
                }
            }
        }
    }
}