namespace AvaritiaMod.Common.AvaritiaUtils
{
    public sealed class CosmicParticle
    {
        private readonly List<StarParticle> _particles = [];
        public void UpdateAndDraw(SpriteBatch sb, Vector2 center, float radius, float time)
        {
            SpawnParticles(center, radius);
            UpdateAndRender(sb);
        }
        private void SpawnParticles(Vector2 center, float radius)
        {
            int spawnCount = Main.rand.Next(2, 5);
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float height = Main.rand.NextFloat(-0.95f, 0.95f);
                _particles.Add(new StarParticle(center, radius, angle, height));
            }
        }
        private void UpdateAndRender(SpriteBatch sb)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update();
                if (_particles[i].IsDead)
                {
                    _particles.RemoveAt(i);
                }
                else
                {
                    _particles[i].Draw(sb);
                }
            }
        }
        public void Clear()
        {
            _particles.Clear();
        }
    }
    public class StarParticle
    {
        private Vector2 position;
        private Vector2 velocity;
        private float life;
        private readonly float maxLife;
        private readonly float baseSize;
        private readonly Color color;
        private float rotation;
        private readonly float rotationSpeed;
        public bool IsDead => life <= 0;
        public StarParticle(Vector2 center, float radius, float angle, float heightFactor)
        {
            float effectiveRadius = radius * 0.92f;
            float ringRadius = effectiveRadius * MathF.Cos(heightFactor * MathHelper.PiOver2);
            float yOffset = MathF.Sin(heightFactor * MathHelper.PiOver2) * effectiveRadius * 0.6f;
            Vector2 surfaceOffset = new(
                MathF.Cos(angle) * ringRadius,
                yOffset
            );
            position = center + surfaceOffset;
            Vector2 radialDir = surfaceOffset.Length() > 0.1f ? Vector2.Normalize(surfaceOffset) : new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            float speed = Main.rand.NextFloat(2f, 5f);
            velocity = radialDir * speed + Main.rand.NextVector2Circular(0.8f, 0.8f);
            maxLife = Main.rand.Next(25, 50);
            life = maxLife;
            baseSize = Main.rand.NextFloat(3f, 10f);
            float colorRoll = Main.rand.NextFloat();
            color = colorRoll < 0.33f ? new Color(150, 255, 255) : colorRoll < 0.66f ? new Color(80, 100, 255) : new Color(255, 255, 255);
            rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            rotationSpeed = Main.rand.NextFloat(-0.15f, 0.15f);
        }
        public void Update()
        {
            position += velocity;
            velocity *= 0.96f;
            rotation += rotationSpeed;
            life--;
        }
        public void Draw(SpriteBatch sb)
        {
            float lifeRatio = life / maxLife;
            float alpha = lifeRatio < 0.2f ? lifeRatio * 5f : lifeRatio;
            float sizeScale = lifeRatio > 0.7f ? 1f + (1f - lifeRatio) * 2f : 0.5f + lifeRatio * 0.5f;
            float finalSize = baseSize * sizeScale;
            Texture2D tex = TextureAssets.MagicPixel.Value;
            sb.Draw(tex, position, new Rectangle(0, 0, 1, 1), color * alpha, rotation, Vector2.One * 0.5f
                , new Vector2(finalSize * 0.3f, finalSize), SpriteEffects.None, 0);
            sb.Draw(tex, position, new Rectangle(0, 0, 1, 1), color * alpha * 0.7f, rotation + MathHelper.PiOver2
                , Vector2.One * 0.5f, new Vector2(finalSize * 0.3f, finalSize), SpriteEffects.None, 0);
        }
    }
}