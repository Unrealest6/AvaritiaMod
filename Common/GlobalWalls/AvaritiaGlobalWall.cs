namespace AvaritiaMod.Common.GlobalWalls
{
    public class AvaritiaGlobalWall : GlobalWall
    {
        public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b)
        {
            if (Main.LocalPlayer.armor[0].ModItem is not InfinityHelmet)
            {
                return;
            }
            r = MathHelper.Clamp(r + 0.2f, 0, 1);
            g = MathHelper.Clamp(g + 0.2f, 0, 1);
            b = MathHelper.Clamp(b + 0.2f, 0, 1);
        }
    }
}