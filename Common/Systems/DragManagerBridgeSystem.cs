namespace AvaritiaMod.Common.Systems;

/// <summary>
/// 把“面板开始拖拽”事件接到 <see cref="DragManager"/>：
/// 玩家按住 Shift/标题栏拖动界面时，若此刻正好按在物品槽上，
/// UI 的点击流程会先把槽位拖拽记为开始，这里在面板拖拽启动的同一帧内把它回滚掉。
/// <para>卸载时必须退订，否则静态事件会一直持有本模组的委托，导致模组无法卸载。</para>
/// </summary>
public sealed class DragManagerBridgeSystem : ModSystem
{
    public override void Load() => DragUISession.DragStarted += DragManager.CancelActiveDrag;
    public override void Unload() => DragUISession.DragStarted -= DragManager.CancelActiveDrag;
}