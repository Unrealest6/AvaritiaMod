namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 能够裁剪纹理绘制的UIImage
    /// </summary>
    public sealed class CroppedUIImage : UIImage
    {
        /// <summary>
        /// X方向偏移量
        /// </summary>
        public int OffsetX { get; set; }
        /// <summary>
        /// Y方向偏移量
        /// </summary>
        public int OffsetY { get; set; }
        /// <summary>
        /// 方向
        /// </summary>
        private readonly int _direction;
        /// <summary>
        /// 纹理资源
        /// </summary>
        private readonly Texture2D _texture;
        /// <summary>
        /// 构造方法，初始化纹理资源，偏移量和方向
        /// </summary>
        /// <param name="texture">纹理实例</param>
        /// <param name="offsetX">X方向偏移量</param>
        /// <param name="offsetY">Y方向偏移量</param>
        /// <param name="direction">方向</param>
        public CroppedUIImage(Texture2D texture, int offsetX = 0, int offsetY = 0, int direction = 0) : base(texture)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            _direction = direction == 1 ? 1 : 0;
            _texture = texture;
        }
        /// <summary>
        /// 处理裁剪纹理的逻辑并绘制
        /// </summary>
        /// <param name="spriteBatch"></param>
        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            CalculatedStyle dimensions = GetDimensions();
            if (ScaleToFit)
            {
                spriteBatch.Draw(_texture, dimensions.ToRectangle(), Color);
                return;
            }
            Rectangle rectangle = new(OffsetX * _direction, OffsetY * _direction, _texture.Width - OffsetX, _texture.Height - OffsetY);
            Vector2 vector = _texture.Size();
            Vector2 vector2 = dimensions.Position() + vector * (1f - ImageScale) / 2f + vector * NormalizedOrigin;
            if (RemoveFloatingPointsFromDrawPosition)
            {
                vector2 = vector2.Floor();
            }
            vector2.X += OffsetX * _direction;
            vector2.Y += OffsetY * _direction;
            spriteBatch.Draw(_texture, vector2, rectangle, Color, Rotation, vector * NormalizedOrigin, ImageScale, SpriteEffects.None, 0f);
        }
    }
}