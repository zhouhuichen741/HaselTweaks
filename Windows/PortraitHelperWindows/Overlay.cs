using System.Numerics;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Tweaks;
using ImGuiNET;

namespace HaselTweaks.Windows.PortraitHelperWindows;

public abstract unsafe class Overlay : Window
{
    public PortraitHelper Tweak { get; init; }

    public AgentBannerEditor* AgentBannerEditor => Tweak.AgentBannerEditor;
    public AddonBannerEditor* AddonBannerEditor => Tweak.AddonBannerEditor;

    protected static PortraitHelper.Configuration Config => Plugin.Config.Tweaks.PortraitHelper;

    public Overlay(string name, PortraitHelper tweak) : base(name)
    {
        Tweak = tweak;

        base.Flags |= ImGuiWindowFlags.NoSavedSettings;
        base.Flags |= ImGuiWindowFlags.NoDecoration;
        base.Flags |= ImGuiWindowFlags.NoMove;
        base.RespectCloseHotkey = false;
        base.IsOpen = true;
    }

    public override bool DrawConditions()
    {
        var windowNode = (AtkResNode*)((AtkUnitBase*)AddonBannerEditor)->WindowNode;
        var controlsHint = GetNode((AtkUnitBase*)AddonBannerEditor, 2);
        var verticalSeparatorNode = GetNode((AtkUnitBase*)AddonBannerEditor, 135);

        var isContextMenuOpen = *(byte*)((nint)AddonBannerEditor + 0x1A1) != 0;
        var isCloseDialogOpen = AgentBannerEditor->EditorState->CloseDialogAddonId != 0;

        return !isContextMenuOpen && !isCloseDialogOpen;
    }

    public override void OnOpen()
        => ToggleUiVisibility(false);

    public override void OnClose()
        => ToggleUiVisibility(true);

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.WindowBg, 0xFF313131);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 4));

        var windowNode = (AtkResNode*)((AtkUnitBase*)AddonBannerEditor)->WindowNode;
        var scale = GetNodeScale(windowNode);

        Position = new Vector2(
            AddonBannerEditor->AtkUnitBase.X + (windowNode->X + 8) * scale.X,
            AddonBannerEditor->AtkUnitBase.Y + (windowNode->Y + 40) * scale.Y
        );

        Size = new Vector2(
            (windowNode->GetWidth() - 16) * scale.X,
            (windowNode->GetHeight() - 56) * scale.Y
        );
    }

    public override void PostDraw()
    {
        // ImGui.PopStyleVar(); // call in Draw()!!!!
        ImGui.PopStyleColor();
    }

    public void ToggleUiVisibility(bool visible)
    {
        var controlsHint = GetNode((AtkUnitBase*)AddonBannerEditor, 2);
        var verticalSeparatorNode = GetNode((AtkUnitBase*)AddonBannerEditor, 135);

        SetVisibility(verticalSeparatorNode, visible);
        SetVisibility(controlsHint, visible);
    }
}
