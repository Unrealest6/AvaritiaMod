namespace AvaritiaMod.Common.UI
{
    public sealed class NeutronCollectorUI : DragUIState<NeutronCollectorUI, UIPanel>
    {
        internal static bool Visible { get; set; }
        private UIText? _title;
        private UIText? _processText;
        private UITextPanel<string>? _closeButton;
        public NeutronCollectorOutputSlot? OutputSlot { get; private set; }
        public NeutronCollectorTileEntity TileEntity { get; }
        public NeutronCollectorUI(NeutronCollectorTileEntity tileEntity) => TileEntity = tileEntity;
        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.SetPadding(5);
            panel.Width.Set(320, 0);
            panel.Height.Set(200, 0);
            panel.HAlign = 0.2f;
            panel.VAlign = 0.4f;
            panel.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            Append(panel);
            _title = new UIText(ModContent.GetModItem(ModContent.ItemType<NeutronCollector>()).DisplayName.Value)
            {
                HAlign = 0.5f
            };
            _title.Top.Set(-30, 0);
            panel.Append(_title);
            _processText = new UIText("Process: ")
            {
                HAlign = 0.5f,
                VAlign = 0.85f
            };
            panel.Append(_processText);
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
            panel.Append(_closeButton);
            OutputSlot = new NeutronCollectorOutputSlot();
            OutputSlot.Width.Set(78, 0);
            OutputSlot.Height.Set(78, 0);
            OutputSlot.Top.Set(0, 0.3f);
            OutputSlot.Left.Set(0, 0.38f);
            panel.Append(OutputSlot);
        }
        public override void OnActivate()
        {
            panel.Left = TileEntity.Styles[0];
            panel.Top = TileEntity.Styles[1];
        }
        public override void OnDeactivate()
        {
            TileEntity.Styles[0] = panel.Left;
            TileEntity.Styles[1] = panel.Top;
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