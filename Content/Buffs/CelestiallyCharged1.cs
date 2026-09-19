namespace AvaritiaMod.Content.Buffs
{
    /// <summary>天能充盈：饱腹类增益，提升防御、伤害、暴击、攻速、移速与生命再生等属性。</summary>
    public sealed class CelestiallyCharged1 : ModBuff
    {
        public override void SetStaticDefaults()
        {
            BuffID.Sets.IsWellFed[Type] = true;
            BuffID.Sets.IsFedState[Type] = true;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            player.wellFed = true;
            player.statDefense += 10;
            player.GetCritChance(DamageClass.Generic) += 10f;
            player.GetDamage(DamageClass.Generic) += 0.25f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.25f;
            player.GetKnockback(DamageClass.Summon).Base += 2.5f;
            player.moveSpeed += 0.8f;
            player.pickSpeed -= 0.3f;
            player.lifeRegen += 21;
        }
    }
}