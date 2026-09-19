namespace AvaritiaMod.Common.GlobalNPCs
{
    public sealed class AvaritiaGlobalNPC : GlobalNPC
    {
        /// <summary>
        /// 每个 NPC 各持一份本类实例
        /// </summary>
        public override bool InstancePerEntity => true;
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            //拜月教邪教徒固定掉落下界之星
            if (npc.type == NPCID.CultistBoss)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NetherStar>()));
            }
        }
    }
}