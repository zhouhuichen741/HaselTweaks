namespace HaselTweaks.Tweaks;

public class EnhancedMiragePrismBoxConfiguration
{
    public bool EnableAutoFillHandIn = false;
}

public partial class EnhancedMiragePrismBox
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("EnableAutoFillHandIn", ref _config.EnableAutoFillHandIn);
    }
}
