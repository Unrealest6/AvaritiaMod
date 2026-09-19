namespace AvaritiaMod.Common.GlobalWalls
{
    public class AvaritiaGlobalWall : GlobalWall
    {
        public override void ModifyLight(int i, int j, int type, ref float r, ref float g, ref float b)
        {
            //穿戴无尽头盔时自己抬高三通道光照，不再调用 base
            if (Main.LocalPlayer.armor[0].ModItem is InfinityHelmet)
            {
                r = MathHelper.Clamp(r + 0.5f, 0, 1);
                g = MathHelper.Clamp(g + 0.5f, 0, 1);
                b = MathHelper.Clamp(b + 0.5f, 0, 1);
                return;
            }
            base.ModifyLight(i, j, type, ref r, ref g, ref b);
        }
    }
}