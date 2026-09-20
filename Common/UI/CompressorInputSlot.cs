namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 压缩机输入槽位UI元素
    /// </summary>
    public sealed class CompressorInputSlot : AvaritiaInputSlot
    {
        /// <summary>发出请求时所在的帧号（0 表示没有未结算的请求）。</summary>
        private static ulong _pendingFrame;
        /// <summary>等待服务端结算的超时帧数（回包异常丢失时的兜底，避免一直点不动）。</summary>
        private const ulong PendingTimeoutFrames = 60;
        /// <summary>
        /// 是否还有一次动作没有被服务端结算。
        /// <para>这不是“点击延迟”：回包一到（通常同一 tick 内）立刻解除。它保证同一时刻只有一笔交易在飞，
        /// 否则连点两下会把同一叠鼠标物品提交两次，而第二次服务端收下的数量在鼠标上已经不存在了（会多出物品）。</para>
        /// </summary>
        private static bool Pending => _pendingFrame != 0 && Main.GameUpdateCount <= _pendingFrame + PendingTimeoutFrames;
        /// <summary>有动作发出，进入等待结算状态。</summary>
        private static void MarkPending() => _pendingFrame = Math.Max(1, Main.GameUpdateCount);
        /// <summary>服务端已结算（由网络层调用）。</summary>
        public static void ClearPending() => _pendingFrame = 0;
        protected override void OnItemChanged()
        {
            if (Parent.Parent is not NeutroniumCompressorUI parent)
            {
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                //联机下本地实体只是服务端广播过来的镜像；界面内容由服务端结算后广播回来，
                //这里绝不能把本地数量写回去（镜像可能已经过期：输入槽每 3 tick 被消耗一次）。
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
        /// <summary>
        /// 左键单击。联机下只发意图（放入 / 取出 / 丢弃 / 交换），数量由服务端按真实数据结算。
        /// </summary>
        /// <param name="evt">鼠标事件参数。</param>
        public override void LeftClick(UIMouseEvent evt)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Parent.Parent is not NeutroniumCompressorUI parent)
            {
                base.LeftClick(evt);
                return;
            }
            if (Pending)
            {
                return;
            }
            if (ItemSlot.ControlInUse)
            {
                Request(parent, AvaritiaNet.CompressorAction.Trash, new Item());
                return;
            }
            if (ItemSlot.ShiftInUse)
            {
                Request(parent, AvaritiaNet.CompressorAction.TakeAllToInventory, new Item());
                return;
            }
            if (Main.mouseItem.IsAir)
            {
                if (!Item.IsAir)
                {
                    Request(parent, AvaritiaNet.CompressorAction.TakeAll, new Item());
                }
                return;
            }
            if (Item.IsAir || Item.type == Main.mouseItem.type && Item.maxStack == Main.mouseItem.maxStack && Item.stack < Item.maxStack)
            {
                Request(parent, AvaritiaNet.CompressorAction.Insert, Main.mouseItem.Clone());
                return;
            }
            Request(parent, AvaritiaNet.CompressorAction.Swap, Main.mouseItem.Clone());
        }
        /// <summary>
        /// 右键单击。联机下同样只发意图：放入一个 / 取出一半。
        /// </summary>
        /// <param name="evt">鼠标事件参数。</param>
        public override void RightClick(UIMouseEvent evt)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient || Parent.Parent is not NeutroniumCompressorUI parent)
            {
                base.RightClick(evt);
                return;
            }
            if (Pending)
            {
                return;
            }
            if (Main.mouseItem.IsAir)
            {
                if (!Item.IsAir)
                {
                    Request(parent, AvaritiaNet.CompressorAction.TakeHalf, new Item());
                }
                return;
            }
            if (Item.IsAir || Item.type == Main.mouseItem.type && Item.stack < Item.maxStack)
            {
                Request(parent, AvaritiaNet.CompressorAction.InsertOne, new Item(Main.mouseItem.type));
            }
        }
        /// <summary>请求服务端对输入槽执行一次动作，并进入“等待结算”状态。</summary>
        /// <param name="parent">压缩机界面。</param>
        /// <param name="action">动作类型。</param>
        /// <param name="payload">动作附带的物品（放入 / 交换用）。</param>
        private static void Request(NeutroniumCompressorUI parent, AvaritiaNet.CompressorAction action, Item payload)
        {
            MarkPending();
            AvaritiaNet.RequestCompressorAction(parent.TileEntity.Position, output: false, action, payload);
        }
    }
}
