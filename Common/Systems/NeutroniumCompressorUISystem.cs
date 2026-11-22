namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 中子态素压缩机UI系统：把界面绑到物块实体，拖拽与显示由组合进来的 <see cref="DragPanelHost"/> 负责。
    /// </summary>
    public sealed class NeutroniumCompressorUISystem : ModSystem, IDragUIPanelHost
    {
        public DragPanelHost Panels { get; } = new("Avaritia/NeutroniumCompressorUI");
        private AvaritiaPanelHost Panel { get; }
        public NeutroniumCompressorUISystem()
        {
            Panel = new AvaritiaPanelHost(Panels,
                static (entity, panel) => ((NeutroniumCompressorTileEntity)entity).NeutroniumCompressorUI = panel as NeutroniumCompressorUI);
        }
        /// <summary>当前打开的压缩机界面（没有则为 null）。</summary>
        public static NeutroniumCompressorUI? CurrentUI => ModContent.GetInstance<NeutroniumCompressorUISystem>().Panels.Current as NeutroniumCompressorUI;
        /// <summary>指定位置的物块是不是当前界面绑定的那个。</summary>
        public bool IsTileCurrent(int i, int j) => Panel.IsTileCurrent(i, j);
        /// <summary>打开界面（同一个物块会复用已有实例）。</summary>
        public void ShowUI(NeutroniumCompressorTileEntity tileEntity) => Panel.Show<NeutroniumCompressorUI>(tileEntity);
        /// <summary>关闭界面。</summary>
        public void HideUI() => Panel.Hide();
        public override void UpdateUI(GameTime gameTime) => Panel.Update();
    }
}
