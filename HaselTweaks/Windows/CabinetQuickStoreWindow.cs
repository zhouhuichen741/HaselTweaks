using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Windows;

[RegisterTransient, AutoConstruct]
public unsafe partial class CabinetQuickStoreWindow : SimpleWindow
{
    private readonly ExcelService _excelService;
    private readonly TextService _textService;
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
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.CabinetStore);
        return numberArray != null && numberArray->IntArray != null && numberArray->IntArray[0] != 0 && AgentCabinet.Instance()->ConfirmationAddonId == 0;
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
        var numberArray = AtkStage.Instance()->GetNumberArrayData(NumberArrayType.CabinetStore);
        if (numberArray == null || numberArray->IntArray == null)
            return;

        var items = AgentCabinet.Instance()->Items;
        if (items == null)
            return;

        using var disable = ImRaii.Disabled(_locked);

        if (ImGui.Button(_textService.Translate("CabinetQuickStoreWindow.Button.Label")))
        {
            var index = numberArray->IntArray[12];
            var id = items[index].Id;
            _locked = true;
            UIState.Instance()->Cabinet.StoreCabinetItem(id);
        }
    }
}
