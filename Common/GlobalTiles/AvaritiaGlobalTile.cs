namespace AvaritiaMod.Common.GlobalTiles
{
    /// <summary>
    /// 模组自己的物块判定：三个工具“形态 0”的单块挖掘规则。
    /// <para>通用的部分（屏蔽自动掉落、收集窗口、罐子奖励兜底）在库的
    /// <see cref="BreakGlobalTile"/>；形态 1 的范围挖掘在工具自己的 <c>UseItem</c> 里。</para>
    /// </summary>
    public sealed class AvaritiaGlobalTile : GlobalTile
    {
        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            Player player = Main.LocalPlayer;
            Item heldItem = player.HeldItem;
            if (!player.controlUseItem || heldItem.IsAir || !BreakHelper.InBounds(i, j)
                || !player.IsInTileInteractionRange(i, j, TileReachCheckSettings.Simple))
            {
                return base.CanKillTile(i, j, type, ref blockDamaged);
            }
            Tile tile = Framing.GetTileSafely(i, j);
            //形态 1（范围挖掘）由工具自己的 UseItem 处理，这里只管形态 0 的单块挖掘
            if (!tile.HasTile || AvaritiaPlayer.GetItemMode(player, heldItem.type) != 0)
            {
                return base.CanKillTile(i, j, type, ref blockDamaged);
            }
            bool worldBreaker = heldItem.ModItem is WorldBreaker && !Main.tileAxe[tile.TileType];
            bool shovelMode = heldItem.ModItem is PlanetEater && TileID.Sets.CanBeDugByShovel[tile.TileType];
            bool axeMode = heldItem.ModItem is NatureRuin && Main.tileAxe[tile.TileType];
            //箱子交给原版：箱内有物品时原版判该格存活，与镐一致（只有范围挖掘形态才收内容物）
            if (!worldBreaker && !shovelMode && !axeMode || worldBreaker && TileID.Sets.BasicChest[tile.TileType])
            {
                return base.CanKillTile(i, j, type, ref blockDamaged);
            }
            if (worldBreaker && TileID.Sets.Ore[tile.TileType])
            {
                //矿石额外产出：用工具上的“额外掉落”表，与范围挖掘共用同一份规则
                foreach (Item drop in BreakHelper.GetTileItemDrops(i, j, IAoeMiningTool.GetIAoeMiningTool(heldItem)?.ExtraDropModifier))
                {
                    BreakHelper.SpawnDrop(i, j, drop);
                }
                BreakHelper.BreakTile(i, j);
            }
            else
            {
                //其余方块（家具等）交给原版掉落
                BreakHelper.BreakTileWithVanillaDrops(i, j);
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(-1, i, j, 1);
            }
            return true;
        }
    }
}
