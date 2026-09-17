namespace AvaritiaMod.Common.GlobalTiles
{
    public sealed class AvaritiaGlobalTile : GlobalTile
    {
        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            Player player = Main.LocalPlayer;
            Item heldItem = player.HeldItem;
            if (!player.controlUseItem || heldItem.IsAir || !AvaritiaBreakHelper.InBounds(i, j)
                || !player.IsInTileInteractionRange(i, j, TileReachCheckSettings.Simple))
            {
                return base.CanKillTile(i, j, type, ref blockDamaged);
            }
            Tile tile = Framing.GetTileSafely(i, j);
            if (!tile.HasTile)
            {
                return base.CanKillTile(i, j, type, ref blockDamaged);
            }
            //世界崩解之镐·形态 0：非斧类方块瞬破，矿石额外乘算产量
            if (heldItem.ModItem is WorldBreaker
                && AvaritiaPlayer.GetItemMode(player, heldItem.type) == 0
                && !Main.tileAxe[tile.TileType])
            {
                if (TileID.Sets.Ore[tile.TileType])
                {
                    if (AvaritiaBreakHelper.TryGetDrop(i, j, tile, out int itemType, out int stack))
                    {
                        AvaritiaBreakHelper.SpawnDrop(i, j, new Item(itemType, stack * Main.rand.Next(4, 41)));
                    }
                    AvaritiaBreakHelper.BreakTile(i, j);
                }
                else if (TileID.Sets.BasicChest[tile.TileType])
                {
                    //箱子保持原版行为：箱内还有物品时原版会判定“该格应当存活”，
                    //与其他镐一致——挖不动、也不掉落；空箱子照常破坏。
                    //（范围挖掘模式形态 1 仍然走“内容物 → 物质团”的处理。）
                    return base.CanKillTile(i, j, type, ref blockDamaged);
                }
                else
                {
                    //其余方块（家具等）交给原版处理
                    AvaritiaBreakHelper.BreakTileWithVanillaDrops(i, j);
                }
                SyncTileChange(i, j);
                return true;
            }
            bool shovelMode = heldItem.ModItem is PlanetEater
                              && AvaritiaPlayer.GetItemMode(player, heldItem.type) == 0
                              && TileID.Sets.CanBeDugByShovel[tile.TileType];
            bool axeMode = heldItem.ModItem is NatureRuin && Main.tileAxe[tile.TileType];
            if (!shovelMode && !axeMode)
            {
                return base.CanKillTile(i, j, type, ref blockDamaged);
            }
            AvaritiaBreakHelper.BreakTileWithVanillaDrops(i, j);
            SyncTileChange(i, j);
            return true;
        }
        /// <summary>把单个方块的变化同步给其它客户端。</summary>
        private static void SyncTileChange(int i, int j)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NetMessage.SendTileSquare(-1, i, j, 1);
            }
        }
    }
}