namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 无尽贪婪UI工具
    /// </summary>
    public static class AvaritiaUIUtils
    {
        /// <summary>
        /// 将物品移入玩家背包，优先合并同种堆叠，否则放入空槽。
        /// </summary>
        public static void MoveItemToPlayerInventory(Item source)
        {
            if (source.IsAir != false)
            {
                return;
            }
            Player player = Main.LocalPlayer;
            for (int i = 0; i < 50 && !source.IsAir; i++)
            {
                Item inv = player.inventory[i];
                if (inv?.IsAir != false || inv.type != source.type || inv.maxStack != source.maxStack)
                {
                    continue;
                }
                int space = inv.maxStack - inv.stack;
                if (space <= 0)
                {
                    continue;
                }
                if (space >= source.stack)
                {
                    inv.stack += source.stack;
                    source.TurnToAir();
                    SoundEngine.PlaySound(SoundID.Grab);
                    return;
                }
                inv.stack = inv.maxStack;
                source.stack -= space;
            }
            if (source.IsAir)
            {
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }
            for (int i = 0; i < 50; i++)
            {
                if (!player.inventory[i].IsAir)
                {
                    continue;
                }
                player.inventory[i] = source.Clone();
                player.inventory[i].stack = source.stack;
                source.TurnToAir();
                SoundEngine.PlaySound(SoundID.Grab);
                return;
            }
            SoundEngine.PlaySound(SoundID.MenuTick);
        }
        /// <summary>
        /// 绘制单个物品槽位（背景，图标，数量）
        /// </summary>
        public static void DrawItemSlot(SpriteBatch spriteBatch, Item item, Vector2 position, Texture2D bgTexture, Color bgColor = default, Color itemColor = default, float scale = 1f)
        {
            if (bgColor == default)
            {
                bgColor = Color.White;
            }
            if (itemColor == default)
            {
                itemColor = Color.White;
            }
            Vector2 vector = bgTexture.Size() * scale;
            spriteBatch.Draw(bgTexture, position, null, bgColor, 0f, default, scale, SpriteEffects.None, 0f);
            if (item.type <= ItemID.None || item.stack <= 0)
            {
                return;
            }
            float itemScale = ItemSlot.DrawItemIcon(item, 0, spriteBatch, position + vector / 2f, scale, 32f, itemColor);
            if (item.stack > 1)
            {
                ChatManager.DrawColorCodedStringWithShadow(spriteBatch, FontAssets.ItemStack.Value, item.stack.ToString(), position + new Vector2(10f, 26f) * scale, bgColor
                    , 0f, Vector2.Zero, new Vector2(scale), -1f, scale);
            }
            if (ItemID.Sets.TrapSigned[item.type])
            {
                spriteBatch.Draw(TextureAssets.Wire.Value, position + new Vector2(40f, 40f) * scale, new Rectangle(4, 58, 8, 8), bgColor, 0f, new Vector2(4f)
                    , 1f, SpriteEffects.None, 0f);
            }
            if (ItemID.Sets.DrawUnsafeIndicator[item.type])
            {
                Vector2 vector2 = new Vector2(-4f, -4f) * scale;
                Texture2D value7 = TextureAssets.Extra[ExtrasID.UnsafeIndicator].Value;
                Rectangle rectangle2 = value7.Frame();
                spriteBatch.Draw(value7, position + vector2 + new Vector2(40f, 40f) * scale, rectangle2, bgColor, 0f, rectangle2.Size() / 2f, 1f, SpriteEffects.None, 0f);
            }
            if (!item.expertOnly || Main.expertMode)
            {
                return;
            }
            Vector2 position3 = position + vector / 2f - TextureAssets.Cd.Value.Size() * scale / 2f;
            spriteBatch.Draw(TextureAssets.Cd.Value, position3, null, Color.White, 0f, default, itemScale, SpriteEffects.None, 0f);
        }
    }
}