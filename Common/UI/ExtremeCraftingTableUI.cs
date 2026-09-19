namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 终极工作台UI组件
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class ExtremeCraftingTableUI : CraftingTableUI
    {
        /// <summary>
        /// 构造方法，使用反射构造
        /// </summary>
        /// <param name="tileEntity">工作台物块实体实例</param>
        public ExtremeCraftingTableUI(CraftingTableTileEntity tileEntity) : base(tileEntity) { }
        protected override BoundedSize Size => 9;
        protected override Vector2 PanelSize => new(830, 528);
        protected override Vector2 ListSize => new(92, 480);
        protected override float ListHAlign => 0.72f;
        protected override float ScrollbarHAlign => 0.76f;
        protected override float ArmorHAlign => 0.785f;
        protected override float ArmorVAlign => 0.46f;
        protected override float createSlotHAlign => 0.975f;
        protected override int ScrollbarViewMin => 200;
        protected override int ScrollbarViewMax => 1000;
        protected override string TitleText => "";
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            //标题每帧按彩虹渐变着色
            Title?.SetText(Lang.GetItemNameValue(ModContent.ItemType<ExtremeCraftingTable>()).ApplyGradient(ModContent.GetInstance<AvaritiaMod>().Name + "Rainbow"));
        }
    }
}