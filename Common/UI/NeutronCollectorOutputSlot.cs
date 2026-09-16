namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 中子态素收集器输出槽UI元素
    /// </summary>
    public sealed class NeutronCollectorOutputSlot : AvaritiaOutputSlot
    {
        protected override void OnItemChanged()
        {
            if (Parent.Parent is not NeutronCollectorUI parent)
            {
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                NeutronCollectorTileEntity.SendOutputChange(parent.TileEntity.Position, Item.Clone());
            }
        }
        public override void Update(GameTime gameTime)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                if (Parent.Parent is not NeutronCollectorUI parent)
                {
                    return;
                }
                SetItemSilently(parent.TileEntity.OutputItem);
            }
            base.Update(gameTime);
        }
    }
}