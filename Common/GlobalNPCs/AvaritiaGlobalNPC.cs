namespace AvaritiaMod.Common.GlobalNPCs
{
    public sealed class AvaritiaGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.CultistBoss)
            {
                npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<NetherStar>()));
            }
        }
    }
}