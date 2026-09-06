namespace AvaritiaMod.Common.Systems
{
    public sealed class NeutronCollectorUISystem : ModSystem
    {
        internal NeutronCollectorUI? CurrentUI { get; private set; }
        private UserInterface? _userInterface;
        private Point16? _currentTilePos;
        public void ShowMyUI(NeutronCollectorTileEntity tileEntity)
        {
            CurrentUI = new NeutronCollectorUI(tileEntity);
            tileEntity.NeutronCollectorUI = CurrentUI;
            CurrentUI.Activate();
            _userInterface?.SetState(CurrentUI);
            _currentTilePos = new Point16(tileEntity.Position.X, tileEntity.Position.Y);
        }
        public void HideUI()
        {
            _userInterface?.SetState(null);
            CurrentUI = null;
            _currentTilePos = null;
            SoundEngine.PlaySound(SoundID.MenuClose);
            NeutronCollectorUI.Visible = false;
        }
        public override void Load() => _userInterface = new UserInterface();
        public override void UpdateUI(GameTime gameTime)
        {
            if (_currentTilePos.HasValue && _userInterface?.CurrentState != null)
            {
                int tileX = _currentTilePos.Value.X;
                int tileY = _currentTilePos.Value.Y;
                if (!Main.LocalPlayer.InInteractionRange(tileX, tileY, TileReachCheckSettings.Simple))
                {
                    HideUI();
                }
            }
            _userInterface?.Update(gameTime);
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int inventoryIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            if (inventoryIndex != -1)
            {
                layers.Insert(inventoryIndex, new LegacyGameInterfaceLayer(
                    "Avaritia: NeutronCollectorUI",
                    delegate
                    {
                        if (_userInterface?.CurrentState != null)
                        {
                            _userInterface.Draw(Main.spriteBatch, new GameTime());
                        }
                        return true;
                    },
                    InterfaceScaleType.UI)
                );
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