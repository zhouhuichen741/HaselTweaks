using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.STD;

namespace HaselTweaks.Structs;

[GenerateInterop]
public unsafe partial struct HaselAgentRecipeNote
{
    [MemberFunction("E8 ?? ?? ?? ?? 48 8D 8F ?? ?? ?? ?? 48 8B D6 E8 ?? ?? ?? ?? 4C 8D B7")]
    public partial void AddRecipeSearchHistoryTerm(Utf8String* term);

    [MemberFunction("E8 ?? ?? ?? ?? 48 8B 8F ?? ?? ?? ?? 8D 55 ?? 41 B8")]
    public static partial void ClearHistory(StdDeque<Utf8String>* history);
}
