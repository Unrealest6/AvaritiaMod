namespace AvaritiaMod.Common.Systems
{
    public sealed class CraftingTableUISystem : ModSystem
    {
        internal CraftingTableUI? CurrentUI { get; private set; }
        private UserInterface? _userInterface;
        private Point16? _currentTilePos;
        public void ShowMyUI<T>(CraftingTableTileEntity tileEntity) where T : CraftingTableUI
        {
            CurrentUI = (T?)Activator.CreateInstance(typeof(T), tileEntity);
            tileEntity.CraftingTableUI = CurrentUI;
            CurrentUI?.Activate();
            _userInterface?.SetState(CurrentUI);
            _currentTilePos = new Point16(tileEntity.Position.X, tileEntity.Position.Y);
        }
        public void HideUI()
        {
            _userInterface?.SetState(null);
            CurrentUI = null;
            _currentTilePos = null;
            SoundEngine.PlaySound(SoundID.MenuClose);
            CraftingTableUI.Visible = false;
        }
        public override void Load() => _userInterface = new UserInterface();
        public override void UpdateUI(GameTime gameTime)
        {
            if (_currentTilePos.HasValue && _userInterface?.CurrentState != null)
            {
                if (!Main.LocalPlayer.InInteractionRange(_currentTilePos.Value.X, _currentTilePos.Value.Y, TileReachCheckSettings.Simple))
                {
                    HideUI();
                }
            }
            _userInterface?.Update(gameTime);
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(l => l.Name.Equals("Vanilla: Mouse Text"));
            if (index != -1)
            {
                layers.Insert(index, new LegacyGameInterfaceLayer(
                    "Avaritia: CraftingTableUI",
                    delegate { _userInterface?.Draw(Main.spriteBatch, new GameTime()); return true; },
                    InterfaceScaleType.UI));
            }
        }
        public override void Unload()
        {
            _userInterface = null;
            CurrentUI = null;
        }
        internal bool IsTileCurrent(int i, int j) => _currentTilePos.HasValue && _currentTilePos.Value.X == i && _currentTilePos.Value.Y == j;
    }
}