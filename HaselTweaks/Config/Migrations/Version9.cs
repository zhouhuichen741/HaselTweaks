using System.Text.Json.Nodes;

namespace HaselTweaks.Config.Migrations;

// version 9: renamed GlamourDresserArmoireAlert to GlamourDresserAlert
public class Version9 : IConfigMigration
{
    public int Version => 9;

    public void Migrate(ref JsonObject config)
    {
        var enabledTweaks = (JsonArray?)config["EnabledTweaks"];
        if (enabledTweaks == null)
            return;

        if (enabledTweaks.FirstOrDefault(node => node?.ToString() == "GlamourDresserArmoireAlert") is { } nodeToRemove)
        {
            enabledTweaks.Remove(nodeToRemove);
            enabledTweaks.Add("GlamourDresserAlert");
        }

        if (!config.TryGetPropertyValue("Tweaks", out var tweaksConfigNode) || tweaksConfigNode == null)
            return;

        var tweaksConfig = tweaksConfigNode.AsObject();
        if (!tweaksConfig.TryGetPropertyValue("GlamourDresserArmoireAlert", out var existingConfigNode))
            return;

        tweaksConfig.Remove("GlamourDresserArmoireAlert");
        tweaksConfig.Add("GlamourDresserAlert", existingConfigNode);
    }
}
