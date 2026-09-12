namespace AvaritiaMod.Common.UI
{
    public sealed class NeutroniumCompressorUI : DragUIState<UIPanel>
    {
        public CompressorInputSlot? InputSlot { get; private set; }
        public CompressorOutputSlot? OutputSlot { get; private set; }
        private ShowItemSlot? _showInputSlot;
        private ShowItemSlot? _showOutputSlot;
        private UIText? _title;
        private UIText? _processText;
        private UIText? _inputText;
        private UIText? _outputText;
        private UITextPanel<string>? _closeButton;
        private CroppedUIImage? _fullSingularity;
        public NeutroniumCompressorTileEntity TileEntity { get; }
        public NeutroniumCompressorUI(NeutroniumCompressorTileEntity tileEntity)
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
            Element.Width.Set(500, 0);
            Element.Height.Set(220, 0);
            Element.HAlign = 0.15f;
            Element.VAlign = 0.7f;
            Element.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            Append(Element);
            _title = new UIText(ModContent.GetModItem(ModContent.ItemType<NeutroniumCompressor>()).DisplayName.Value)
            {
                HAlign = 0.5f
            };
            _title.Top.Set(-30, 0);
            Element.Append(_title);
            _processText = new UIText("") { HAlign = 0.5f, VAlign = 0.8f };
            _inputText = new UIText("") { HAlign = 0.075f, VAlign = 0.3f };
            _outputText = new UIText("") { HAlign = 0.96f, VAlign = 0.3f };
            Element.Append(_processText);
            Element.Append(_inputText);
            Element.Append(_outputText);
            _closeButton = new UITextPanel<string>(Language.GetTextValue("LegacyMisc.56"));
            _closeButton.Width.Set(100, 0);
            _closeButton.Height.Set(40, 0);
            _closeButton.HAlign = 0.99f;
            _closeButton.VAlign = 0.01f;
            _closeButton.OnLeftClick += (_, _) =>
            {
                Visible = false;
                ModContent.GetInstance<NeutroniumCompressorUISystem>().HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
            };
            Element.Append(_closeButton);
            InputSlot = new CompressorInputSlot();
            InputSlot.Top.Set(0, 0.42f);
            InputSlot.Left.Set(0, 0.21f);
            Element.Append(InputSlot);
            OutputSlot = new CompressorOutputSlot();
            OutputSlot.Top.Set(0, 0.35f);
            OutputSlot.Left.Set(0, 0.66f);
            Element.Append(OutputSlot);
            _showInputSlot = new ShowItemSlot();
            _showInputSlot.Top.Set(0, 0.42f);
            _showInputSlot.Left.Set(0, 0.06f);
            Element.Append(_showInputSlot);
            _showOutputSlot = new ShowItemSlot();
            _showOutputSlot.Top.Set(0, 0.42f);
            _showOutputSlot.Left.Set(0, 0.86f);
            Element.Append(_showOutputSlot);
            UIImage image = new(ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/UI/ArmorUI", AssetRequestMode.ImmediateLoad));
            image.Top.Set(0, 0.43f);
            image.Left.Set(0, 0.36f);
            Element.Append(image);
            UIImage airSingularity = new(ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/UI/AirSingularity", AssetRequestMode.ImmediateLoad));
            airSingularity.Top.Set(0, 0.43f);
            airSingularity.Left.Set(0, 0.53f);
            Element.Append(airSingularity);
            _fullSingularity = new CroppedUIImage(ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/UI/FullSingularity", AssetRequestMode.ImmediateLoad).Value, 0, 48, 1);
            _fullSingularity.Top.Set(0, 0.43f);
            _fullSingularity.Left.Set(0, 0.53f);
            Element.Append(_fullSingularity);
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
                ModContent.GetInstance<NeutroniumCompressorUISystem>().HideUI();
            }
            base.Update(gameTime);
            if (!TileEntity.ProcessingItem.IsAir && Singularity.Singularities.TryGetValue(TileEntity.ProcessingItem.type, out Singularity? singularity))
            {
                _showInputSlot?.Item = new Item(TileEntity.ProcessingItem.type);
                _showOutputSlot?.Item = new Item(singularity.Type);
                int req = singularity.RequiredQuantity;
                _processText?.SetText($"{TileEntity.ProcessingItem.stack} / {req}");
                _fullSingularity?.OffsetY = 48 - (int)(TileEntity.ProcessingItem.stack / (float)req * 48);
                _inputText?.SetText("Input");
                _outputText?.SetText("Output");
                if (_fullSingularity is { IsMouseHovering: true })
                {
                    Main.instance.MouseText((TileEntity.ProcessingItem.stack / (float)req * 100f).ToString("F2") + "%");
                }
            }
            else
            {
                _showInputSlot?.Item.TurnToAir();
                _showOutputSlot?.Item.TurnToAir();
                _processText?.SetText("");
                _fullSingularity?.OffsetY = 48;
                _inputText?.SetText("");
                _outputText?.SetText("");
                if (_fullSingularity is { IsMouseHovering: true })
                {
                    Main.instance.MouseText("NaN%");
                }
            }
        }
    }
}