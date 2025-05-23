namespace TitleEdit.Data.BGM
{
    public struct BgmInfo
    {
        public string Title;
        public string Location;
        public string FilePath;
        public string AdditionalInfo;
        public uint RowId;
        public bool Available;
        public readonly string DisplayName => $"{RowId} - {Title}";
    }
}
