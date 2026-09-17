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
                return;
            }
            //单机/服务端必须写回物块实体：否则 Update() 每帧又把实体里的旧物品套回槽位，
            //玩家刚取走的物品会被“复原”，相当于复制输出。
            parent.TileEntity.OutputItem = Item.Clone();
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