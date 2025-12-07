namespace TitleEdit.Data.Persistence;

// Copied straight from ClientStructs we don't want to actually persist the ClientStructs one in case it changes 
public struct OutdoorPlotFixtureData
{
    public ushort FixtureId;
    public byte StainId;

    public OutdoorPlotFixtureData(
        FFXIVClientStructs.FFXIV.Client.LayoutEngine.OutdoorPlotFixtureData csOutdoorPlotFixtureData)
    {
        FixtureId = csOutdoorPlotFixtureData.FixtureId;
        StainId = csOutdoorPlotFixtureData.StainId;
    }
}
