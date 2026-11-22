namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 与物块实体绑定的面板宿主：在 <see cref="DragPanelHost"/> 之上加“按物块打开 / 复用实例 / 走远自动关闭”。
    /// <para>这些逻辑原先挂在 <c>AvaritiaUISystem&lt;T, TEntity&gt;</c> 泛型基类上（还要再套一层 CRTP），
    /// 现在只是一个普通类：UI 类型在创建时给出，绑定动作用委托注入，不再有泛型参数。</para>
    /// </summary>
    public sealed class AvaritiaPanelHost
    {
        private readonly DragPanelHost _panels;
        private readonly Action<TileEntity, UIState> _bindToEntity;
        private Point16? _tilePos;
        /// <param name="panels">组合进来的面板宿主。</param>
        /// <param name="bindToEntity">把界面写回物块实体（例如 <c>entity.CraftingTableUI = ui</c>）。</param>
        public AvaritiaPanelHost(DragPanelHost panels, Action<TileEntity, UIState> bindToEntity)
        {
            _panels = panels;
            _bindToEntity = bindToEntity;
        }
        /// <summary>当前界面绑定的物块坐标；为空表示没有界面。</summary>
        public Point16? TilePosition => _tilePos;
        /// <summary>指定位置的物块是不是当前界面绑定的那个。</summary>
        public bool IsTileCurrent(int i, int j) => _tilePos is { } pos && pos.X == i && pos.Y == j;
        /// <summary>每帧检查：玩家离物块太远就自动关闭。</summary>
        public void Update()
        {
            if (_tilePos is { } pos && _panels.IsOpen
                && !Main.LocalPlayer.InInteractionRange(pos.X, pos.Y, TileReachCheckSettings.Simple))
            {
                Hide();
            }
        }
        /// <summary>按 <typeparamref name="TUI"/> 打开界面并绑定到物块。</summary>
        public void Show<TUI>(TileEntity tileEntity) where TUI : UIState, IDraggablePanel<UIPanel> => Show(typeof(TUI), tileEntity);
        /// <summary>按指定类型打开界面并绑定到物块（同一个物块再次打开时复用已有实例）。</summary>
        public void Show(Type uiType, TileEntity tileEntity)
        {
            Point16 position = tileEntity.Position;
            // 同一个物块再次打开时复用已有实例：新建实例会从实体重新读一次内容物，
            // 而实体这一刻可能还是旧数据（界面里的改动尚未写回），玩家手上的物品会再出现一份。
            if (_panels.Current is { } current && current.GetType() == uiType
                && _tilePos is { } previous && previous.X == position.X && previous.Y == position.Y)
            {
                _bindToEntity(tileEntity, current);
                _panels.Show(current);
                return;
            }
            if (Activator.CreateInstance(uiType, tileEntity) is not UIState panel)
            {
                EternalLog.Error($"{uiType.Name} is not a valid UI for {GetType().Name}; the UI was not opened.");
                return;
            }
            _bindToEntity(tileEntity, panel);
            _panels.Show(panel);
            _tilePos = position;
        }
        /// <summary>关闭界面：先写回槽位内容、丢弃未结束的槽位拖拽，再交给面板宿主。</summary>
        public void Hide()
        {
            //关闭前把槽位内容写回实体 / 服务端，并丢弃未结束的槽位拖拽：
            //否则实体里的旧数据会在下次打开时把物品“变回来”，残留的拖拽快照也会在下次交互时被套用
            if (_panels.Current is CraftingTableUI table)
            {
                AvaritiaItemSlot.SyncSlotsOfParent(table);
            }
            DragManager.MouseUp();
            _tilePos = null;
            _panels.Hide();
        }
    }
}
