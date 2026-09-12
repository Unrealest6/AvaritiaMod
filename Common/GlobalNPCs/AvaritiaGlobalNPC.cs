namespace AvaritiaMod.Common.GlobalNPCs
{
    public sealed class AvaritiaGlobalNPC : GlobalNPC
    {
        /// <summary>
        /// 确保每个npc都有该GlobalNPC的新实例
        /// </summary>
        public override bool InstancePerEntity => true;
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            //处理拜月教邪教徒的掉落逻辑
            if (npc.type == NPCID.CultistBoss)
            {
                //添加下界之星作为额外掉落物
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NetherStar>()));
            }
        }
    }
}