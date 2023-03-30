using Dalamud.Game.ClientState.Keys;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HaselTweaks.Structs;
using HaselTweaks.Utils;

namespace HaselTweaks.Tweaks;

public unsafe partial class CharacterClassSwitcher : Tweak
{
    public override string Name => "Character Class Switcher";
    public override string Description => "Clicking on a class/job in the character window finds the matching gearset and equips it. Hold shift on crafters to open the original desynthesis window.";
    public override bool HasIncompatibilityWarning => Service.PluginInterface.PluginInternalNames.Contains("SimpleTweaksPlugin");
    public override string IncompatibilityWarning => "In order for this tweak to work properly, please make sure \"Character Window Job Switcher\" is disabled in Simple Tweaks.";

    public static Configuration Config => Plugin.Config.Tweaks.CharacterClassSwitcher;

    public class Configuration
    {
        [ConfigField(Label = "Disable Tooltips", OnChange = nameof(OnTooltipConfigChange))]
        public bool DisableTooltips = false;
    }

    private bool TooltipPatchApplied;

    /* Address for AddonCharacterClass Tooltip Patch

        83 FD 14         cmp     ebp, 14h
        48 8B 6C 24 ??   mov     rbp, [rsp+68h+arg_8]
        7D 69            jge     short loc_140EB06A1     <- replacing this with a jmp rel8

       completely skips the whole if () {...} block, by jumping regardless of cmp result
     */
    [Signature("83 FD 14 48 8B 6C 24 ?? 7D 69")]
    private nint TooltipAddress { get; init; }

    /* Address for AddonPvPCharacter Tooltip Patch

        48 8D 4C 24 ??   lea     rcx, [rsp+68h+var_28]   <- replacing this with a jmp rel8
        E8 ?? ?? ?? ??   call    Component::GUI::AtkTooltipArgs_ctor
        ...

        completely skips the tooltip code, by jumping to the end of the function
     */
    [Signature("48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8B 83 ?? ?? ?? ?? 48 8B CF 0F B7 9F")]
    private nint PvPTooltipAddress { get; init; }

    private RaptureGearsetModule* gearsetModule;

    public override void Setup()
    {
        gearsetModule = RaptureGearsetModule.Instance();
    }

    public override void Enable()
    {
        ApplyTooltipPatch(Config.DisableTooltips);
    }

    public override void Disable()
    {
        ApplyTooltipPatch(false);
    }

    private void OnTooltipConfigChange()
    {
        ApplyTooltipPatch(Config.DisableTooltips);
    }

    private void ApplyTooltipPatch(bool enable)
    {
        if (enable && !TooltipPatchApplied)
        {
            MemoryUtils.ReplaceRaw(TooltipAddress + 8, new byte[] { 0xEB }); // jmp rel8
            MemoryUtils.ReplaceRaw(PvPTooltipAddress, new byte[] { 0xEB, 0x63 }); // jmp rel8

            TooltipPatchApplied = true;
        }
        else if (!enable && TooltipPatchApplied)
        {
            MemoryUtils.ReplaceRaw(TooltipAddress + 8, new byte[] { 0x7D }); // jge rel8
            MemoryUtils.ReplaceRaw(PvPTooltipAddress, new byte[] { 0x48, 0x8D }); // original bytes (see signature)

            TooltipPatchApplied = false;
        }
    }

    private static bool IsCrafter(int id)
    {
        return id >= 20 && id <= 27;
    }

    // AddonCharacterClass_OnSetup (vf47)
    [SigHook("48 8B C4 48 89 58 10 48 89 70 18 48 89 78 20 55 41 54 41 55 41 56 41 57 48 8D 68 A1 48 81 EC ?? ?? ?? ?? 0F 29 70 C8 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 17 F3 0F 10 35 ?? ?? ?? ?? 45 33 C9 45 33 C0 F3 0F 11 74 24 ?? 0F 57 C9 48 8B F9 E8")]
    private nint OnSetup(AddonCharacterClass* addon, int a2)
    {
        var result = OnSetupHook!.Original(addon, a2);
        var eventListener = &addon->AtkUnitBase.AtkEventListener;

        for (var i = 0; i < AddonCharacterClass.NUM_CLASSES; i++)
        {
            // skip crafters as they already have ButtonClick events
            if (IsCrafter(i)) continue;

            var node = addon->ButtonNodesSpan[i].Value;
            if (node == null) continue;

            var collisionNode = (AtkCollisionNode*)node->AtkComponentBase.UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AtkResNode.AddEvent(AtkEventType.MouseClick, (uint)i + 2, eventListener, null, false);
            collisionNode->AtkResNode.AddEvent(AtkEventType.InputReceived, (uint)i + 2, eventListener, null, false);
        }

        return result;
    }

