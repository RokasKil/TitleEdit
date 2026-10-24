using System;
using Dalamud;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.LayoutEngine;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;
using TitleEdit.Data.Housing;
using TitleEdit.Data.Persistence;
using TitleEdit.Utility;
using HousingFurniture = FFXIVClientStructs.FFXIV.Client.Game.HousingFurniture;

namespace TitleEdit.PluginServices.Lobby;

public unsafe partial class LobbyService
{
    // TitleEdit FurnitureOwnerId - can be used to recognise furniture spawned by us, not currently used
    const uint FurnitureOwnerId = 0x54455046;

    [Signature("E9 ?? ?? ?? ?? 48 8B 49 ?? 48 85 C9 74 ?? 48 83 C1")]
    private readonly delegate*unmanaged<HousingFurnitureManager*, SetHousingObjectDataStruct*, void> setHousingObjectData = null!;

    [Signature("48 83 EC ?? B8 ?? ?? ?? ?? 4C 8B D1 66 3B D0")]
    private readonly delegate*unmanaged<HousingFurnitureManager*, ushort, SetFurnitureStruct*, void> setFurniture = null!;

    [Signature("E8 ?? ?? ?? ?? FF C5 48 85 C0 0F 84")]
    private readonly delegate*unmanaged<HousingFurniture*, HousingFurniture*, HousingObject*> spawnFurnitureObject = null!;

    [Signature("40 55 41 57 48 83 EC ?? 48 89 5C 24 ?? 48 8B D9")]
    private readonly delegate*unmanaged<HousingManager*, uint, void> initializeHousingLayout = null!;

    [Signature("8B 3D ?? ?? ?? ?? 85 D2", ScanType = ScanType.StaticAddress)]
    private readonly nint territoryTypeAddress = 0;

    [Signature("E8 ?? ?? ?? ?? 41 8B 4E ?? 85 C9 74 ?? E8 ?? ?? ?? ?? 48 85 C0 74 ?? 8B 58")]
    private readonly delegate*unmanaged<int, uint, ushort, int, byte, void> setInteriorFixture = null!;

    // Initialize housing system, this call will also reset it and clean out all furniture
    // WARNING: It's important to not spawn any furniture on the same frame this call happened because it needs a frame to fully initialize
    // haven't actually looked what is being initialized, it might not be a frame but depend on pc specs
    // doesn't apply if housing was already initialized and you're only using this to clean it up
    private void InitializeHousingLayout(LocationModel? model = null)
    {
        Services.Log.Debug("[InitializeHousingLayout]");
        // Set the territory type the housing manager will be using
        // We always keep an interior value hardcoded unless the preset has a territory type which is an outdoor housing zone
        var territory = 649U; // Private Cotage - Shirogane
        if (model != null && Services.DataManager.GetExcelSheet<TerritoryType>().TryGetRow(model.Value.TerritoryTypeId, out var territoryTypeRow))
        {
            if (territoryTypeRow.TerritoryIntendedUse.RowId == 13) // Outdoor housing zone
            {
                territory = model.Value.TerritoryTypeId;
            }
        }

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

    // Initialize estate fixtures and lighting
    private void LoadEstate(LocationModel model)
    {
        if (model.SaveLayout == false || model.SaveHousing == false || model.Estate == null) return;
        Services.Log.Debug("[LoadEstate]");
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

    // Load individual plot
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

    // Load outdoor houses
    private void LoadPlots(LocationModel model)
    {
        if (model.SaveLayout == false || model.SaveHousing == false) return;
        if (model.Plots is not { Count: > 0 }) return;

        Services.Log.Debug("[LoadPlots]");
        foreach (var plotModel in model.Plots)
        {
            SetPlot(plotModel);
        }
    }

    // Load furniture
    private void LoadFurniture(LocationModel model)
    {
        if (model.SaveLayout == false || model.SaveHousing == false || model.Furniture is not { Count: > 0 }) return;
        Services.Log.Debug("[LoadFurniture]");
        var housingManager = HousingManager.Instance();
        if (housingManager == null)
        {
            Services.Log.Warning("HousingManager object is null");
            return;
        }

        if (housingManager->CurrentTerritory == null) return;

        HousingFurnitureManager* housingFurnitureManager;

        if (housingManager->IndoorTerritory != null)
        {
            housingFurnitureManager = &housingManager->IndoorTerritory->FurnitureManager;
        }
        else if (housingManager->OutdoorTerritory != null)
        {
            housingFurnitureManager = &housingManager->OutdoorTerritory->FurnitureManager;
        }
        else
        {
            return;
        }


        for (ushort i = 0; i < model.Furniture.Count; i++)
        {
            var furniture = model.Furniture[i];
            Services.Log.Debug($"Spawning {i}: {furniture.Id:X} {furniture.Position} {furniture.Rotation} {furniture.Stain} {furniture.HousingObjectData != null}");
            SetFurnitureStruct furnitureStruct = new()
            {
                Id = furniture.Id,
                Position = furniture.Position,
                Rotation = furniture.Rotation,
                Stain = furniture.Stain
            };
            setFurniture(housingFurnitureManager, i, &furnitureStruct);
            if (furniture.HousingObjectData != null)
            {
                var data = furniture.HousingObjectData.Value;
                var setHousingObjectDataStruct = new SetHousingObjectDataStruct();
                setHousingObjectDataStruct.Count = 8;
                setHousingObjectDataStruct.Index = i;
                data.Value1.CopyTo(new Span<ushort>(setHousingObjectDataStruct.Value1, 8));
                data.Value2.CopyTo(new Span<byte>(setHousingObjectDataStruct.Value2, 8));
                data.Value3.CopyTo(new Span<byte>(setHousingObjectDataStruct.Value3, 8));
                data.Value4.CopyTo(new Span<byte>(setHousingObjectDataStruct.Value4, 8));
                // There is also a Value5 in the actual settled struct but this method doesn't accept that
                // and computes it instead
                // Didn't notice any visual effect of not transfering this value 1:1 so I will simply not worry about it
                setHousingObjectData(housingFurnitureManager, &setHousingObjectDataStruct);
            }

            var spawnedObject = spawnFurnitureObject(housingFurnitureManager->FurnitureMemory.GetPointer(0), housingFurnitureManager->FurnitureMemory.GetPointer(i));
            if (spawnedObject != null)
            {
                spawnedObject->OwnerId = FurnitureOwnerId;
            }
        }
    }
}