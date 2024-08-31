using System.Numerics;

namespace TitleEdit.Data.Persistence
{
    public static class UiColors
    {

        // Approximation of the texture color of the highlight, taken from https://github.com/lmcintyre/TitleEditPlugin/blob/aa54f82c05d14d9f1893fc58a9bba150d46026bc/TitleEdit/TitleEdit.cs#L612
        // We subtract this value when applying the highlight color and we add them here cause these numbers are what the game actually uses
        public static readonly Vector4 HighlightColorApproximation = new Vector4(0x00, 0x7F, 0xE0, 0x00) / 255f;

        public static readonly UiColorModel ARealmReborn = new()
        {
            Expansion = UiColorExpansion.ARealmReborn,
            Color = new Vector4(0xFF, 0xFF, 0xFF, 0xFF) / 255f,
            EdgeColor = new Vector4(0x00, 0x99, 0xFF, 0xFF) / 255f,
            HighlightColor = new Vector4(0, 0, 0, 0xCC) / 255f + HighlightColorApproximation,
        };

        public static readonly UiColorModel Heavensward = new()
        {
            Expansion = UiColorExpansion.Heavensward,
            Color = new Vector4(0xFF, 0xFF, 0xFF, 0xFF) / 255f,
            EdgeColor = new Vector4(0x00, 0x99, 0xFF, 0xFF) / 255f,
            HighlightColor = new Vector4(0, 0, 0, 0xCC) / 255f + HighlightColorApproximation,
        };

        public static readonly UiColorModel Stormblood = new()
        {
            Expansion = UiColorExpansion.Stormblood,
            Color = new Vector4(0xFF, 0xFF, 0xFF, 0xFF) / 255f,
            EdgeColor = new Vector4(0xFE, 0x8E, 0x37, 0xFF) / 255f,
            HighlightColor = new Vector4(255, 0, -190, 0xCC) / 255f + HighlightColorApproximation,
        };

        public static readonly UiColorModel Shadowbringers = new()
        {
            Expansion = UiColorExpansion.Shadowbringers,
            Color = new Vector4(0xFF, 0xFF, 0xFF, 0xFF) / 255f,
            EdgeColor = new Vector4(0x55, 0x34, 0xC2, 0xFF) / 255f,
            HighlightColor = new Vector4(0, -255, 0, 0xCC) / 255f + HighlightColorApproximation,
        };

        public static readonly UiColorModel Endwalker = new()
        {
            Expansion = UiColorExpansion.Endwalker,
            Color = new Vector4(0xFF, 0xFF, 0xFF, 0xFF) / 255f,
            EdgeColor = new Vector4(0x8D, 0x88, 0x00, 0xFF) / 255f,
            HighlightColor = new Vector4(255, 128, -128, 0xCC) / 255f + HighlightColorApproximation,
        };

        public static readonly UiColorModel Dawntrail = new()
        {
            Expansion = UiColorExpansion.Dawntrail,
            Color = new Vector4(0xFC, 0xDC, 0x97, 0xFF) / 255f,
            EdgeColor = new Vector4(0x4C, 0x34, 0x2F, 0xFF) / 255f,
            HighlightColor = new Vector4(255, -64, -255, 0xCC) / 255f + HighlightColorApproximation,
        };


        public static UiColorModel GetColorModelByExpansion(UiColorExpansion expansion) => expansion switch
        {
            UiColorExpansion.ARealmReborn => ARealmReborn,
            UiColorExpansion.Heavensward => Heavensward,
            UiColorExpansion.Stormblood => Stormblood,
            UiColorExpansion.Shadowbringers => Shadowbringers,
            UiColorExpansion.Endwalker => Endwalker,
            UiColorExpansion.Dawntrail => Dawntrail,
            _ => throw new System.NotImplementedException(),
        };
    }
}

