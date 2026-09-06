namespace AvaritiaMod.Common.AvaritiaUtils
{
    public static class ScreenShakeManager
    {
        private static float _intensity;
        private static int _duration;
        private static int _maxDuration;
        public static void AddShake(float intensity, int duration)
        {
            if (!(intensity > _intensity) && duration <= _duration)
            {
                return;
            }
            _intensity = intensity;
            _duration = duration;
            _maxDuration = duration;
        }
        internal static void Update()
        {
            if (_duration <= 0)
            {
                return;
            }
            _duration--;
            if (_duration > 0)
            {
                return;
            }
            _intensity = 0f;
            _maxDuration = 0;
        }
        internal static Vector2 GetShakeOffset()
        {
            if (_duration <= 0 || _intensity <= 0f)
            {
                return Vector2.Zero;
            }
            float fade = (float)_duration / _maxDuration;
            float currentIntensity = _intensity * fade;
            float offsetX = (Main.rand.NextFloat() * 2f - 1f) * currentIntensity;
            float offsetY = (Main.rand.NextFloat() * 2f - 1f) * currentIntensity;
            return new Vector2(offsetX, offsetY);
        }
    }
}