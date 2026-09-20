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
                //联机下本地实体只是服务端广播过来的镜像；产物数量由服务端结算后广播回来。
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
        /// <summary>
        /// 处理鼠标单击：联机下只发“取出”意图，由服务端按真实产物数量结算并回发。
        /// </summary>
        /// <param name="evt">鼠标事件参数。</param>
        protected override void MouseClick(UIMouseEvent evt)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Parent.Parent is not NeutroniumCompressorUI parent)
            {
                base.MouseClick(evt);
                return;
            }
            if (Item.IsAir && parent.TileEntity.OutputItem.IsAir)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                return;
            }
            AvaritiaNet.RequestCompressorAction(parent.TileEntity.Position, output: true,
                ItemSlot.ShiftInUse ? AvaritiaNet.CompressorAction.TakeAllToInventory : AvaritiaNet.CompressorAction.TakeAll, new Item());
        }
    }
}
