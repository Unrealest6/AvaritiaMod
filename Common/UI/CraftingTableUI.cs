namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 工作台UI组件，继承<see cref="DragUIState{T}"/>拥有拖拽功能
    /// </summary>
    public abstract class CraftingTableUI : DragUIState<UIPanel>
    {
        /// <summary>
        /// 槽位尺寸常量
        /// </summary>
        private const float SlotSize = 54;
        /// <summary>
        /// 物块实体实例
        /// </summary>
        public CraftingTableTileEntity TileEntity { get; }
        /// <summary>
        /// 合成输出槽UI元素实例
        /// </summary>
        public AvaritiaCreateItemSlot? CreateSlot { get; private set; }
        /// <summary>
        /// 输入槽UI元素数组
        /// </summary>
        public AvaritiaItemSlot[,]? Slots { get; private set; }
        /// <summary>
        /// 工作台合成槽尺寸
        /// </summary>
        protected abstract BoundedSize Size { get; }
        /// <summary>
        /// 主面板尺寸
        /// </summary>
        protected virtual Vector2 PanelSize => new(480, 200);
        /// <summary>
        /// 配方列表尺寸
        /// </summary>
        protected virtual Vector2 ListSize => new(92, 160);
        /// <summary>
        /// 配方列表水平对齐位置
        /// </summary>
        protected virtual float ListHAlign => 0.52f;
        /// <summary>
        /// 滚动条水平对齐位置
        /// </summary>
        protected virtual float ScrollbarHAlign => 0.61f;
        /// <summary>
        /// 箭头图水平对齐位置
        /// </summary>
        protected virtual float ArmorHAlign => 0.65f;
        /// <summary>
        /// 箭头图垂直对齐位置
        /// </summary>
        protected virtual float ArmorVAlign => 0.4f;
        /// <summary>
        /// 创建槽水平对齐位置
        /// </summary>
        protected virtual float createSlotHAlign => 0.97f;
        /// <summary>
        /// 工作台标题文本
        /// </summary>
        protected virtual string TitleText => string.Empty;
        /// <summary>
        /// 滚动条视图最小值
        /// </summary>
        protected virtual int ScrollbarViewMin => 66;
        /// <summary>
        /// 滚动条视图最大值
        /// </summary>
        protected virtual int ScrollbarViewMax => 333;
        /// <summary>
        /// 标题 UI 文本实例
        /// </summary>
        protected UIText? Title { get; private set; }
        /// <summary>
        /// 初始化工作台 UI，绑定对应的物块实体并创建根面板元素。
        /// </summary>
        /// <param name="tileEntity">该 UI 对应的工作台物块实体。</param>
        protected CraftingTableUI(CraftingTableTileEntity tileEntity)
        {
            TileEntity = tileEntity;
            Element = new UIPanel();
        }
        /// <summary>
        /// 每帧更新 UI。按下 Escape 键时关闭工作台 UI。
        /// </summary>
        /// <param name="gameTime">游戏时间信息。</param>
        public override void Update(GameTime gameTime)
        {
            if (Main.keyState.IsKeyDown(Keys.Escape))
            {
                ModContent.GetInstance<CraftingTableUISystem>().HideUI();
            }
            base.Update(gameTime);
        }
        /// <summary>
        /// UI 激活时调用。从物块实体同步所有槽位物品，并恢复上次保存的面板位置。
        /// </summary>
        public override void OnActivate()
        {
            if (Element is null)
            {
                return;
            }
            if (TileEntity.Items is not null && Slots is not null)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        Slots[x, y].SetItemSilently(TileEntity.Items[x, y]);
                    }
                }
            }
            Element.Left = TileEntity.Styles[0];
            Element.Top = TileEntity.Styles[1];
        }
        /// <summary>
        /// UI 关闭时调用。将当前面板位置保存到物块实体，以便下次打开时恢复。
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
        /// UI 首次初始化时调用。依次创建面板、背景图、标题、槽位、关闭按钮和配方列表。
        /// </summary>

        public override void OnInitialize()
        {
            InitPanel();
            InitImage();
            InitTitle();
            InitSlots();
            InitCloseButton();
            InitRecipeList();
        }
        /// <summary>
        /// 初始化主面板，设置内边距、尺寸、对齐方式和背景颜色，并将其附加到 UI 根节点。
        /// </summary>
        private void InitPanel()
        {
            if (Element is null)
            {
                return;
            }
            Element.SetPadding(5);
            Element.Width.Set(PanelSize.X, 0);
            Element.Height.Set(PanelSize.Y, 0);
            Element.HAlign = 0.6f;
            Element.VAlign = 0.4f;
            Element.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            Append(Element);
        }
        /// <summary>
        /// 初始化装饰箭头图，并按预设对齐方式放置到面板中。
        /// </summary>
        private void InitImage()
        {
            if (Element is null)
            {
                return;
            }
            UIImage image = new(ModContent.Request<Texture2D>("AvaritiaMod/Assets/Textures/UI/ArrowUI", AssetRequestMode.ImmediateLoad));
            image.Left.Set(0, ArmorHAlign);
            image.Top.Set(0, ArmorVAlign);
            Element.Append(image);
        }
        /// <summary>
        /// 初始化标题文本，使用 <see cref="TitleText"/> 的内容并居中显示在面板上方。
        /// </summary>
        private void InitTitle()
        {
            if (Element is null)
            {
                return;
            }
            Title = new UIText(TitleText) { HAlign = 0.5f };
            Title.Top.Set(-30, 0);
            Element.Append(Title);
        }
        /// <summary>
        /// 初始化合成槽位网格。根据 <see cref="Size"/> 创建二维槽位数组，
        /// 并按照居中布局将每个槽位附加到面板上。
        /// </summary>
        private void InitSlots()
        {
            if (Element is null)
            {
                return;
            }
            float start = (Element.Height.Pixels - (Size + 0.1f) * SlotSize) / 2;
            Slots = new AvaritiaItemSlot[Size, Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Slots[x, y] = new AvaritiaItemSlot(x, y);
                    Slots[x, y].Left.Set(start + x * SlotSize, 0);
                    Slots[x, y].Top.Set(start + y * SlotSize, 0);
                    Element.Append(Slots[x, y]);
                }
            }
        }
        /// <summary>
        /// 初始化关闭按钮。点击后隐藏 UI、关闭工作台界面并播放关闭音效。
        /// </summary>
        private void InitCloseButton()
        {
            if (Element is null)
            {
                return;
            }
            UITextPanel<string> close = new(Language.GetTextValue("LegacyMisc.56"));
            close.Width.Set(100, 0);
            close.Height.Set(40, 0);
            close.HAlign = 0.99f;
            close.VAlign = 0.01f;
            close.OnLeftClick += (_, _) =>
            {
                Visible = false;
                ModContent.GetInstance<CraftingTableUISystem>().HideUI();
                SoundEngine.PlaySound(SoundID.MenuClose);
            };
            Element.Append(close);
        }
        /// <summary>
        /// 初始化配方列表。创建可滚动的 <see cref="UIList"/>，将当前尺寸下所有有效配方
        /// 以 <see cref="AvaritiaRecipeItemSlot"/> 的形式加入列表，并放置创建槽和滚动条。
        /// </summary>
        private void InitRecipeList()
        {
            if (Element is null)
            {
                return;
            }
            UIList list = new()
            {
                HAlign = ListHAlign,
                VAlign = 0.5f
            };
            list.Width.Set(ListSize.X, 0);
            list.Height.Set(ListSize.Y, 0);
            UIScrollbar scrollbar = new();
            scrollbar.SetView(ScrollbarViewMin, ScrollbarViewMax);
            scrollbar.HAlign = ScrollbarHAlign;
            scrollbar.VAlign = 0.5f;
            scrollbar.Height.Set(ListSize.Y, 0);
            list.SetScrollbar(scrollbar);
            CreateSlot = new AvaritiaCreateItemSlot();
            foreach (AvaritiaRecipe recipe in AvaritiaRecipe.Recipes.Where(r => r.Size == Size && !r.Result.IsAir))
            {
                list.Add(new AvaritiaRecipeItemSlot(recipe));
            }
            Element.Append(list);
            Element.Append(scrollbar);
            CreateSlot.Width.Set(78f, 0);
            CreateSlot.Height.Set(78f, 0);
            CreateSlot.HAlign = createSlotHAlign;
            CreateSlot.VAlign = 0.5f;
            Element.Append(CreateSlot);
        }
    }
}