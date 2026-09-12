namespace AvaritiaMod.Common.Systems
{
    public sealed class AvaritiaFrameSystem : ModSystem
    {
        /// <summary>
        /// 光环噪声序列图纹理实例
        /// </summary>
        public static FrameTexture? HaloNoise { get; private set; }
        /// <summary>
        /// 十个星星的序列图纹理实例
        /// </summary>
        public static FrameTexture?[] CosmicTextures { get; private set; } = new FrameTexture[10];
        /// <summary>
        /// 在SetupContent方法中对纹理进行加载以保证客户端生效
        /// </summary>
        public override void SetupContent()
        {
            HaloNoise = FrameTextureSystem.Register("HaloNoise", "AvaritiaMod/Assets/Textures/HaloNoise", 8, 3);
            CosmicTextures[0] = FrameTextureSystem.Register("Cosmic0", "AvaritiaMod/Assets/Textures/cosmic_0", 4
                , [new FrameDef(0, 21), 1, 2, 3], 3);
            CosmicTextures[1] = FrameTextureSystem.Register("Cosmic1", "AvaritiaMod/Assets/Textures/cosmic_1", 4,
                [new FrameDef(0, 12), 1, new FrameDef(0, 27), 2, new FrameDef(0, 21), 3], 3);
            CosmicTextures[2] = FrameTextureSystem.Register("Cosmic2", "AvaritiaMod/Assets/Textures/cosmic_2", 5,
                [new FrameDef(0, 48), 1, 1, 1, 2, 2, 3, 4, 3, 4, 3, 2, 2, 1, 1, 1], 3);
            CosmicTextures[3] = FrameTextureSystem.Register("Cosmic3", "AvaritiaMod/Assets/Textures/cosmic_3", 5,
                [new FrameDef(0, 39), 1, new FrameDef(0, 30), 3, new FrameDef(0, 15), 2, new FrameDef(0, 45), 4], 3);
            CosmicTextures[4] = FrameTextureSystem.Register("Cosmic4", "AvaritiaMod/Assets/Textures/cosmic_4", 4,
                [new FrameDef(0, 102), 1, 2, 3], 3);
            CosmicTextures[5] = FrameTextureSystem.Register("Cosmic5", "AvaritiaMod/Assets/Textures/cosmic_5", 4,
                [new FrameDef(0, 54), 1, new FrameDef(0, 12), 3, new FrameDef(0, 42), 2], 3);
            CosmicTextures[6] = FrameTextureSystem.Register("Cosmic6", "AvaritiaMod/Assets/Textures/cosmic_6", 6, 2);
            CosmicTextures[7] = FrameTextureSystem.Register("Cosmic7", "AvaritiaMod/Assets/Textures/cosmic_7", 4, 4);
            CosmicTextures[8] = FrameTextureSystem.Register("Cosmic8", "AvaritiaMod/Assets/Textures/cosmic_8", 7,
                [1, 2, 3, 2, 3, 2, 1, new FrameDef(0, 66), 4, 5, 6, 5, 6, 5, 4, new FrameDef(0, 93), 1, 2, 3, 2, 1, new FrameDef(0, 36)], 3);
            CosmicTextures[9] = FrameTextureSystem.Register("Cosmic9", "AvaritiaMod/Assets/Textures/cosmic_9", 3, 6);
        }
    }
}