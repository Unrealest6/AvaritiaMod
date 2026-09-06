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
        public static void SendInputChange(Point16 tilePos, Item inputItem)
        {
            ModPacket packet = ModContent.GetInstance<AvaritiaMod>().GetPacket();
            packet.Write((byte)AvaritiaMod.SyncMessageType.RequestCompressorInput);
            packet.Write(tilePos.X);
            packet.Write(tilePos.Y);
            ItemIO.Send(inputItem, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
        public static void SendOutputChange(Point16 tilePos, Item outputItem)
        {
            ModPacket packet = ModContent.GetInstance<AvaritiaMod>().GetPacket();
            packet.Write((byte)AvaritiaMod.SyncMessageType.RequestCompressorOutput);
            packet.Write(tilePos.X);
            packet.Write(tilePos.Y);
            ItemIO.Send(outputItem, packet, writeStack: true, writeFavorite: true);
            packet.Send();
        }
        public void SendWholeCompressor(int toClient = -1)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)AvaritiaMod.SyncMessageType.BroadcastCompressor);
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
            return tile.HasTile && tile.TileType == ModContent.TileType<NeutroniumCompressorTile>();
        }
        public override void Update()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (Main.netMode == NetmodeID.Server)
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