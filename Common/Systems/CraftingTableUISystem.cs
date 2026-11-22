namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 工作台UI系统：只负责“把工作台界面绑到物块上”，拖拽与显示交给组合进来的 <see cref="DragPanelHost"/>。
    /// </summary>
    public sealed class CraftingTableUISystem : ModSystem, IDragUIPanelHost
    {
        public DragPanelHost Panels { get; } = new("Avaritia/CraftingTableUI");
        private AvaritiaPanelHost Panel { get; }
        public CraftingTableUISystem()
        {
            Panel = new AvaritiaPanelHost(Panels,
                static (entity, panel) => ((CraftingTableTileEntity)entity).CraftingTableUI = panel as CraftingTableUI);
        }
        /// <summary>当前打开的工作台界面（没有则为 null）。</summary>
        public static CraftingTableUI? CurrentUI => ModContent.GetInstance<CraftingTableUISystem>().Panels.Current as CraftingTableUI;
        /// <summary>指定位置的物块是不是当前界面绑定的那个。</summary>
        public bool IsTileCurrent(int i, int j) => Panel.IsTileCurrent(i, j);
        /// <summary>按指定界面类型打开（同一个物块会复用已有实例）。</summary>
        public void ShowUI<TUI>(CraftingTableTileEntity tileEntity) where TUI : UIState, IDraggablePanel<UIPanel>
            => Panel.Show<TUI>(tileEntity);
        /// <summary>关闭界面。</summary>
        public void HideUI() => Panel.Hide();
        public override void UpdateUI(GameTime gameTime) => Panel.Update();
    }
}
