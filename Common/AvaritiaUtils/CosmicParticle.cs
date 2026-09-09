namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 宇宙粒子
    /// </summary>
    public sealed class CosmicParticle
    {
        /// <summary>
        /// 粒子列表
        /// </summary>
        private readonly List<StarParticle> _particles = [];
        /// <summary>
        /// 更新和绘制全部粒子
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="center">粒子环绕中心位置</param>
        /// <param name="radius">粒子覆盖半径大小</param>
        public void UpdateAndDrawAll(SpriteBatch spriteBatch, Vector2 center, float radius)
        {
            int spawnCount = Main.rand.Next(2, 5);
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float height = Main.rand.NextFloat(-0.95f, 0.95f);
                _particles.Add(new StarParticle(center, radius, angle, height));
            }
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                _particles[i].Update();
                if (!_particles[i].IsActive)
                {
                    _particles.RemoveAt(i);
                }
                else
                {
                    _particles[i].Draw(spriteBatch);
                }
            }
        }
        /// <summary>
        /// 清除所有粒子
        /// </summary>
        public void Clear()
        {
            _particles.Clear();
        }
    }
    /// <summary>
    /// 星空粒子
    /// </summary>
    public sealed class StarParticle
    {
        /// <summary>
        /// 是否活跃
        /// </summary>
        public bool IsActive => _life > 0;
        /// <summary>
        /// 最大持续时间/tick
        /// </summary>
        private readonly int _maxLife;
        /// <summary>
        /// 基础大小
        /// </summary>
        private readonly float _baseScale;
        /// <summary>
        /// 颜色
        /// </summary>
        private readonly Color _color;
        /// <summary>
        /// 旋转速度
        /// </summary>
        private readonly float _rotatedSpeed;
        /// <summary>
        /// 位置
        /// </summary>
        private Vector2 _position;
        /// <summary>
        /// 速度
        /// </summary>
        private Vector2 _velocity;
        /// <summary>
        /// 持续时间/tick
        /// </summary>
        private int _life;
        /// <summary>
        /// 旋转
        /// </summary>
        private float _rotation;
        /// <summary>
        /// 构造方法，用于创建粒子实例
        /// </summary>
        /// <param name="center">环绕中心位置</param>
        /// <param name="radius">覆盖半径大小</param>
        /// <param name="angle">偏移角度</param>
        /// <param name="heightFactor">高度系数</param>
        public StarParticle(Vector2 center, float radius, float angle, float heightFactor)
        {
            float effectiveRadius = radius * 0.92f;
            float ringRadius = effectiveRadius * MathF.Cos(heightFactor * MathHelper.PiOver2);
            float yOffset = MathF.Sin(heightFactor * MathHelper.PiOver2) * effectiveRadius * 0.6f;
            Vector2 surfaceOffset = new(MathF.Cos(angle) * ringRadius, yOffset);
            _position = center + surfaceOffset;
            Vector2 radialDir = surfaceOffset.Length() > 0.1f ? Vector2.Normalize(surfaceOffset) : new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            float speed = Main.rand.NextFloat(2f, 5f);
            _velocity = radialDir * speed + Main.rand.NextVector2Circular(0.8f, 0.8f);
            _maxLife = Main.rand.Next(25, 50);
            _life = _maxLife;
            _baseScale = Main.rand.NextFloat(3f, 10f);
            float colorRoll = Main.rand.NextFloat();
            _color = colorRoll < 0.33f ? new Color(150, 255, 255) : colorRoll < 0.66f ? new Color(80, 100, 255) : new Color(255, 255, 255);
            _rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            _rotatedSpeed = Main.rand.NextFloat(-0.15f, 0.15f);
        }
        /// <summary>
        /// 更新
        /// </summary>
        public void Update()
        {
            _position += _velocity;
            _velocity *= 0.96f;
            _rotation += _rotatedSpeed;
            _life--;
        }
        /// <summary>
        /// 绘制
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            float lifeRatio = (float)_life / _maxLife;
            float alpha = lifeRatio < 0.2f ? lifeRatio * 5f : lifeRatio;
            float sizeScale = lifeRatio > 0.7f ? 1f + (1f - lifeRatio) * 2f : 0.5f + lifeRatio * 0.5f;
            float finalSize = _baseScale * sizeScale;
            Texture2D tex = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(tex, _position, new Rectangle(0, 0, 1, 1), _color * alpha, _rotation, Vector2.One * 0.5f
                , new Vector2(finalSize * 0.3f, finalSize), SpriteEffects.None, 0);
            spriteBatch.Draw(tex, _position, new Rectangle(0, 0, 1, 1), _color * alpha * 0.7f, _rotation + MathHelper.PiOver2
                , Vector2.One * 0.5f, new Vector2(finalSize * 0.3f, finalSize), SpriteEffects.None, 0);
        }
    }
}