namespace HaselTweaks.Tweaks;

public class GlamourDresserAlertConfiguration
{
    public bool IgnoreOutfits = false;
    public bool IgnoreDyedGlamour = false;
}

public partial class GlamourDresserAlert
{
    public override void DrawConfig()
    {
        _configGui.DrawConfigurationHeader();
        _configGui.DrawBool("IgnoreOutfits", ref _config.IgnoreOutfits);
        _configGui.DrawBool("IgnoreDyedGlamour", ref _config.IgnoreDyedGlamour);
    }

    public override void OnConfigChange(string fieldName)
    {
        _lastItemIds = [];
    }
}
