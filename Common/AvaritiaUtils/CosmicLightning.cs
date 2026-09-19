namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 宇宙闪电
    /// </summary>
    public sealed class CosmicLightning
    {
        /// <summary>
        /// 活跃的闪电列表
        /// </summary>
        private readonly List<LightningBolt> _activeBolts = [];
        /// <summary>
        /// 生成计数器
        /// </summary>
        private int _spawnTimer;
        /// <summary>
        /// 每 6 帧有 1/3 概率生成新闪电，再更新并绘制全部闪电。
        /// 会结束并重开调用方的 <paramref name="spriteBatch"/>（附加混合 + 游戏缩放矩阵）。
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="center">闪电环绕中心位置</param>
        /// <param name="radius">闪电覆盖半径大小</param>
        public void UpdateAndDrawAll(SpriteBatch spriteBatch, Vector2 center, float radius)
        {
            _spawnTimer++;
            if (_spawnTimer > 5)
            {
                _spawnTimer = 0;
                if (Main.rand.NextBool(3))
                {
                    _activeBolts.Add(new LightningBolt(center, radius, Main.rand.NextFloat(MathHelper.TwoPi), Main.rand.NextFloat(-0.8f, 0.8f)));
                }
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
            for (int i = _activeBolts.Count - 1; i >= 0; i--)
            {
                _activeBolts[i].Update();
                if (!_activeBolts[i].IsActive)
                {
                    _activeBolts.RemoveAt(i);
                }
                else
                {
                    _activeBolts[i].Draw(spriteBatch);
                }
            }
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.ZoomMatrix);
        }
        /// <summary>
        /// 清除所有闪电
        /// </summary>
        public void Clear()
        {
            _activeBolts.Clear();
            _spawnTimer = 0;
        }
    }
    /// <summary>
    /// 闪电特效
    /// </summary>
    public sealed class LightningBolt
    {
        /// <summary>
        /// 是否活跃
        /// </summary>
        public bool IsActive => _life > 0;
        /// <summary>
        /// 点集数组
        /// </summary>
        private readonly Vector2[] _points;
        /// <summary>
        /// 分支列表
        /// </summary>
        private readonly List<List<Vector2>> _branches = [];
        /// <summary>
        /// 最大持续时间/tick
        /// </summary>
        private readonly int _maxLife;
        /// <summary>
        /// 厚度
        /// </summary>
        private readonly float _thickness;
        /// <summary>
        /// 颜色
        /// </summary>
        private readonly Color _color;
        /// <summary>
        /// 闪烁偏移量
        /// </summary>
        private readonly float _flickerOffset;
        /// <summary>
        /// 持续时间/tick
        /// </summary>
        private int _life;
        /// <summary>
        /// 生成闪电路径
        /// </summary>
        /// <param name="start">开始位置</param>
        /// <param name="end">结束位置</param>
        /// <param name="roughness">粗糙度</param>
        /// <param name="segments">段数</param>
        /// <param name="seed">种子</param>
        /// <returns>路径位置列表</returns>
        private static List<Vector2> GenerateLightningPath(Vector2 start, Vector2 end, float roughness, int segments, int seed)
        {
            List<Vector2> points = [start];
            Random rand = new(seed);
            Vector2 dir = end - start;
            float length = dir.Length();
            Vector2 normal = new(-dir.Y, dir.X);
            normal.Normalize();
            int totalSegments = Math.Max(1, segments);
            for (int i = 1; i < totalSegments; i++)
            {
                float t = (float)i / totalSegments;
                Vector2 point = start + dir * t;
                float offset = length * roughness * (float)(rand.NextDouble() * 2.0 - 1.0) * (1f - t * 0.5f);
                point += normal * offset;
                points.Add(point);
            }
            points.Add(end);
            return points;
        }
        /// <summary>
        /// 绘制闪电线段
        /// </summary>
        /// <param name="spriteBatch"></param>
        /// <param name="points">点集位置列表</param>
        /// <param name="color">颜色</param>
        /// <param name="thickness">厚度</param>
        private static void DrawLightningLine(SpriteBatch spriteBatch, IReadOnlyList<Vector2> points, Color color, float thickness)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 start = points[i];
                Vector2 end = points[i + 1];
                Vector2 diff = end - start;
                float length = diff.Length();
                spriteBatch.Draw(pixel, start, new Rectangle(0, 0, 1, 1), color,
                    MathF.Atan2(diff.Y, diff.X),
                    Vector2.Zero,
                    new Vector2(length, thickness),
                    SpriteEffects.None, 0);
            }
        }
        /// <summary>
        /// 按球面坐标生成一条带随机分支的闪电
        /// </summary>
        /// <param name="center">环绕中心位置</param>
        /// <param name="radius">覆盖半径大小</param>
        /// <param name="angle">偏移角度</param>
        /// <param name="heightFactor">高度系数</param>
        public LightningBolt(Vector2 center, float radius, float angle, float heightFactor)
        {
            _maxLife = Main.rand.Next(8, 15);
            _life = _maxLife;
            _thickness = Main.rand.NextFloat(1.5f, 3f);
            _flickerOffset = Main.rand.NextFloat(100f);
            float colorT = Main.rand.NextFloat();
            _color = Color.Lerp(new Color(100, 220, 255), new Color(255, 100, 200), colorT);
            Vector2 baseDir = new(MathF.Cos(angle), MathF.Sin(angle));
            float verticalOffset = heightFactor * radius * 0.6f;
            Vector2 startPos = center + baseDir * radius * MathF.Cos(heightFactor * MathHelper.PiOver2) + new Vector2(0, verticalOffset);
            Vector2 endPos = startPos + baseDir * radius * 0.3f;
            int seed = (int)(Main.rand.NextFloat() * int.MaxValue);
            _points = [.. GenerateLightningPath(startPos, endPos, 0.2f, 6, seed)];
            int branchCount = Main.rand.Next(1, 4);
            for (int b = 0; b < branchCount; b++)
            {
                if (_points.Length < 3)
                {
                    break;
                }
                int startIdx = Main.rand.Next(1, _points.Length - 1);
                Vector2 branchStart = _points[startIdx];
                Vector2 dirToEnd = _points[^1] - branchStart;
                float angleOffset = Main.rand.NextFloat(-0.8f, 0.8f);
                Vector2 branchDir = dirToEnd.RotatedBy(angleOffset);
                float branchLength = dirToEnd.Length() * Main.rand.NextFloat(0.2f, 0.5f);
                Vector2 branchEnd = branchStart + branchDir.SafeNormalize(Vector2.Zero) * branchLength;
                List<Vector2> branchPoints = GenerateLightningPath(branchStart, branchEnd, 0.3f, 5, seed + b + 1);
                if (branchPoints.Count >= 2)
                {
                    _branches.Add(branchPoints);
                }
            }
        }
        /// <summary>
        /// 更新
        /// </summary>
        public void Update() => _life--;
        /// <summary>
        /// 绘制
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (_life <= 0)
            {
                return;
            }
            float lifeRatio = (float)_life / _maxLife;
            float alpha = lifeRatio * (0.7f + 0.3f * MathF.Sin(Main.GameUpdateCount * 0.5f + _flickerOffset));
            DrawLightningLine(spriteBatch, _points, _color * (alpha * 0.3f), _thickness * 3.0f);
            DrawLightningLine(spriteBatch, _points, _color * (alpha * 0.7f), _thickness * 1.5f);
            DrawLightningLine(spriteBatch, _points, Color.White * alpha, _thickness * 0.6f);
            foreach (List<Vector2> branch in _branches)
            {
                DrawLightningLine(spriteBatch, branch, _color * (alpha * 0.5f), _thickness * 1.5f);
                DrawLightningLine(spriteBatch, branch, Color.White * (alpha * 0.8f), _thickness * 0.5f);
            }
        }
    }
}