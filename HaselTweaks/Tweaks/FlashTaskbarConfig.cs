namespace HaselTweaks.Tweaks;

public class FlashTaskbarConfiguration
{
    public bool FlashOnAlarm = true;
    public bool FlashOnCombat = true;
}

public partial class FlashTaskbar
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("FlashOnAlarm", ref _config.FlashOnAlarm);
        _configGui.DrawBool("FlashOnCombat", ref _config.FlashOnCombat, noFixSpaceAfter: true);
    }
}
