using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using CabinetSheet = Lumina.Excel.Sheets.Cabinet;

namespace HaselTweaks.Windows;

[RegisterTransient, AutoConstruct]
public unsafe partial class CabinetQuickStoreWindow : SimpleWindow
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
    private readonly ItemService _itemService;
    private readonly ITextureProvider _textureProvider;
    private byte[]? _lastItems;
    private bool _locked;

    [AutoPostConstruct]
    private void Initialize()
    {
        Flags |= ImGuiWindowFlags.NoSavedSettings;
        Flags |= ImGuiWindowFlags.NoDecoration;
        Flags |= ImGuiWindowFlags.NoMove;
        Flags |= ImGuiWindowFlags.AlwaysAutoResize;

        DisableWindowSounds = true;
        RespectCloseHotkey = false;
    }

    public override bool DrawConditions()
    {
        return AgentCabinet.Instance()->ConfirmationAddonId == 0;
    }

    public override void PreDraw()
    {
        base.PreDraw();

        if (!TryGetAddon<AtkUnitBase>("Cabinet"u8, out var addon))
            return;

        var height = ImStyle.TextLineHeight + ImStyle.FramePadding.Y * 2 + ImStyle.WindowPadding.Y * 2;
        var offset = new Vector2(4, 3 - height);

        Position = ImGui.GetMainViewport().Pos + addon->Position + offset;

        ref var items = ref UIState.Instance()->Cabinet.UnlockedItems;
        if (_lastItems == null || !_lastItems.SequenceEqual(items))
        {
            _lastItems = [.. UIState.Instance()->Cabinet.UnlockedItems];
            _locked = false;
        }
    }

    public override void Draw()
    {
        if (!TryGetNextCabinetId(out var cabinetItemId))
        {
            ImGui.AlignTextToFramePadding();
            ImGui.Text(_textService.Translate("CabinetQuickStoreWindow.NoMoreItems"));
            return;
        }

        using var disable = ImRaii.Disabled(_locked);

        if (ImGui.Button(_textService.Translate("CabinetQuickStoreWindow.Button.Label")))
        {
            _locked = true;
            UIState.Instance()->Cabinet.StoreCabinetItem(cabinetItemId);
        }

        if (_excelService.TryGetRow<CabinetSheet>(cabinetItemId, out var row)) {
            ImGui.SameLine();
            _textureProvider.DrawIcon(_itemService.GetItemIcon(row.Item.RowId), ImGui.GetFrameHeight());
            ImGui.SameLine();
            ImGui.TextColored(_itemService.GetItemRarityColor(row.Item.RowId), _itemService.GetItemName(row.Item.RowId).ToString());
        }
    }

    private bool TryGetNextCabinetId(out uint cabinetId)
    {
        cabinetId = 0;

        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.CabinetStore);
        if (numberArray == null || numberArray->IntArray == null)
            return false;

        var numItems = numberArray->IntArray[0];
        if (numItems == 0)
            return false;

        var items = AgentCabinet.Instance()->Items;
        if (items == null)
            return false;

        ref var cabinet = ref UIState.Instance()->Cabinet;
        var sheet = _excelService.GetSheet<CabinetSheet>();

        for (var i = 0; i < numItems; i++)
        {
            var cabinetItemIndex = numberArray->IntArray[12 + i * 7];
            var cabinetItemId = items[cabinetItemIndex].Id;

            if (cabinetItemId == 0)
                break;

            if (sheet.TryGetRow(cabinetItemId, out var row) && row.Item.RowId != 0 && row.Item.IsValid && !cabinet.IsItemInCabinet(cabinetItemId))
            {
                cabinetId = cabinetItemId;
                return true;
            }
        }

        return false;
    }
}
