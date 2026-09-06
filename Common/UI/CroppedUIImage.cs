namespace AvaritiaMod.Common.UI
{
    public sealed class CroppedUIImage : UIImage
    {
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
        private readonly int _direction;
        private readonly Texture2D _texture;
        public CroppedUIImage(Texture2D texture, int offsetX = 0, int offsetY = 0, int direction = 0) : base(texture)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
            _direction = direction == 1 ? 1 : 0;
            _texture = texture;
        }
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