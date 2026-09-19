namespace AvaritiaMod.Content.TileEntities
{
    public sealed class NeutroniumCompressorTileEntity : ModTileEntity
    {
        public NeutroniumCompressorUI? NeutroniumCompressorUI { get; set; }
        internal StyleDimension[] Styles { get; set; } = new StyleDimension[2];
        internal Item InputItem { get; set; } = new();
        internal Item OutputItem { get; set; } = new();
        internal Item ProcessingItem { get; private set; } = new();
        internal bool IsWorking { get; private set; }
        private int _processTimer;
        /// <summary>距下次强制同步还剩多少 tick：状态变化时立即同步，否则每 30 tick 补一次（防丢包）。</summary>
        private int _syncCooldown;
        private int _lastInputType = -1;
        private int _lastInputStack = -1;
        private int _lastOutputType = -1;
        private int _lastOutputStack = -1;
        private int _lastProcessingType = -1;
        private int _lastProcessingStack = -1;
        private bool _lastWorking;
        public static void SendInputChange(Point16 tilePos, Item inputItem)
            => AvaritiaNet.RequestCompressorSlot(tilePos, inputItem, output: false);
        public static void SendOutputChange(Point16 tilePos, Item outputItem)
            => AvaritiaNet.RequestCompressorSlot(tilePos, outputItem, output: true);
        public void SendWholeCompressor(int toClient = -1)
            => AvaritiaNet.BroadcastTileEntity(this, AvaritiaMod.SyncMessageType.BroadcastCompressor, toClient);
        public override bool IsTileValidForEntity(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && tile.TileType == ModContent.TileType<NeutroniumCompressorTile>();
        }
        public override void Update()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (Main.netMode == NetmodeID.Server && ShouldSyncCompressor())
            {
                SendWholeCompressor();
            }
            if (InputItem.IsAir)
            {
                IsWorking = false;
                _processTimer = 0;
                return;
            }
            if (!Singularity.Singularities.TryGetValue(InputItem.type, out Singularity? singularity) || !OutputItem.IsAir && OutputItem.type != singularity.Type)
            {
                IsWorking = false;
                return;
            }
            if (ProcessingItem.IsAir)
            {
                ProcessingItem = new Item(InputItem.type, 0);
            }
            else if (ProcessingItem.type != InputItem.type)
            {
                IsWorking = false;
                return;
            }
            IsWorking = true;
            _processTimer++;
            if (_processTimer < 3)
            {
                return;
            }
            _processTimer = 0;
            if (InputItem.stack > 0)
            {
                InputItem.stack--;
                ProcessingItem.stack++;
                if (ProcessingItem.stack >= Singularity.Singularities[ProcessingItem.type].RequiredQuantity)
                {
                    if (OutputItem.IsAir)
                    {
                        OutputItem = new Item(Singularity.Singularities[ProcessingItem.type].Type);
                    }
                    else
                    {
                        OutputItem.stack++;
                    }
                    ProcessingItem.TurnToAir();
                }
            }
            if (InputItem.stack <= 0)
            {
                InputItem.TurnToAir();
            }
        }
        public override void NetSend(BinaryWriter writer)
        {
            ItemIO.Send(InputItem, writer, writeStack: true, writeFavorite: true);
            ItemIO.Send(OutputItem, writer, writeStack: true, writeFavorite: true);
            ItemIO.Send(ProcessingItem, writer, writeStack: true, writeFavorite: true);
            writer.Write(IsWorking);
            writer.Write(_processTimer);
        }
        public override void NetReceive(BinaryReader reader)
        {
            InputItem = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            OutputItem = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            ProcessingItem = ItemIO.Receive(reader, readStack: true, readFavorite: true);
            IsWorking = reader.ReadBoolean();
            _processTimer = reader.ReadInt32();
        }
        /// <summary>
        /// 服务端是否需要广播整台压缩机：只在界面可见状态变化时发包（进度每 3 tick 变一次），
        /// 并每 30 tick 兜底同步一次，避免空闲时逐 tick 发包。
        /// </summary>
        private bool ShouldSyncCompressor()
        {
            bool changed = InputItem.type != _lastInputType || InputItem.stack != _lastInputStack
                || OutputItem.type != _lastOutputType || OutputItem.stack != _lastOutputStack
                || ProcessingItem.type != _lastProcessingType || ProcessingItem.stack != _lastProcessingStack
                || IsWorking != _lastWorking;
            if (!changed && --_syncCooldown > 0)
            {
                return false;
            }
            _lastInputType = InputItem.type;
            _lastInputStack = InputItem.stack;
            _lastOutputType = OutputItem.type;
            _lastOutputStack = OutputItem.stack;
            _lastProcessingType = ProcessingItem.type;
            _lastProcessingStack = ProcessingItem.stack;
            _lastWorking = IsWorking;
            _syncCooldown = 30;
            return true;
        }
        public override void SaveData(TagCompound tag)
        {
            tag["NeutroniumCompressorInput"] = InputItem;
            tag["NeutroniumCompressorOutput"] = OutputItem;
            tag["NeutroniumCompressorProcessing"] = ProcessingItem;
        }
        public override void LoadData(TagCompound tag)
        {
            InputItem = tag.Get<Item>("NeutroniumCompressorInput") ?? new Item();
            OutputItem = tag.Get<Item>("NeutroniumCompressorOutput") ?? new Item();
            ProcessingItem = tag.Get<Item>("NeutroniumCompressorProcessing") ?? new Item();
        }
    }
}