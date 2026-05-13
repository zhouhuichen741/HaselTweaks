using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HaselTweaks.Structs;

[GenerateInterop]
[StructLayout(LayoutKind.Explicit, Size = AgentMiragePrismPrismSetConvert.StructSize)]
public partial struct HaselAgentMiragePrismPrismSetConvert
{
    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 47 ?? 33 DB")]
    public partial void UpdateAddon(uint flags);
}
