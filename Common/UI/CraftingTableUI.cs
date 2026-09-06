namespace AvaritiaMod.Common.UI
{
    public abstract class CraftingTableUI : DragUIState<CraftingTableUI, UIPanel>
    {
        internal static bool Visible { get; set; }
        private static float SlotSize => 54;
        private static string ArmorPath => "AvaritiaMod/Assets/Textures/UI/ArmorUI";
        public readonly CraftingTableTileEntity TileEntity;
        public AvaritiaCreateItemSlot? CreateSlot { get; private set; }
        public AvaritiaItemSlot[,]? Slots { get; private set; }
        protected abstract BoundedSize Size { get; }
        protected virtual Vector2 PanelSize => new(480, 200);
        protected virtual Vector2 ListSize => new(92, 160);
        protected virtual float ListHAlign => 0.52f;
        protected virtual float ScrollbarHAlign => 0.61f;
        protected virtual float ArmorHAlign => 0.65f;
        protected virtual float ArmorVAlign => 0.4f;
        protected virtual float createSlotHAlign => 0.97f;
        protected virtual string TitleText => string.Empty;
        protected virtual int ScrollbarViewMin => 66;
        protected virtual int ScrollbarViewMax => 333;
        protected UIText? Title { get; private set; }
        protected CraftingTableUI(CraftingTableTileEntity tileEntity)
        {
            TileEntity = tileEntity;
        }
        public override void Update(GameTime gameTime)
        {
            if (Main.keyState.IsKeyDown(Keys.Escape))
            {
                ModContent.GetInstance<CraftingTableUISystem>().HideUI();
            }
            base.Update(gameTime);
        }
        public override void OnActivate()
        {
            if (TileEntity.Items is not null && Slots is not null)
            {
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        Slots[x, y].Item = TileEntity.Items[x, y].Clone();
                    }
                }
            }
            panel.Left = TileEntity.Styles[0];
            panel.Top = TileEntity.Styles[1];
        }
        public override void OnDeactivate()
        {
            TileEntity.Styles[0] = panel.Left;
            TileEntity.Styles[1] = panel.Top;
        }
        public override void OnInitialize()
        {
            InitPanel();
            InitImage();
            InitTitle();
            InitSlots();
            InitCloseButton();
            InitRecipeList();
        }
        private void InitPanel()
        {
            panel.SetPadding(5);
            panel.Width.Set(PanelSize.X, 0);
            panel.Height.Set(PanelSize.Y, 0);
            panel.HAlign = 0.6f;
            panel.VAlign = 0.4f;
            panel.BackgroundColor = new Color(63, 82, 151) * 0.8f;
            Append(panel);
        }
        private void InitImage()
        {
            UIImage image = new(ModContent.Request<Texture2D>(ArmorPath, AssetRequestMode.ImmediateLoad));
            image.Left.Set(0, ArmorHAlign);
            image.Top.Set(0, ArmorVAlign);
            panel.Append(image);
        }
        private void InitTitle()
        {
            Title = new UIText(TitleText) { HAlign = 0.5f };
            Title.Top.Set(-30, 0);
            panel.Append(Title);
        }
        private void InitSlots()
        {
            float start = (panel.Height.Pixels - (Size + 0.1f) * SlotSize) / 2;
            Slots = new AvaritiaItemSlot[Size, Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Slots[x, y] = new AvaritiaItemSlot(x, y);
                    Slots[x, y].Left.Set(start + x * SlotSize, 0);
                    Slots[x, y].Top.Set(start + y * SlotSize, 0);
                    panel.Append(Slots[x, y]);
                }
            }
        }
        private void InitCloseButton()
        {
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
            panel.Append(close);
        }
        private void InitRecipeList()
        {
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
            panel.Append(list);
            panel.Append(scrollbar);
            CreateSlot.Width.Set(78f, 0);
            CreateSlot.Height.Set(78f, 0);
            CreateSlot.HAlign = createSlotHAlign;
            CreateSlot.VAlign = 0.5f;
            panel.Append(CreateSlot);
        }
    }
}