using AvaritiaMod.Content.Items;

namespace AvaritiaMod.Content.Items.Consumables
{
    public sealed class CosmicMeatballs : FrameItem
    {
        protected override int FrameCount => 8;
        protected override int FrameDuration => 3;
        protected override FrameDef[] FrameTimeline =>
        [
            0, 0, 0,
            1, 1, 1,
            2, 2, 2,
            3, 3,
            4, 4,
            5,
            6,
            7,
            6,
            5,
            4, 4, 3, 3,
            2, 2, 2, 1, 1, 1
        ];
        private static readonly List<int> ItemTypes = [ModContent.ItemType<PileOfNeutrons>(), 261, 261, 2425, 2425, 2426, 2426, 2427, 2427, 3195, 3195, 3532, 3532, 4013, 4013,
            4014, 4014, 4015, 4015, 4016, 4016, 4019, 4019, 4020, 4020, 4022, 4022, 4024, 4024, 4031, 4031, 4032, 4032, 4033, 4033, 4034, 4034, 4037, 4037, 4403, 4403, 4411, 4411];
        public static bool AddItemToRecipe(int type)
        {
            if (ItemTypes.Count >= 81)
            {
                return false;
            }
            ItemTypes.Add(type);
            return true;
        }
        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.maxStack = Item.CommonMaxStack;
            Item.UseSound = SoundID.Item2;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTurn = true;
            Item.useTime = 17;
            Item.useAnimation = 17;
            Item.maxStack = Item.CommonMaxStack;
            Item.buffType = ModContent.BuffType<CelestiallyCharged2>();
            Item.buffTime = 151200;
        }
        public override void AddRecipes()
        {
            AvaritiaRecipe recipe = new(Type, 9, isOrdered: false);
            foreach (int type in ItemTypes)
            {
                recipe.AddIngredient(type);
            }
            recipe.Register();
        }
    }
}