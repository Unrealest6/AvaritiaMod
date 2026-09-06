namespace AvaritiaMod.Content.Tiles
{
    public sealed class InfinityBlockTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            HitSound = SoundID.Dig;
            DustType = DustID.ShimmerSplash;
            MinPick = 210;
            Main.tileFrameImportant[Type] = true;
            Main.tileSolid[Type] = true;
            Main.tileNoAttach[Type] = false;
            Main.tileBlockLight[Type] = true;
            RegisterItemDrop(ModContent.ItemType<InfinityBlock>());
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
            Main.tile[i, j].TileFrameX = (short)(frame.X * 18 + 2 + 110 * (InfinityBlock.AnimatedTexture?.CurrentFrameIndex ?? 0));
            Main.tile[i, j].TileFrameY = (short)(frame.Y * 18 + 2);
        }
        private Point GetFrameFromMask(int mask)
        {
            return mask switch
            {
                0 => new Point(0, 0),
                1 => new Point(1, 1),
                2 => new Point(0, 1),
                4 => new Point(3, 1),
                8 => new Point(2, 1),
                3 => new Point(1, 2),
                12 => new Point(0, 2),
                5 => new Point(5, 2),
                9 => new Point(4, 2),
                6 => new Point(3, 2),
                10 => new Point(2, 2),
                13 => new Point(0, 3),
                14 => new Point(1, 3),
                7 => new Point(2, 3),
                11 => new Point(3, 3),
                15 => new Point(0, 4),
                _ => new Point(0, 0)
            };
        }
    }
}