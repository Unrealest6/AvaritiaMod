namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 压缩机输入槽位UI元素
    /// </summary>
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
                return;
            }
            //单机 / 服务端必须写回实体，否则 Update() 会把实体里的旧物品套回槽位，等于复制输入
            parent.TileEntity.InputItem = Item.Clone();
        }
        public override void Update(GameTime gameTime)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && Parent.Parent is NeutroniumCompressorUI parent)
            {
                SetItemSilently(parent.TileEntity.InputItem);
            }
            base.Update(gameTime);
        }
    }
}