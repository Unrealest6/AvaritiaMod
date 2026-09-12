namespace AvaritiaMod.Common.UI
{
    public sealed class CompressorInputSlot : AvaritiaInputSlot
    {
        protected override void OnItemChanged()
        {
            if (Parent.Parent is not NeutroniumCompressorUI parent)
            {
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NeutroniumCompressorTileEntity.SendInputChange(parent.TileEntity.Position, Item.Clone());
            }
            else if (Main.netMode == NetmodeID.SinglePlayer)
            {
                parent.TileEntity.InputItem = Item.Clone();
            }
        }
        public override void Update(GameTime gameTime)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (Parent.Parent is not NeutroniumCompressorUI parent)
                {
                    return;
                }
                SetItemSilently(parent.TileEntity.InputItem);
            }
            base.Update(gameTime);
        }
    }
}