    // AddonCharacterClass_OnUpdate (vf50)
    [SigHook("4C 8B DC 53 55 56 57 41 55 41 56")]
    private void OnUpdate(AddonCharacterClass* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        OnUpdateHook.Original(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < AddonCharacterClass.NUM_CLASSES; i++)
        {
            var node = addon->ButtonNodesSpan[i].Value;
            if (node == null) continue;

            // skip crafters as they already have Cursor Pointer flags
            if (IsCrafter(i))
            {
                // but ensure the button is enabled, even though the player might not have desynthesis unlocked
                node->AtkComponentBase.SetEnabledState(true);
                continue;
            }

            var collisionNode = (AtkCollisionNode*)node->AtkComponentBase.UldManager.RootNode;
            if (collisionNode == null) continue;

            var imageNode = GetNode<AtkImageNode>((AtkComponentBase*)node, 4);
            if (imageNode == null) continue;

            // if job is unlocked, it has full alpha
            var isUnlocked = imageNode->AtkResNode.Color.A == 255;

            if (isUnlocked)
                collisionNode->AtkResNode.Flags_2 |= 1 << 20; // add Cursor Pointer flag
            else
                collisionNode->AtkResNode.Flags_2 &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    // AddonCharacterClass_ReceiveEvent (vf2)
    [SigHook("48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 4D 8B D1")]
    private nint AddonCharacterClass_ReceiveEvent(AddonCharacterClass* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        // skip events for tabs
        if (eventParam < 2)
            goto OriginalReceiveEventCode;

        var node = addon->ButtonNodesSpan[eventParam - 2].Value;
        if (node == null || node->AtkComponentBase.OwnerNode == null)
            goto OriginalReceiveEventCode;

        var imageNode = GetNode<AtkImageNode>((AtkComponentBase*)node, 4);
        if (imageNode == null)
            goto OriginalReceiveEventCode;

        // if job is unlocked, it has full alpha
        var isUnlocked = imageNode->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            goto OriginalReceiveEventCode;

        // special handling for crafters
        if (IsCrafter(eventParam - 2))
        {
            var isClick =
                eventType == AtkEventType.MouseClick || eventType == AtkEventType.ButtonClick ||
                (eventType == AtkEventType.InputReceived && GamepadUtils.IsPressed(GamepadUtils.GamepadBinding.Accept));

            if (isClick && !Service.KeyState[VirtualKey.SHIFT])
            {
                SwitchClassJob(8 + (uint)eventParam - 22);
                return 0;
            }
        }
        else if (ProcessEvents(node->AtkComponentBase.OwnerNode, imageNode, eventType))
        {
            return 0;
        }

        OriginalReceiveEventCode:
        return AddonCharacterClass_ReceiveEventHook.Original(addon, eventType, eventParam, atkEvent, a5);
    }

    // AddonPvPCharacter_OnSetup (first fn in vf47)
    [SigHook("E8 ?? ?? ?? ?? 48 8B 83 ?? ?? ?? ?? 45 33 FF 41 8B CF 48 85 C0 74 07 48 8B 88 ?? ?? ?? ?? 45 33 C0")]
    private nint OnPvPSetup(AddonPvPCharacter* addon)
    {
        var result = OnPvPSetupHook.Original(addon);
        var eventListener = &addon->AtkUnitBase.AtkEventListener;

        for (var i = 0; i < AddonPvPCharacter.NUM_CLASSES; i++)
        {
            var entry = addon->ClassEntriesSpan[i];
            if (entry.Base == null) continue;

            var collisionNode = (AtkCollisionNode*)entry.Base->UldManager.RootNode;
            if (collisionNode == null) continue;

            collisionNode->AtkResNode.AddEvent(AtkEventType.MouseClick, (uint)i | 0x10000, eventListener, null, false);
            collisionNode->AtkResNode.AddEvent(AtkEventType.InputReceived, (uint)i | 0x10000, eventListener, null, false);
        }

        return result;
    }

    // AddonPvPCharacter_OnUpdate (second fn in vf50)
    [SigHook("48 8B C4 48 89 58 20 55 56 57 41 56 41 57 48 81 EC")]
    private void OnPvPUpdate(AddonPvPCharacter* addon, NumberArrayData** numberArrayData, StringArrayData** stringArrayData)
    {
        OnPvPUpdateHook.Original(addon, numberArrayData, stringArrayData);

        for (var i = 0; i < AddonPvPCharacter.NUM_CLASSES; i++)
        {
            var entry = addon->ClassEntriesSpan[i];
            if (entry.Base == null || entry.Icon == null) continue;

            var collisionNode = (AtkCollisionNode*)entry.Base->UldManager.RootNode;
            if (collisionNode == null) continue;

            // if job is unlocked, it has full alpha
            var isUnlocked = entry.Icon->AtkResNode.Color.A == 255;

            if (isUnlocked)
                collisionNode->AtkResNode.Flags_2 |= 1 << 20; // add Cursor Pointer flag
            else
                collisionNode->AtkResNode.Flags_2 &= ~(uint)(1 << 20); // remove Cursor Pointer flag
        }
    }

    // AddonPvPCharacter_ReceiveEvent (vf2)
    [SigHook("48 89 5C 24 ?? 57 48 83 EC 30 0F B7 C2 4D 8B D1 83 C0 FD")]
    private nint AddonPvPCharacter_ReceiveEvent(AddonPvPCharacter* addon, AtkEventType eventType, int eventParam, AtkEvent* atkEvent, nint a5)
    {
        if ((eventParam & 0xFFFF0000) != 0x10000)
            goto OriginalPvPReceiveEventCode;

        var entryId = eventParam & 0x0000FFFF;
        if (entryId is < 0 or > AddonPvPCharacter.NUM_CLASSES)
            goto OriginalPvPReceiveEventCode;

        var entry = addon->ClassEntriesSpan[entryId];

        if (entry.Base == null || entry.Base->OwnerNode == null || entry.Icon == null)
            goto OriginalPvPReceiveEventCode;

        // if job is unlocked, it has full alpha
        var isUnlocked = entry.Icon->AtkResNode.Color.A == 255;
        if (!isUnlocked)
            goto OriginalPvPReceiveEventCode;

        if (ProcessEvents(entry.Base->OwnerNode, entry.Icon, eventType))
            return 0;

        OriginalPvPReceiveEventCode:
        return AddonPvPCharacter_ReceiveEventHook.Original(addon, eventType, eventParam, atkEvent, a5);
    }

    /// <returns>Boolean whether original code should be skipped (true) or not (false)</returns>
    private bool ProcessEvents(AtkComponentNode* componentNode, AtkImageNode* imageNode, AtkEventType eventType)
    {
        var isClick =
            eventType == AtkEventType.MouseClick ||
            (eventType == AtkEventType.InputReceived && GamepadUtils.IsPressed(GamepadUtils.GamepadBinding.Accept));

        if (isClick)
        {
            var textureInfo = imageNode->PartsList->Parts[imageNode->PartId].UldAsset;
            if (textureInfo == null || textureInfo->AtkTexture.Resource == null)
                return false;

            var iconId = textureInfo->AtkTexture.Resource->IconID;
            if (iconId <= 62100)
                return false;

            // yes, you see correctly. the iconId is 62100 + ClassJob RowId :)
            var classJobId = iconId - 62100;

            SwitchClassJob((uint)classJobId);

            return true; // handled
        }

        if (eventType == AtkEventType.MouseOver)
        {
            componentNode->AtkResNode.AddBlue = 16;
            componentNode->AtkResNode.AddGreen = 16;
            componentNode->AtkResNode.AddRed = 16;
        }
        else if (eventType == AtkEventType.MouseOut)
        {
            componentNode->AtkResNode.AddBlue = 0;
            componentNode->AtkResNode.AddGreen = 0;
            componentNode->AtkResNode.AddRed = 0;
        }

        return false;
    }

    private void SwitchClassJob(uint classJobId)
    {
        if (gearsetModule == null)
            return;

        // loop through all gearsets and find the one matching classJobId with the highest avg itemlevel
        var selectedGearset = (Index: -1, ItemLevel: -1);
        for (var id = 0; id < 100; id++)
        {
            // skip if invalid
            if (gearsetModule->IsValidGearset(id) == 0)
                continue;

            var gearset = gearsetModule->GetGearset(id);

            // skip wrong job
            if (gearset->ClassJob != classJobId)
                continue;

            // skip if lower itemlevel than previous selected gearset
            if (selectedGearset.ItemLevel >= gearset->ItemLevel)
                continue;

            selectedGearset = (id + 1, gearset->ItemLevel);
        }

        UIModule.PlaySound(8, 0, 0, 0);

        if (selectedGearset.Index == -1)
        {
            // TODO: localize
            Service.Chat.PrintError("Couldn't find a suitable gearset.");
            return;
        }

        var command = $"/gearset change {selectedGearset.Index}";
        Log($"Executing {command}");
        Chat.SendMessage(command);
    }
}
