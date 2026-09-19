namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 无尽贪婪与物块实体绑定的拖拽UI系统
    /// </summary>
    /// <typeparam name="T">拖拽UI类型</typeparam>
    /// <typeparam name="TEntity">物块实体实例</typeparam>
    public abstract class AvaritiaUISystem<T, TEntity> : DragUISystem<AvaritiaUISystem<T, TEntity>, T> where T : DragUIState<UIPanel> where TEntity : TileEntity
    {
        /// <summary>
        /// 当前打开的 UI 所绑定的物块坐标；为空表示没有 UI
        /// </summary>
        private Point16? _currentTilePos;
        /// <summary>
        /// 在工作台物块实体中绑定展示的UI
        /// </summary>
        /// <param name="tileEntity">物块实体实例</param>
        protected virtual void ShowUITileEntity(TEntity tileEntity) { }
        /// <summary>
        /// 指定位置物块是否是该UI对应的物块
        /// </summary>
        /// <param name="i">物块x坐标</param>
        /// <param name="j">物块y坐标</param>
        public bool IsTileCurrent(int i, int j) => _currentTilePos.HasValue && _currentTilePos.Value.X == i && _currentTilePos.Value.Y == j;
        /// <summary>
        /// 按类级 UI 类型 <c>T</c> 显示 UI 并记录绑定的物块坐标
        /// </summary>
        /// <param name="tileEntity">UI物块实体</param>
        public void ShowUI(TEntity tileEntity)
        {
            CurrentUI = (T?)Activator.CreateInstance(typeof(T), tileEntity);
            ShowUITileEntity(tileEntity);
            base.ShowUI();
            _currentTilePos = new Point16(tileEntity.Position.X, tileEntity.Position.Y);
        }
        /// <summary>
        /// 按指定 UI 类型 <c>TUI</c> 显示 UI 并记录绑定的物块坐标
        /// </summary>
        /// <param name="tileEntity">UI物块实体</param>
        public void ShowUI<TUI>(TEntity tileEntity) where TUI : DragUIState<UIPanel>
        {
            //同一个物块再次打开时复用已有实例：新建实例会从实体重新读一次内容物，
            //而实体这一刻可能还是旧数据（界面里的改动尚未写回），玩家手上的物品会再出现一份。
            if (CurrentUI is TUI && CurrentUI is ITileEntityUI<TEntity> bound && ReferenceEquals(bound.TileEntity, tileEntity))
            {
                ShowUITileEntity(tileEntity);
                base.ShowUI();
                _currentTilePos = new Point16(tileEntity.Position.X, tileEntity.Position.Y);
                return;
            }
            //TUI 必须真的是本系统的 UI 类型，否则 CreateInstance 的结果会转成 null
            if (Activator.CreateInstance(typeof(TUI), tileEntity) is not T ui)
            {
                EternalLog.Error($"{typeof(TUI).Name} is not a valid UI for {GetType().Name}; the UI was not opened.");
                return;
            }
            CurrentUI = ui;
            ShowUITileEntity(tileEntity);
            base.ShowUI();
            _currentTilePos = new Point16(tileEntity.Position.X, tileEntity.Position.Y);
        }
        /// <summary>
        /// 隐藏工作台UI
        /// </summary>
        public override void HideUI()
        {
            //关闭前把槽位内容写回实体 / 服务端：否则实体里的旧数据会在下次打开时把物品“变回来”
            if (CurrentUI is CraftingTableUI table)
            {
                AvaritiaItemSlot.SyncSlotsOfParent(table);
            }
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