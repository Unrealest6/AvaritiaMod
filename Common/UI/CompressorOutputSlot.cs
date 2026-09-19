namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 压缩机输出槽位UI元素
    /// </summary>
    public sealed class CompressorOutputSlot : AvaritiaOutputSlot
    {
        protected override void OnItemChanged()
        {
            if (Parent.Parent is not NeutroniumCompressorUI parent)
            {
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NeutroniumCompressorTileEntity.SendOutputChange(parent.TileEntity.Position, Item.Clone());
                return;
            }
            //单机 / 服务端必须写回实体，否则 Update() 会把实体里的旧物品套回槽位，等于复制输出
            parent.TileEntity.OutputItem = Item.Clone();
        }
        public override void Update(GameTime gameTime)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && Parent.Parent is NeutroniumCompressorUI parent)
            {
                SetItemSilently(parent.TileEntity.OutputItem);
            }
            base.Update(gameTime);
        }
    }
}