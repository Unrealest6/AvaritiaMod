namespace AvaritiaMod.Common.GlobalTiles
{
    public sealed class AvaritiaGlobalTile : GlobalTile
    {
        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            if (Main.LocalPlayer.HeldItem.ModItem is WorldBreaker { Mode: 0 } && Main.LocalPlayer.controlUseItem
                && Main.LocalPlayer.IsInTileInteractionRange(i, j, TileReachCheckSettings.Simple) && !Main.tileAxe[Main.tile[i, j].TileType])
            {
                Tile tile = Main.tile[i, j];
                if (TileID.Sets.Ore[tile.TileType])
                {
                    Type worldGenType = typeof(WorldGen);
                    MethodInfo? killTileGetItemDrops = worldGenType.GetMethod("KillTile_GetItemDrops", BindingFlags.NonPublic | BindingFlags.Static);
                    object[] parameters = [i, j, tile, 0, 0, 0, 0, false,];
                    killTileGetItemDrops?.Invoke(null, parameters);
                    if ((int)parameters[3] > 0 && (int)parameters[4] > 0)
                    {
                        Item item = new((int)parameters[3], (int)parameters[4]);
                        item.stack *= Main.rand.Next(4, 41);
                        int index = Item.NewItem(item.GetSource_DropAsItem(), new Vector2(i * 16, j * 16), item);
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.SyncItem, -1, -1, null, index, 1);
                        }
                    }
                    WorldGen.KillTile(i, j, noItem: true);
                }
                else
                {
                    int x = i;
                    int y = j;
                    if (tile.TileFrameX % 36 != 0)
                    {
                        x--;
                    }
                    if (tile.TileFrameY % 36 != 0)
                    {
                        y--;
                    }
                    int chestIndex = Chest.FindChest(x, y);
                    int chestEmptyIndex = Chest.FindEmptyChest(x, y);
                    if (chestIndex != -1 && chestEmptyIndex == -1)
                    {
                        return true;
                    }
                    if (Main.netMode != NetmodeID.SinglePlayer)
                    {
                        ModPacket packet = Mod.GetPacket();
                        packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                        packet.Write(i);
                        packet.Write(j);
                        packet.Send();
                    }
                    WorldGen.KillTile(i, j);
                }
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendTileSquare(-1, i, j, 1);
                }
                return true;
            }
            if (Main.LocalPlayer.HeldItem.ModItem is PlanetEater { Mode: 0 } && Main.LocalPlayer.controlUseItem && Main.LocalPlayer.IsInTileInteractionRange(i, j, TileReachCheckSettings.Simple)
                && Framing.GetTileSafely(i, j).HasTile && TileID.Sets.CanBeDugByShovel[Framing.GetTileSafely(i, j).TileType] || Main.LocalPlayer.HeldItem.ModItem is NatureRuin &&
                Main.LocalPlayer.controlUseItem &&
                Main.LocalPlayer.IsInTileInteractionRange(i, j, TileReachCheckSettings.Simple)
                && Main.tileAxe[Main.tile[i, j].TileType])
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    ModPacket packet = Mod.GetPacket();
                    packet.Write((byte)AvaritiaMod.SyncMessageType.ServerKillTile);
                    packet.Write(i);
                    packet.Write(j);
                    packet.Send();
                }
                WorldGen.KillTile(i, j);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    NetMessage.SendTileSquare(-1, i, j, 1);
                }
                return true;
            }
            return base.CanKillTile(i, j, type, ref blockDamaged);
        }
    }
}