namespace AvaritiaMod.Common.UI
{
    public sealed class NeutronCollectorUI : DragUIState<UIPanel>
    {
        private UIText? _title;
        private UIText? _processText;
        private UITextPanel<string>? _closeButton;
        public NeutronCollectorOutputSlot? OutputSlot { get; private set; }
        public NeutronCollectorTileEntity TileEntity { get; }
        public NeutronCollectorUI(NeutronCollectorTileEntity tileEntity)
        {
            TileEntity = tileEntity;
            Element = new UIPanel();
        }
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
            _closeButton = new UITextPanel<string>(Language.GetTextValue("LegacyMisc.56"));
            _closeButton.Width.Set(100, 0);
            _closeButton.Height.Set(40, 0);
            _closeButton.HAlign = 0.99f;
            _closeButton.VAlign = 0.01f;
            _closeButton.OnLeftClick += (_, _) =>
            {
                Visible = false;
                ModContent.GetInstance<NeutronCollectorUISystem>().HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
            };
            Element.Append(_closeButton);
            OutputSlot = new NeutronCollectorOutputSlot();
            OutputSlot.Width.Set(78, 0);
            OutputSlot.Height.Set(78, 0);
            OutputSlot.Top.Set(0, 0.3f);
            OutputSlot.Left.Set(0, 0.38f);
            Element.Append(OutputSlot);
        }
        public override void OnActivate()
        {
            if (Element is null)
            {
                return;
            }
            Element.Left = TileEntity.Styles[0];
            Element.Top = TileEntity.Styles[1];
        }
        public override void OnDeactivate()
        {
            if (Element is null)
            {
                return;
            }
            TileEntity.Styles[0] = Element.Left;
            TileEntity.Styles[1] = Element.Top;
        }
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