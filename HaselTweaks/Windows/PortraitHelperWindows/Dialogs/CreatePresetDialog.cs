using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using HaselCommon.Services;
using HaselTweaks.Config;
using HaselTweaks.ImGuiComponents;
using HaselTweaks.Records.PortraitHelper;
using HaselTweaks.Tweaks;
using ImGuiNET;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace HaselTweaks.Windows.PortraitHelperWindows.Dialogs;

public class CreatePresetDialog : ConfirmationDialog
{
    private readonly ConfirmationButton _saveButton;
    private readonly PluginConfig _pluginConfig;
    private readonly TranslationManager _translationManager;
    private string? _name;
    private PortraitPreset? _preset;
    private Image<Bgra32>? _image;
    private HashSet<Guid>? _tags;

    private PortraitHelperConfiguration Config => _pluginConfig.Tweaks.PortraitHelper;

    public CreatePresetDialog(
        PluginConfig pluginConfig,
        TranslationManager translationManager)
        : base(translationManager.Translate("PortraitHelperWindows.CreatePresetDialog.Title"))
    {
        _pluginConfig = pluginConfig;
        _translationManager = translationManager;

        AddButton(_saveButton = new ConfirmationButton(translationManager.Translate("ConfirmationButtonWindow.Save"), OnSave));
    }

    public void Open(string name, PortraitPreset? preset, Image<Bgra32>? image)
    {
        _name = name;
        _preset = preset;
        _image = image;
        _tags = [];
        Show();
    }

    public void Close()
    {
        Hide();
        _name = null;
        _preset = null;
        _image?.Dispose();
        _image = null;
        _tags = null;
    }

    public override bool DrawCondition()
        => base.DrawCondition() && _name != null && _preset != null && _image != null && _tags != null;

    public override void InnerDraw()
    {
        ImGui.TextUnformatted(t("PortraitHelperWindows.CreatePresetDialog.Name.Label"));
        ImGui.Spacing();
        ImGui.InputText("##PresetName", ref _name, 100);

        var disabled = string.IsNullOrEmpty(_name.Trim());
        if (!disabled && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter)))
        {
            OnSave();
        }

        if (Config.PresetTags.Count != 0)
        {
            ImGui.Spacing();
            ImGui.TextUnformatted(t("PortraitHelperWindows.CreatePresetDialog.Tags.Label"));

            var tagNames = _tags!
                .Select(id => Config.PresetTags.FirstOrDefault((t) => t.Id == id)?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name));

            var preview = tagNames.Any() ? string.Join(", ", tagNames) : t("PortraitHelperWindows.CreatePresetDialog.Tags.None");

            ImGui.Spacing();
            using var tagsCombo = ImRaii.Combo("##PresetTag", preview, ImGuiComboFlags.HeightLarge);
            if (tagsCombo.Success)
            {
                foreach (var tag in Config.PresetTags)
                {
                    var isSelected = _tags!.Contains(tag.Id);

                    if (ImGui.Selectable($"{tag.Name}##PresetTag{tag.Id}", isSelected))
                    {
                        if (isSelected)
                        {
                            _tags.Remove(tag.Id);
                        }
                        else
                        {
                            _tags.Add(tag.Id);
                        }
                    }
                }
            }
        }

        _saveButton.Disabled = disabled;
    }

    private void OnSave()
    {
        if (_preset == null || _image == null || string.IsNullOrEmpty(_name?.Trim()))
        {
            Service.Get<IPluginLog>().Error("Could not save portrait: data missing"); // TODO: show error
            Close();
            return;
        }

        Hide();

        Task.Run((Action)(() =>
        {
            var guid = Guid.NewGuid();
            var thumbPath = PortraitHelper.GetPortraitThumbnailPath(guid);

            if (Config.EmbedPresetStringInThumbnails)
            {
                _image.Metadata.ExifProfile ??= new();
                _image.Metadata.ExifProfile.SetValue(ExifTag.UserComment, _preset.ToExportedString());
            }

            _image.SaveAsPng(thumbPath, new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.BestCompression,
                ColorType = PngColorType.Rgb // no need for alpha channel
            });

            Config.Presets.Insert(0, new(guid, _name.Trim(), _preset, _tags!));
            Service.Get<Config.PluginConfig>().Save();

            Close();
        }));
    }
}
