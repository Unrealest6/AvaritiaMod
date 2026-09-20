namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 中子态素压缩机UI组件
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public sealed class NeutroniumCompressorUI : DragUIState<UIPanel>, ITileEntityUI<NeutroniumCompressorTileEntity>
    {
        /// <summary>
        /// 输入槽 UI 元素实例
        /// </summary>
        public CompressorInputSlot? InputSlot { get; private set; }
        /// <summary>
        /// 输出槽 UI 元素实例
        /// </summary>
        public CompressorOutputSlot? OutputSlot { get; private set; }
        /// <summary>
        /// 中子态素压缩机物块实体实例
        /// </summary>
        public NeutroniumCompressorTileEntity TileEntity { get; }
        /// <summary>
        /// 输入物品展示槽 UI 实例
        /// </summary>
        private ShowItemSlot? _showInputSlot;
        /// <summary>
        /// 输出物品展示槽 UI 实例
        /// </summary>
        private ShowItemSlot? _showOutputSlot;
        /// <summary>
        /// 标题文本 UI 实例
        /// </summary>
        private UIText? _title;
        /// <summary>
        /// 处理进度文本 UI 实例
        /// </summary>
        private UIText? _processText;
        /// <summary>
        /// 输入文本标签 UI 实例
        /// </summary>
        private UIText? _inputText;
        /// <summary>
        /// 输出文本标签 UI 实例
        /// </summary>
        private UIText? _outputText;
        /// <summary>
        /// 关闭按钮 UI 实例
        /// </summary>
        private UITextPanel<string>? _closeButton;
        /// <summary>
        /// 奇点填充进度图像 UI 实例
        /// </summary>
        private CroppedUIImage? _fullSingularity;
        /// <summary>
        /// 构造方法，使用反射构造
        /// </summary>
        /// <param name="tileEntity">中子态素压缩机物块实体实例</param>
        public NeutroniumCompressorUI(NeutroniumCompressorTileEntity tileEntity)
        {
            TileEntity = tileEntity;
            Element = new UIPanel();
        }
        /// <summary>
        /// 初始化 UI 组件。创建主面板、标题、进度文本、输入输出槽、箭头图以及关闭按钮。
        /// </summary>
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
            UIImage image = new(ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/UI/ArrowUI", AssetRequestMode.ImmediateLoad));
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
            //联机下打开界面时主动要一次真实状态：本地镜像可能停在上一次广播，显示会过期。
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                AvaritiaNet.RequestCompressorAction(TileEntity.Position, output: false, AvaritiaNet.CompressorAction.Resync, new Item());
            }
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
        /// 每帧更新 UI。处理 Escape 键关闭，刷新处理进度、输入输出展示槽及奇点填充效果。
        /// </summary>
        /// <param name="gameTime">游戏时间信息。</param>
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