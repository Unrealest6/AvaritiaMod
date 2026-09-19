namespace AvaritiaMod.Common.UI
{
    /// <summary>
    /// 绑定某个物块实体的 UI。
    /// <para>UI 系统据此判断“同一个物块再次打开界面”并复用已有实例：
    /// 若每次都新建实例，新界面会从实体重新读一次内容物，而实体这一刻可能还是旧数据（界面里的改动尚未写回），
    /// 玩家手上已经拿到的物品会再出现一份。</para>
    /// </summary>
    /// <typeparam name="TEntity">绑定的物块实体类型</typeparam>
    public interface ITileEntityUI<out TEntity> where TEntity : TileEntity
    {
        TEntity TileEntity { get; }
    }
}
