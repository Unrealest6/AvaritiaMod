namespace AvaritiaMod.Common.Systems
{
    /// <summary>
    /// 中子态素压缩机UI系统
    /// </summary>
    public sealed class NeutroniumCompressorUISystem : AvaritiaUISystem<NeutroniumCompressorUI, NeutroniumCompressorTileEntity>
    {
        protected override void ShowUITileEntity(NeutroniumCompressorTileEntity tileEntity)
        {
            tileEntity.NeutroniumCompressorUI = CurrentUI;
        }
    }
}