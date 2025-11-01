using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using TitleEdit.Data.Persistence;

namespace TitleEdit.PluginServices;

public class HousingService : AbstractService
{
    public unsafe void SetHousing(ref LocationModel model)
    {
        model.Furniture = null;
        model.Plots = null;
        model.Estate = null;

        var housingManager = HousingManager.Instance();

        if (housingManager == null) return;
        Span<HousingFurniture> furnitureList = null;

        if (housingManager->IndoorTerritory != null)
        {
            furnitureList = housingManager->IndoorTerritory->FurnitureManager.FurnitureMemory;

            var layoutManager = LayoutWorld.Instance()->ActiveLayout;
            if (layoutManager != null && layoutManager->IndoorAreaData != null)
            {
                model.Estate = new HousingEstateModel
                {
                    LightLevel = layoutManager->IndoorAreaData->LightLevel,
                    Floors =
                    [
                        layoutManager->IndoorAreaData->Floor0,
                        layoutManager->IndoorAreaData->Floor1,
                        layoutManager->IndoorAreaData->Floor2,
                    ]
                };
            }
        }
        else if (housingManager->OutdoorTerritory != null)
        {
            furnitureList = housingManager->OutdoorTerritory->FurnitureStruct.FurnitureMemory;

            var layoutManager = LayoutWorld.Instance()->ActiveLayout;
            if (layoutManager != null && layoutManager->OutdoorAreaData != null)
            {
                model.Plots = [];
                for (byte plotIndex = 0; plotIndex < layoutManager->OutdoorAreaData->Plots.Length && plotIndex < sbyte.MaxValue; plotIndex++)
                {
                    if (layoutManager->OutdoorAreaData->Plots[plotIndex].Fixture[0].FixtureId == 0) continue;
                    model.Plots.Add(new HousingPlotModel()
                    {
                        Plot = plotIndex,
                        Fixtures = layoutManager->OutdoorAreaData->Plots[plotIndex].Fixture.ToArray()
                    });
                }
            }
        }

        model.Furniture = [];
        foreach (var furnitureItem in furnitureList)
        {
            if (furnitureItem.Id == 0) continue;
            if (furnitureItem.Index == -1) continue;
            model.Furniture.Add(new HousingFurnitureModel()
            {
                Id = furnitureItem.Id,
                Stain = furnitureItem.Stain,
                Position = furnitureItem.Position,
                Rotation = furnitureItem.Rotation
            });
        }
    }
}
