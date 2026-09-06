namespace AvaritiaMod.Content.TileEntities
{
    public sealed class NeutronCollectorTileEntity : ModTileEntity
    {
        public NeutronCollectorUI? NeutronCollectorUI { get; set; }
        internal int ProcessTimer { get; private set; }
        internal StyleDimension[] Styles { get; set; } = new StyleDimension[2];
        internal Item OutputItem { get; set; } = new();
        internal bool IsWorking { get; private set; }
        public static void SendOutputChange(Point16 tilePos, Item outputItem)
        {
            ModPacket packet = ModContent.GetInstance<AvaritiaMod>().GetPacket();
            packet.Write((byte)AvaritiaMod.SyncMessageType.RequestCollectorOutput);
            packet.Write(tilePos.X);
            packet.Write(tilePos.Y);
            ItemIO.Send(outputItem, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
        public void SendWholeCollector(int toClient = -1)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)AvaritiaMod.SyncMessageType.BroadcastCollector);
            packet.Write(Position.X);
            packet.Write(Position.Y);
            NetSend(packet);
            if (toClient == -1)
            {
                packet.Send();
            }
            else
            {
                packet.Send(toClient);
            }
        }
        public override bool IsTileValidForEntity(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && tile.TileType == ModContent.TileType<NeutronCollectorTile>();
        }
        public override void Update()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (Main.netMode == NetmodeID.Server)
            {
                SendWholeCollector();
            }
            IsWorking = OutputItem.IsAir || OutputItem.stack < OutputItem.maxStack;
            if (!IsWorking)
            {
                return;
            }
            ProcessTimer++;
            if (ProcessTimer < 21333)
            {
                return;
            }
            ProcessTimer = 0;
            if (OutputItem.IsAir)
            {
                OutputItem = new Item(ModContent.ItemType<PileOfNeutrons>());
            }
            else if (OutputItem.stack < OutputItem.maxStack)
            {
                OutputItem.stack++;
            }
        }
        public override void SaveData(TagCompound tag)
        {
            tag["NeutronCollectorTileEntityItem"] = OutputItem;
            tag["NeutronCollectorTileEntityProcessTimer"] = ProcessTimer;
        }
        public override void LoadData(TagCompound tag)
        {
            if (tag.ContainsKey("NeutronCollectorTileEntityItem"))
            {
                Item item = tag.Get<Item>("NeutronCollectorTileEntityItem");
                OutputItem = item ?? new Item();
            }
            else
            {
                OutputItem = new Item();
            }
            ProcessTimer = tag.ContainsKey("NeutronCollectorTileEntityProcessTimer") ? tag.Get<int>("NeutroniumBlockTileEntityProcessTimer") : 0;
        }
        public override void NetSend(BinaryWriter writer)
        {
            ItemIO.Send(OutputItem, writer, writeStack: true, writeFavorite: true);
            writer.Write(IsWorking);
            writer.Write(ProcessTimer);
        }

        public override void NetReceive(BinaryReader reader)
        {
            OutputItem = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            IsWorking = reader.ReadBoolean();
            ProcessTimer = reader.ReadInt32();
        }
    }
}