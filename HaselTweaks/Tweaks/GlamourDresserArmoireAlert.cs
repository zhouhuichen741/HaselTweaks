using Dalamud.Game.Inventory.InventoryEventArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game;
using HaselTweaks.Windows;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class GlamourDresserArmoireAlert : ConfigurableTweak<GlamourDresserArmoireAlertConfiguration>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameInventory _gameInventory;
    private readonly AddonObserver _addonObserver;
    private readonly ExcelService _excelService;
    private readonly ItemService _itemService;
    private readonly CabinetService _cabinetService;
    private readonly IFramework _framework;

    private GlamourDresserArmoireAlertWindow? _window;
    private bool _isPendingUpdate;
    private uint[]? _lastItemIds = null;

    public Dictionary<uint, HashSet<ItemHandle>> Categories { get; } = [];

    public override void OnEnable()
    {
        _addonObserver.AddonOpen += OnAddonOpen;
        _addonObserver.AddonClose += OnAddonClose;
        _gameInventory.InventoryChangedRaw += OnInventoryUpdate;
        _framework.Update += OnFrameworkUpdate;
    }

    public override void OnDisable()
    {
        _addonObserver.AddonOpen -= OnAddonOpen;
        _addonObserver.AddonClose -= OnAddonClose;
        _gameInventory.InventoryChangedRaw -= OnInventoryUpdate;
        _framework.Update -= OnFrameworkUpdate;

        _isPendingUpdate = false;
        _window?.Dispose();
        _window = null;
    }

    private void OnAddonOpen(string addonName)
    {
        _isPendingUpdate |= addonName == "MiragePrismPrismBox";
    }

    private void OnAddonClose(string addonName)
    {
        if (addonName == "MiragePrismPrismBox")
        {
            _lastItemIds = null;
            _isPendingUpdate = false;
            _window?.Close();
        }
    }

    private void OnInventoryUpdate(IReadOnlyCollection<InventoryEventArgs> events)
    {
        _isPendingUpdate |= _addonObserver.IsAddonVisible("MiragePrismPrismBox");
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        var mirageManager = MirageManager.Instance();
        if (!mirageManager->PrismBoxLoaded)
            return;

        var itemIds = mirageManager->PrismBoxItemIds;

        if (!_isPendingUpdate || (_lastItemIds != null && mirageManager->PrismBoxItemIds.SequenceEqual(_lastItemIds)))
            return;

        _isPendingUpdate = true;
        _lastItemIds = itemIds.ToArray();

        Categories.Clear();

        _logger.LogInformation("Updating...");

        for (var i = 0; i < itemIds.Length; i++)
        {
            ItemHandle item = itemIds[i];

            // skip empty slots
            if (item.IsEmpty)
                continue;

            // check if item exists and has UI category set
            if (!_itemService.TryGetItem(item, out var itemRow) || itemRow.ItemUICategory.RowId == 0 || !itemRow.ItemUICategory.IsValid)
                continue;

            var isSet = _excelService.TryGetRow<MirageStoreSetItem>(item, out var setRow);

            // skip outfits that can't be stored in the armoire
            if (_config.IgnoreOutfits && isSet)
                continue;

            if (isSet && itemRow.ItemUICategory.RowId == 112 && !setRow.Items.Any(item => _cabinetService.TryGetCabinetId(item.RowId, out _)))
                continue;

            // skip items that can't be stored in the armoire
            if (!isSet && !_cabinetService.TryGetCabinetId(item, out _))
                continue;

            if (!Categories.TryGetValue(itemRow.ItemUICategory.RowId, out var categoryItems))
                Categories.TryAdd(itemRow.ItemUICategory.RowId, categoryItems = []);

            categoryItems.Add(item);
        }

        _window?.IsUpdatePending = false;

        if (Categories.Count == 0)
            return;

        _window ??= _serviceProvider.CreateInstance<GlamourDresserArmoireAlertWindow>(this);
        _window.Open();
    }
}
