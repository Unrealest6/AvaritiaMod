namespace AvaritiaMod.Common.AvaritiaUtils
{
    /// <summary>
    /// 球体网格生成系统
    /// </summary>
    public static class SphereMeshGenerator
    {
        /// <summary>
        /// 生成球体
        /// </summary>
        /// <param name="latSegments">纬度段</param>
        /// <param name="lonSegments">经度段</param>
        /// <param name="radius">半径</param>
        /// <returns>球体顶点数组</returns>
        public static SphereVertex[] GenerateSphere(int latSegments, int lonSegments, float radius)
        {
            SphereVertex[] vertices = new SphereVertex[(latSegments + 1) * lonSegments];
            int index = 0;
            for (int lat = 0; lat <= latSegments; lat++)
            {
                float phi = MathHelper.Pi * lat / latSegments;
                float y = MathF.Cos(phi) * radius;
                float ringRadius = MathF.Sin(phi) * radius;
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    float theta = MathHelper.TwoPi * lon / lonSegments;
                    vertices[index++] = new SphereVertex
                    {
                        Position = new Vector3(MathF.Cos(theta) * ringRadius, y, MathF.Sin(theta) * ringRadius),
                        UV = new Vector2((float)lon / lonSegments, (float)lat / latSegments),
                        Color = Color.White
                    };
                }
            }
            return vertices;
        }
        /// <summary>
        /// 生成球体索引
        /// </summary>
        /// <param name="latSegments">纬度段</param>
        /// <param name="lonSegments">经度段</param>
        /// <returns>索引数组</returns>
        public static int[] GenerateSphereIndices(int latSegments, int lonSegments)
        {
            int[] indices = new int[latSegments * lonSegments * 6];
            int idx = 0;
            for (int lat = 0; lat < latSegments; lat++)
            {
                for (int lon = 0; lon < lonSegments; lon++)
                {
                    int current = lat * lonSegments + lon;
                    int below = (lat + 1) * lonSegments + lon;
                    int next = lat * lonSegments + (lon + 1) % lonSegments;
                    int belowNext = (lat + 1) * lonSegments + (lon + 1) % lonSegments;
                    indices[idx++] = current;
                    indices[idx++] = below;
                    indices[idx++] = next;
                    indices[idx++] = next;
                    indices[idx++] = below;
                    indices[idx++] = belowNext;
                }
            }
            return indices;
        }
    }
    /// <summary>
    /// 球体顶点
    /// </summary>
    public struct SphereVertex : IVertexType
    {
        /// <summary>
        /// 三维坐标
        /// </summary>
        public Vector3 Position;
        /// <summary>
        /// 纹理坐标
        /// </summary>
        public Vector2 UV;
        /// <summary>
        /// 颜色
        /// </summary>
        public Color Color;
        /// <summary>
        /// 顶点声明
        /// </summary>
        public static readonly VertexDeclaration VertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(20, VertexElementFormat.Color, VertexElementUsage.Color, 0)
        );
        /// <summary>
        /// 返回静态顶点声明实例
        /// </summary>
        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
    }
}