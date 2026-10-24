using System;
using System.Linq;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using TitleEdit.Data.Persistence;
using IndoorFloorLayoutData = TitleEdit.Data.Persistence.IndoorFloorLayoutData;
using OutdoorPlotFixtureData = TitleEdit.Data.Persistence.OutdoorPlotFixtureData;

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
        HousingFurnitureManager* housingFurnitureManager = null;

        if (housingManager->IndoorTerritory != null)
        {
            housingFurnitureManager = &housingManager->IndoorTerritory->FurnitureManager;

            var layoutManager = LayoutWorld.Instance()->ActiveLayout;
            if (layoutManager != null && layoutManager->IndoorAreaData != null)
            {
                model.Estate = new HousingEstateModel
                {
                    LightLevel = layoutManager->IndoorAreaData->LightLevel,
                    Floors =
                    [
                        new IndoorFloorLayoutData(layoutManager->IndoorAreaData->Floor0),
                        new IndoorFloorLayoutData(layoutManager->IndoorAreaData->Floor1),
                        new IndoorFloorLayoutData(layoutManager->IndoorAreaData->Floor2)
                    ]
                };
            }
        }
        else if (housingManager->OutdoorTerritory != null)
        {
            housingFurnitureManager = &housingManager->OutdoorTerritory->FurnitureManager;

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
                            .Select(fixture => new OutdoorPlotFixtureData(fixture))
                            .ToArray()
                    });
                }
            }
        }

        if (housingFurnitureManager == null) return;
        model.Furniture = [];

        for (int index = 0; index < housingFurnitureManager->FurnitureMemory.Length; index++)
        {
            var furnitureItem = housingFurnitureManager->FurnitureMemory[index];
            if (furnitureItem.Id == 0) continue;
            if (furnitureItem.Index == -1) continue;

            HousingObjectDataModel? housingObjectDataModel = null;
            if (housingFurnitureManager->ObjectManager.DataMap.TryGetValuePointer((ushort)index, out var housingObjectData))
            {
                var valueSets = housingObjectData->ValueSets;
                HousingObjectDataModel dataModel = new()
                {
                    Value1 = new ushort[valueSets.Length],
                    Value2 = new byte[valueSets.Length],
                    Value3 = new byte[valueSets.Length],
                    Value4 = new byte[valueSets.Length],
                };

                for (int i = 0; i < valueSets.Length; i++)
                {
                    dataModel.Value1[i] = valueSets[i].Value1;
                    dataModel.Value2[i] = valueSets[i].Value2;
                    dataModel.Value3[i] = valueSets[i].Value3;
                    dataModel.Value4[i] = valueSets[i].Value4;
                }

                housingObjectDataModel = dataModel;
            }


            model.Furniture.Add(new HousingFurnitureModel()
            {
                Id = furnitureItem.Id,
                Stain = furnitureItem.Stain,
                Position = furnitureItem.Position,
                Rotation = furnitureItem.Rotation,
                HousingObjectData = housingObjectDataModel
            });
        }
    }
}