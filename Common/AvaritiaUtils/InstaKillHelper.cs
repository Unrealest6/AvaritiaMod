namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// “一击必杀”类攻击（无尽剑）的掉落与死亡结算规则。
    /// <para><b>多部件 Boss 必须按本体结算</b>：月亮领主的掉落挂在核心（<see cref="NPCID.MoonLordCore"/>）上，
    /// 眼睛 / 手臂各有自己的（很小的）掉落表，而核心在部件没被清完之前是无敌的、根本打不到。
    /// 所以直接把命中的部件当目标结算，就只会掉出药水之类的小掉落，见 <see cref="ResolveLootTarget"/>。</para>
    /// </summary>
    public static class InstaKillHelper
    {
        /// <summary>命中的是不是月亮领主身上的部件（不含它放出来的小水蛭，那是独立小怪）。</summary>
        public static bool IsMoonLordPart(int npcType) => npcType is NPCID.MoonLordHand or NPCID.MoonLordHead or NPCID.MoonLordFreeEye;
        /// <summary>把掉落 / 死亡结算落到真正的 Boss 本体上（月亮领主部件 → 核心，其它 → 自己）。</summary>
        /// <param name="target">实际被命中的 NPC。</param>
        /// <returns>应当用来结算掉落与死亡的 NPC。</returns>
        public static NPC ResolveLootTarget(NPC target)
        {
            if (!IsMoonLordPart(target.type))
            {
                return target;
            }
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && npc.type == NPCID.MoonLordCore)
                {
                    return npc;
                }
            }
            //只剩残肢（核心已经死了）：按命中的部件自己结算，至少不会把掉落吞掉
            return target;
        }
        /// <summary>
        /// 结算一次必杀：按 <see cref="ResolveLootTarget"/> 生成掉落并让本体死亡。
        /// <para>掉落只在结算目标还活着时生成一次 —— 一次挥动可能同时命中眼睛和手臂，
        /// 否则同一个 Boss 会掉两份。</para>
        /// </summary>
        /// <param name="target">实际被命中的 NPC。</param>
        public static void KillAndLoot(NPC target)
        {
            if (target is not { active: true })
            {
                return;
            }
            NPC lootTarget = ResolveLootTarget(target);
            if (lootTarget is { active: true, life: > 0 })
            {
                lootTarget.NPCLoot();
                lootTarget.life = 0;
                lootTarget.HitEffect(0, 0, true);
            }
            if (ReferenceEquals(lootTarget, target))
            {
                target.life = 0;
                return;
            }
            //命中的部件跟着本体一起收掉
            target.life = 0;
            foreach (NPC npc in Main.npc)
            {
                if (npc.active && !ReferenceEquals(npc, lootTarget) && IsMoonLordPart(npc.type))
                {
                    npc.life = 0;
                }
            }
        }
    }
}