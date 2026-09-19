namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 屏幕震动管理器
    /// </summary>
    public static class ScreenShakeManager
    {
        /// <summary>
        /// 强度
        /// </summary>
        private static float _intensity;
        /// <summary>
        /// 持续时间
        /// </summary>
        private static int _life;
        /// <summary>
        /// 最大持续时间（衰减基准）
        /// </summary>
        private static int _maxLife;
        /// <summary>
        /// 添加震动；比当前更弱且更短的请求会被忽略
        /// </summary>
        /// <param name="intensity">强度</param>
        /// <param name="duration">持续时间</param>
        public static void AddShake(float intensity, int duration)
        {
            if (!(intensity > _intensity) && duration <= _life)
            {
                return;
            }
            _intensity = intensity;
            _life = duration;
            _maxLife = duration;
        }
        /// <summary>
        /// 更新震动
        /// </summary>
        internal static void Update()
        {
            if (_life <= 0)
            {
                return;
            }
            _life--;
            if (_life > 0)
            {
                return;
            }
            _intensity = 0f;
            _maxLife = 0;
        }
        /// <summary>
        /// 取当前帧的屏幕偏移：随机方向抖动，强度按剩余时间线性衰减
        /// </summary>
        internal static Vector2 GetShakeOffset()
        {
            if (_life <= 0 || _intensity <= 0f)
            {
                return Vector2.Zero;
            }
            float fade = (float)_life / _maxLife;
            float currentIntensity = _intensity * fade;
            float offsetX = (Main.rand.NextFloat() * 2f - 1f) * currentIntensity;
            float offsetY = (Main.rand.NextFloat() * 2f - 1f) * currentIntensity;
            return new Vector2(offsetX, offsetY);
        }
    }
}