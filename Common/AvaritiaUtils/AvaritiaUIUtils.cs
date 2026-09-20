namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 无尽贪婪UI工具
    /// </summary>
    public static class AvaritiaUIUtils
    {
        /// <summary>
        /// 背包（前 50 格）还能装下多少个该物品：先算同类堆叠的剩余空间，再算空格整叠容量。
        /// <para>联机时服务端用它判断“送背包”是否放得下（放不下就不该消耗 / 不该取走）。</para>
        /// </summary>
        /// <param name="player">目标玩家。</param>
        /// <param name="item">要放入的物品（按 type / maxStack 判定）。</param>
        /// <returns>能放下的数量（不超过 <paramref name="item"/> 自身的数量）。</returns>
        public static int FitAmount(Player player, Item item)
        {
            if (item is null || item.IsAir || item.stack <= 0)
            {
                return 0;
            }
            int max = Math.Max(1, item.maxStack);
            int space = 0;
            for (int i = 0; i < 50 && space < item.stack; i++)
            {
                Item inv = player.inventory[i];
                if (inv is null)
                {
                    continue;
                }
                if (inv.IsAir)
                {
                    space += max;
                }
                else if (inv.type == item.type && inv.maxStack == max)
                {
                    space += Math.Max(0, max - inv.stack);
                }
            }
            return Math.Min(space, item.stack);
        }
        /// <summary>
        /// 将物品移入玩家背包，优先合并同种堆叠，否则放入空槽。
        /// </summary>
        public static void MoveItemToPlayerInventory(Item source) => MoveItemToPlayerInventory(Main.LocalPlayer, source);
        /// <summary>
        /// 将物品移入指定玩家的背包（联机时服务端用，服务端才有权改写玩家背包）。
        /// <para>背包装不下时不丢弃：余量留在 <paramref name="source"/> 里，由调用方决定掉落。</para>
        /// </summary>
        /// <param name="player">目标玩家。</param>
        /// <param name="source">要移入的物品；放不下的部分会留在其中。</param>
        public static void MoveItemToPlayerInventory(Player player, Item source)
        {
            if (source.IsAir != false)
            {
                return;
            }
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
        /// 绘制单个物品槽位：背景、图标与数量，另含电线 / 不安全的 / 专家模式标记
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