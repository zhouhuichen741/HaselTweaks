using Dalamud.Game.Chat;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public partial class FlashTaskbar : ConfigurableTweak<FlashTaskbarConfiguration>
{
    private readonly IChatGui _chatGui;
    private readonly ICondition _condition;

    public override void OnEnable()
    {
        _chatGui.LogMessage += OnLogMessage;
        _condition.ConditionChange += OnConditionChange;
    }

    public override void OnDisable()
    {
        _chatGui.LogMessage -= OnLogMessage;
        _condition.ConditionChange -= OnConditionChange;
    }

    private void OnLogMessage(ILogMessage message)
    {
        if (_config.FlashOnAlarm && message.LogMessageId == 3906)
        {
            _logger.LogDebug("Alarm! Flashing taskbar...");
            Flash();
        }
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (_config.FlashOnCombat && flag == ConditionFlag.InCombat && value)
        {
            _logger.LogDebug("Combat started! Flashing taskbar...");
            Flash();
        }
    }

    private unsafe void Flash()
    {
        var framework = Framework.Instance();
        if (framework == null || framework->GameWindow == null || !framework->WindowInactive)
            return;

        PInvoke.FlashWindowEx(new FLASHWINFO()
        {
            cbSize = (uint)sizeof(FLASHWINFO),
            uCount = uint.MaxValue,
            dwTimeout = 0,
            dwFlags = FLASHWINFO_FLAGS.FLASHW_ALL | FLASHWINFO_FLAGS.FLASHW_TIMERNOFG,
            hwnd = (HWND)framework->GameWindow->WindowHandle,
        });
    }
}
