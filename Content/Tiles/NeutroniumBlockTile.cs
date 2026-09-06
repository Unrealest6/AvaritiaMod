namespace AvaritiaMod.Content.Tiles
{
    public sealed class NeutroniumBlockTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            HitSound = SoundID.Dig;
            DustType = DustID.Granite;
            MinPick = 210;
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = false;
            Main.tileBlockLight[Type] = true;
            Main.tileMergeDirt[Type] = true;
            TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
            TileObjectData.newTile.CoordinateHeights = [16];
            TileObjectData.newTile.CoordinateWidth = 16;
            TileObjectData.newTile.CoordinatePadding = 2;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);
            RegisterItemDrop(ModContent.ItemType<NeutroniumBlock>());
        }
        public override void PostDraw(int i, int j, SpriteBatch spriteBatch)
        {
            int mask = 0;
            if (Main.tile[i, j - 1].TileType == Type)
            {
                mask |= 1;
            }
            if (Main.tile[i, j + 1].TileType == Type)
            {
                mask |= 2;
            }
            if (Main.tile[i - 1, j].TileType == Type)
            {
                mask |= 4;
            }
            if (Main.tile[i + 1, j].TileType == Type)
            {
                mask |= 8;
            }
            Point frame = GetFrameFromMask(mask);
            Main.tile[i, j].TileFrameX = (short)(frame.Y * 18 + 2);
            Main.tile[i, j].TileFrameY = (short)(frame.X * 18 + 2);
        }
        private Point GetFrameFromMask(int mask)
        {
            return mask switch
            {
                0 => new Point(0, 0),
                1 => new Point(1, 1),
                2 => new Point(1, 0),
                4 => new Point(1, 3),
                8 => new Point(1, 2),
                3 => new Point(2, 1),
                12 => new Point(2, 0),
                5 => new Point(2, 5),
                9 => new Point(2, 4),
                6 => new Point(2, 3),
                10 => new Point(2, 2),
                13 => new Point(3, 0),
                14 => new Point(3, 1),
                7 => new Point(3, 2),
                11 => new Point(3, 3),
                15 => new Point(4, 0),
                _ => new Point(0, 0)
            };
        }
    }
}