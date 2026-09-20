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
                //联机下本地实体只是服务端广播过来的镜像（产物由服务端持续生成，镜像随时可能过期）；
                //取出的数量由服务端结算后回发，这里绝不能把本地算出的数量写回去。
                return;
            }
            //单机/服务端必须写回实体，否则 Update() 会把实体里的旧物品套回槽位，等于复制输出。
            parent.TileEntity.OutputItem = Item.Clone();
        }
        public override void Update(GameTime gameTime)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && Parent.Parent is NeutronCollectorUI parent)
            {
                SetItemSilently(parent.TileEntity.OutputItem);
            }
            base.Update(gameTime);
        }
        /// <summary>
        /// 处理鼠标单击：联机下只发“取出”意图，由服务端按真实产物数量结算并回发。
        /// <para>多个玩家同时点同一个输出槽时，服务端按顺序结算，每个请求都只拿到“轮到自己时还剩多少”，
        /// 因此不会出现两人各取一份。</para>
        /// </summary>
        /// <param name="evt">鼠标事件参数。</param>
        protected override void MouseClick(UIMouseEvent evt)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Parent.Parent is not NeutronCollectorUI parent)
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
