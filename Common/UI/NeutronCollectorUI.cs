namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 中子态素收集器UI组件
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class NeutronCollectorUI : DragUIState<UIPanel>, ITileEntityUI<NeutronCollectorTileEntity>
    {
        /// <summary>
        /// 输出槽 UI 元素实例
        /// </summary>
        public NeutronCollectorOutputSlot? OutputSlot { get; private set; }
        /// <summary>
        /// 中子态素收集器物块实体实例
        /// </summary>
        public NeutronCollectorTileEntity TileEntity { get; }
        /// <summary>
        /// 标题文本 UI 实例
        /// </summary>
        private UIText? _title;
        /// <summary>
        /// 处理进度文本 UI 实例
        /// </summary>
        private UIText? _processText;
        /// <summary>
        /// 关闭按钮 UI 实例
        /// </summary>
        private UITextPanel<string>? _closeButton;
        /// <summary>
        /// 直接拖动标题栏即可移动面板（无需按住 Shift，也不会先按到输出槽上）。
        /// </summary>
        protected override UIElement? DragHandle => _title;
        /// <summary>
        /// 构造方法，使用反射构造
        /// </summary>
        /// <param name="tileEntity">中子态素收集器物块实体实例</param>
        public NeutronCollectorUI(NeutronCollectorTileEntity tileEntity)
        {
            TileEntity = tileEntity;
            Element = new UIPanel();
        }
        /// <summary>
        /// 初始化 UI 组件。创建主面板、标题、进度文本、关闭按钮和输出槽。
        /// </summary>
        public override void OnInitialize()
        {
            if (Element is null)
            {
                return;
            }
            Element.SetPadding(5);
            Element.Width.Set(320, 0);
            Element.Height.Set(200, 0);
            Element.HAlign = 0.2f;
            Element.VAlign = 0.4f;
            Element.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            Append(Element);
            _title = new UIText(ModContent.GetModItem(ModContent.ItemType<NeutronCollector>()).DisplayName.Value)
            {
                HAlign = 0.5f
            };
            _title.Top.Set(-30, 0);
            Element.Append(_title);
            _processText = new UIText("Process: ")
            {
                HAlign = 0.5f,
                VAlign = 0.85f
            };
            Element.Append(_processText);
            _closeButton = EternalUI.CreateCloseButton(Language.GetTextValue("LegacyMisc.56"), () =>
            {
                Visible = false;
                ModContent.GetInstance<NeutronCollectorUISystem>().HideUI();
            });
            Element.Append(_closeButton);
            OutputSlot = new NeutronCollectorOutputSlot();
            OutputSlot.Width.Set(78, 0);
            OutputSlot.Height.Set(78, 0);
            OutputSlot.Top.Set(0, 0.3f);
            OutputSlot.Left.Set(0, 0.38f);
            Element.Append(OutputSlot);
        }
        /// <summary>
        /// UI 激活时调用。恢复上次保存的面板位置。
        /// </summary>
        public override void OnActivate()
        {
            if (Element is null)
            {
                return;
            }
            Element.Left = TileEntity.Styles[0];
            Element.Top = TileEntity.Styles[1];
        }
        /// <summary>
        /// UI 关闭时调用。将当前面板位置保存到物块实体。
        /// </summary>
        public override void OnDeactivate()
        {
            if (Element is null)
            {
                return;
            }
            TileEntity.Styles[0] = Element.Left;
            TileEntity.Styles[1] = Element.Top;
        }
        /// <summary>
        /// 每帧更新 UI。处理 Escape 键关闭，并刷新处理进度文本。
        /// </summary>
        /// <param name="gameTime">游戏时间信息。</param>
        public override void Update(GameTime gameTime)
        {
            if (Main.keyState.IsKeyDown(Keys.Escape))
            {
                ModContent.GetInstance<NeutronCollectorUISystem>().HideUI();
            }
            base.Update(gameTime);
            _processText?.SetText("Process: " + (TileEntity.ProcessTimer / 21333f * 100f).ToString("F1") + "%");
        }
    }
}