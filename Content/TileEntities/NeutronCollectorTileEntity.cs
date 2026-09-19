namespace AvaritiaMod.Content.TileEntities
{
    public sealed class NeutronCollectorTileEntity : ModTileEntity
    {
        public NeutronCollectorUI? NeutronCollectorUI { get; set; }
        internal int ProcessTimer { get; private set; }
        internal StyleDimension[] Styles { get; set; } = new StyleDimension[2];
        internal Item OutputItem { get; set; } = new();
        internal bool IsWorking { get; private set; }
        /// <summary>距离下一次周期性进度同步还剩多少 tick。</summary>
        private int _syncCooldown;
        private int _lastSyncedType = -1;
        private int _lastSyncedStack = -1;
        public static void SendOutputChange(Point16 tilePos, Item outputItem)
            => AvaritiaNet.RequestCollectorOutput(tilePos, outputItem);
        public void SendWholeCollector(int toClient = -1)
            => AvaritiaNet.BroadcastTileEntity(this, AvaritiaMod.SyncMessageType.BroadcastCollector, toClient);
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
                //物品变化时立即同步，否则每 30 tick 同步一次进度，避免每 tick 广播完整实体。
                _syncCooldown--;
                bool itemChanged = OutputItem.type != _lastSyncedType || OutputItem.stack != _lastSyncedStack;
                if (itemChanged || _syncCooldown <= 0)
                {
                    _lastSyncedType = OutputItem.type;
                    _lastSyncedStack = OutputItem.stack;
                    _syncCooldown = 30;
                    SendWholeCollector();
                }
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
            ProcessTimer = tag.ContainsKey("NeutronCollectorTileEntityProcessTimer") ? tag.Get<int>("NeutronCollectorTileEntityProcessTimer") : 0;
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