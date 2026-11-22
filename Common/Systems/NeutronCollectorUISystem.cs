namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 中子态素收集器UI系统：把界面绑到物块实体，拖拽与显示由组合进来的 <see cref="DragPanelHost"/> 负责。
    /// </summary>
    public sealed class NeutronCollectorUISystem : ModSystem, IDragUIPanelHost
    {
        public DragPanelHost Panels { get; } = new("Avaritia/NeutronCollectorUI");
        private AvaritiaPanelHost Panel { get; }
        public NeutronCollectorUISystem()
        {
            Panel = new AvaritiaPanelHost(Panels,
                static (entity, panel) => ((NeutronCollectorTileEntity)entity).NeutronCollectorUI = panel as NeutronCollectorUI);
        }
        /// <summary>当前打开的收集器界面（没有则为 null）。</summary>
        public static NeutronCollectorUI? CurrentUI => ModContent.GetInstance<NeutronCollectorUISystem>().Panels.Current as NeutronCollectorUI;
        /// <summary>指定位置的物块是不是当前界面绑定的那个。</summary>
        public bool IsTileCurrent(int i, int j) => Panel.IsTileCurrent(i, j);
        /// <summary>打开界面（同一个物块会复用已有实例）。</summary>
        public void ShowUI(NeutronCollectorTileEntity tileEntity) => Panel.Show<NeutronCollectorUI>(tileEntity);
        /// <summary>关闭界面。</summary>
        public void HideUI() => Panel.Hide();
        public override void UpdateUI(GameTime gameTime) => Panel.Update();
    }
}
