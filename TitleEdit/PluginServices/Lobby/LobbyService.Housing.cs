using System;
using Dalamud;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.Interop;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;

namespace TitleEdit.PluginServices.Lobby;

public unsafe partial class LobbyService
{
    const uint FurnitureOwnerId = 0x54455046;
    
    [Signature(" E8 ?? ?? ?? ?? FF C5 48 85 C0 0F 84")]
    private readonly delegate*unmanaged<HousingFurniture*, HousingFurniture*, void*, int, HousingObject*> spawnFurnitureObject = null!;

    [Signature("40 55 41 57 48 83 EC 58")]
    private readonly delegate*unmanaged<HousingManager*, uint, void> initializeHousingLayout = null!;

    [Signature("0F B7 0D ?? ?? ?? ?? 66 0F 45 C8 48 89 7C 24", ScanType = ScanType.StaticAddress)]
    private readonly nint territoryTypeAddress;

    [Signature("E8 ?? ?? ?? ?? 41 8B 4E 20")]
    private readonly delegate*unmanaged<int, uint, ushort, int, byte, void> setInteriorFixture = null!;

    private void InitializeHousingLayout(LocationModel model)
    {
        
        var territory = model.Plots != null ? model.LayoutTerritoryTypeId : 649U;
        SafeMemory.Write(territoryTypeAddress, territory);

        try
        {
            var housingManager = HousingManager.Instance();
            if (housingManager == null) return;

            initializeHousingLayout(housingManager, territory);
        }
        catch (Exception ex)
        {
            Services.Log.Error(ex, "Error initializing housing layout");
        }
    }

    private void SetupHousing(LocationModel model)
    {
        InitializeHousingLayout(model);
        
        if (model.SaveLayout == false || model.SaveHousing == false) return;
        LoadEstate(model);
        LoadPlots(model);
        SpawnFurniture(model);
    }

    private void LoadEstate(LocationModel model)
    {
        if (model.Estate == null) return;

        var housingManager = HousingManager.Instance();
        if (housingManager->IndoorTerritory == null) return;

        var layoutManager = LayoutWorld.Instance()->ActiveLayout;
        if (layoutManager == null) return;

        for (var floorIndex = 0; floorIndex < 2; floorIndex++)
        {
            var floor = model.Estate.Floors[floorIndex];
            setInteriorFixture(floorIndex, 0, 0, floor.Part0, byte.MaxValue);
            setInteriorFixture(floorIndex, 1, 0, floor.Part1, byte.MaxValue);
            setInteriorFixture(floorIndex, 2, 0, floor.Part2, byte.MaxValue);
            setInteriorFixture(floorIndex, 3, 0, floor.Part3, byte.MaxValue);
            setInteriorFixture(floorIndex, 4, 0, floor.Part4, byte.MaxValue);
        }

        if (layoutManager->IndoorAreaData != null)
        {
            layoutManager->IndoorAreaData->LightLevel = model.Estate.LightLevel;
        }
    }

    private void SetPlot(HousingPlotModel plotModel)
    {
        if (plotModel.Plot >= 60) return;
        
        var housingManager = HousingManager.Instance();
        if (housingManager == null) return;
        if (housingManager->OutdoorTerritory == null) return;
        var layoutManager = LayoutWorld.Instance()->ActiveLayout;
        if (layoutManager == null) return;
        if (layoutManager->OutdoorAreaData == null) return;
        
        for (byte fixtureIndex = 0; fixtureIndex < plotModel.Fixtures.Length && fixtureIndex < 8; fixtureIndex++)
        {
            layoutManager->OutdoorAreaData->SetFixture(plotModel.Plot + 1U, fixtureIndex, plotModel.Fixtures[fixtureIndex].FixtureId);
            layoutManager->OutdoorAreaData->SetFixtureStain(plotModel.Plot + 1U, fixtureIndex, plotModel.Fixtures[fixtureIndex].StainId);
        }
    }
    
    private void LoadPlots(LocationModel model)
    {
        if (model.Plots is not { Count: > 0 }) return;
        foreach (var plotModel in model.Plots)
        {
            Services.Framework.RunOnTick(() => {
                SetPlot(plotModel);
            }, delayTicks: plotModel.Plot * 2);
        }
    }

    private void SpawnFurniture(LocationModel model)
    {
        if (model.Furniture is not { Count: > 0 }) return;
        var housingManager = HousingManager.Instance();
        if (housingManager == null)
        {
            Services.Log.Warning("HousingManager object is null");
            return;
        }

        if (housingManager->CurrentTerritory == null) return;

        HousingFurniture* furnitureArray;

        if (housingManager->IndoorTerritory != null)
        {
            furnitureArray = housingManager->IndoorTerritory->Furniture.GetPointer(0);
        }
        else if (housingManager->OutdoorTerritory != null)
        {
            furnitureArray = housingManager->OutdoorTerritory->Furniture.GetPointer(0);
        }
        else
        {
            return;
        }

        var furnitureData = stackalloc HousingFurniture[1];
        foreach (var furniture in model.Furniture)
        {
            furnitureData->Index = -1;
            furnitureData->Position = furniture.Position;
            furnitureData->Id = furniture.Id & 0x0000FFFF;
            furnitureData->Stain = furniture.Stain;
            furnitureData->Rotation = furniture.Rotation;
            var spawnedObject = spawnFurnitureObject(furnitureArray, furnitureData, null, 0);
            if (spawnedObject != null)
            {
                spawnedObject->OwnerId = FurnitureOwnerId;
            }
        }
    }
}
