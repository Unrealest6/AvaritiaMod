namespace AvaritiaMod.Common.Systems
{
    public abstract class AvaritiaUISystem<T, TEntity> : DragUISystem<AvaritiaUISystem<T, TEntity>, T> where T : DragUIState<UIPanel> where TEntity : TileEntity
    {
        /// <summary>
        /// 当前物块位置
        /// </summary>
        private Point16? _currentTilePos;
        protected virtual void ShowUITileEntity(TEntity tileEntity) { }
        /// <summary>
        /// 指定位置物块是否是该UI对应的物块
        /// </summary>
        /// <param name="i">物块x坐标</param>
        /// <param name="j">物块y坐标</param>
        /// <returns></returns>
        public bool IsTileCurrent(int i, int j) => _currentTilePos.HasValue && _currentTilePos.Value.X == i && _currentTilePos.Value.Y == j;
        /// <summary>
        /// 显示工作台UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <param name="tileEntity">UI物块实体</param>
        public void ShowUI(TEntity tileEntity)
        {
            CurrentUI = (T?)Activator.CreateInstance(typeof(T), tileEntity);
            ShowUITileEntity(tileEntity);
            base.ShowUI();
            _currentTilePos = new Point16(tileEntity.Position.X, tileEntity.Position.Y);
        }
        /// <summary>
        /// 显示工作台UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <typeparam name="TUI"></typeparam>
        /// <param name="tileEntity">UI物块实体</param>
        public void ShowUI<TUI>(TEntity tileEntity) where TUI : DragUIState<UIPanel>
        {
            CurrentUI = Activator.CreateInstance(typeof(TUI), tileEntity) as T;
            ShowUITileEntity(tileEntity);
            base.ShowUI();
            _currentTilePos = new Point16(tileEntity.Position.X, tileEntity.Position.Y);
        }
        /// <summary>
        /// 隐藏工作台UI
        /// </summary>
        public override void HideUI()
        {
            _currentTilePos = null;
            base.HideUI();
        }
        public override void UpdateUI(GameTime gameTime)
        {
            //当玩家离物块位置较远时自动关闭UI
            if (_currentTilePos.HasValue && UserInterface?.CurrentState != null)
            {
                if (!Main.LocalPlayer.InInteractionRange(_currentTilePos.Value.X, _currentTilePos.Value.Y, TileReachCheckSettings.Simple))
                {
                    HideUI();
                }
            }
            base.UpdateUI(gameTime);
        }
    }
}