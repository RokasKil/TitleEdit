namespace TitleEdit.Data.Persistence;

// Copied straight from ClientStructs we don't want to actually persist the ClientStructs one in case it changes
public struct IndoorFloorLayoutData
{
    public int Part0;
    public int Part1;
    public int Part2;
    public int Part3;
    public int Part4;

    public IndoorFloorLayoutData(
        FFXIVClientStructs.FFXIV.Client.LayoutEngine.IndoorFloorLayoutData csIndoorFloorLayoutData)
    {
        Part0 = csIndoorFloorLayoutData.Part0;
        Part1 = csIndoorFloorLayoutData.Part1;
        Part2 = csIndoorFloorLayoutData.Part2;
        Part3 = csIndoorFloorLayoutData.Part3;
        Part4 = csIndoorFloorLayoutData.Part4;
    }
}
