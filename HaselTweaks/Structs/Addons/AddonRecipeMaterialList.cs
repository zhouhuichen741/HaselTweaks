using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Structs;

// ctor "40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 0F 57 C0 48 8D 05 ?? ?? ?? ?? 48 89 03 48 8D 8B ?? ?? ?? ?? 0F 11 83"
[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = 0x2B8)]
public unsafe partial struct AddonRecipeMaterialList
{
    [FieldOffset(0)] public AtkUnitBase AtkUnitBase;

    [FieldOffset(0x238)] public AtkComponentTreeList* TreeList;
    [FieldOffset(0x240)] public AtkComponentButton* RefreshButton;

    [MemberFunction("E8 ?? ?? ?? ?? BB ?? ?? ?? ?? C7 45 ?? ?? ?? ?? ?? 8B D3 C7 45")]
    public partial void SetWindowLock(bool locked);

    [MemberFunction("48 89 5C 24 ?? 48 89 54 24 ?? 48 89 4C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC 50 49 8B 08")]
    public partial void SetupRow(nint a2, nint a3);
}
