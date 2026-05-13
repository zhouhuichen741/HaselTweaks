using FFXIVClientStructs.FFXIV.Client.Game.Event;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HaselTweaks.Tweaks;

[RegisterSingleton<IHostedService>(Duplicate = DuplicateStrategy.Append), AutoConstruct]
public unsafe partial class ShopItemIcons : ConfigurableTweak<ShopItemIconsConfiguration>
{
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly ItemService _itemService;

    public override void OnEnable()
    {
        _addonLifecycle.RegisterListener(AddonEvent.PreSetup, "Shop", OnShopPreSetup);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "Shop", OnShopPreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, ["ShopExchangeItem", "ShopExchangeCurrency"], OnShopExchangePreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "GrandCompanyExchange", OnGrandCompanyExchangePreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "InclusionShop", OnInclusionShopPreRefresh);
        _addonLifecycle.RegisterListener(AddonEvent.PreRefresh, "FreeShop", OnFreeShopPreRefresh);
    }

    public override void OnDisable()
    {
        _addonLifecycle.UnregisterListener(AddonEvent.PreSetup, "Shop", OnShopPreSetup);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "Shop", OnShopPreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, ["ShopExchangeItem", "ShopExchangeCurrency"], OnShopExchangePreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "GrandCompanyExchange", OnGrandCompanyExchangePreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "InclusionShop", OnInclusionShopPreRefresh);
        _addonLifecycle.UnregisterListener(AddonEvent.PreRefresh, "FreeShop", OnFreeShopPreRefresh);
    }

    private void OnShopPreSetup(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleShop || args is not AddonSetupArgs setupArgs)
            return;

        UpdateShopIcons(setupArgs.GetAtkValues());
    }

    private void OnShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleShop || args is not AddonRefreshArgs refreshArgs)
            return;

        UpdateShopIcons(refreshArgs.GetAtkValues());
    }

    private void UpdateShopIcons(Span<AtkValue> values)
    {
        const int valueCount = 625;

        if (values.Length != valueCount)
        {
            _logger.LogDebug("[UpdateShopIcons] Expected {count} AtkValues, found {actualCount}. Aborting.", valueCount, values.Length);
            return;
        }

        var handler = ShopEventHandler.AgentProxy.Instance()->Handler;
        if (handler == null)
        {
            _logger.LogDebug("[UpdateShopIcons] ShopEventHandler Handler is null. Aborting.");
            return;
        }

        if (!values[0].TryGetUInt(out var tabIndex))
        {
            _logger.LogDebug("[UpdateShopIcons] Could not read tab index. Aborting.");
            return;
        }

        const int IconIdOffset = 197;

        // Buy
        if (tabIndex == 0)
        {
            for (var i = 0; i < handler->VisibleItemsCount; i++)
            {
                ref var iconIdValue = ref values[IconIdOffset + i];
                if (!iconIdValue.IsUInt)
                    continue;

                var itemIndex = handler->VisibleItems[i];
                if (itemIndex < 0 || itemIndex > handler->ItemsCount)
                    continue;

                var itemId = handler->Items[itemIndex].ItemId;
                if (itemId == 0)
                    continue;

                iconIdValue.UInt = _itemService.GetItemIcon(itemId);
            }
        }
        // Buyback
        else if (tabIndex == 1)
        {
            for (var i = 0; i < handler->BuybackCount; i++)
            {
                ref var iconIdValue = ref values[IconIdOffset + i];
                if (!iconIdValue.IsUInt)
                    continue;

                var itemId = handler->Buyback[i].ItemId;
                if (itemId == 0)
                    continue;

                iconIdValue.UInt = _itemService.GetItemIcon(itemId);
            }
        }
    }

    private void OnShopExchangePreRefresh(AddonEvent type, AddonArgs args)
    {
        if (args is not AddonRefreshArgs refreshArgs)
            return;

        if ((args.AddonName == "ShopExchangeItem" && !_config.HandleShopExchangeItem) ||
            (args.AddonName == "ShopExchangeCurrency" && !_config.HandleShopExchangeCurrency))
        {
            return;
        }

        // 48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 83 EC ?? 0F B6 99
        const int valueCount = 3325;

        var values = refreshArgs.GetAtkValues();
        if (values.Length != valueCount)
        {
            _logger.LogDebug("[OnShopExchangePreRefresh] Expected {count} AtkValues, found {actualCount}. Aborting.", valueCount, values.Length);
            return;
        }

        if (!values[4].TryGetUInt(out var itemCount))
            return;

        for (var i = 0; i < itemCount; i++)
        {
            ref var itemIdValue = ref values[1066 + i];
            ref var iconIdValue = ref values[212 + i];

            if (!itemIdValue.IsUInt || !iconIdValue.IsInt || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = _itemService.GetItemIcon(itemIdValue.UInt);
        }
    }

    private void OnGrandCompanyExchangePreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleGrandCompanyExchange || args is not AddonRefreshArgs refreshArgs)
            return;

        // last function called in GCShopEventHandler_vf47
        const int valueCount = 556;

        var values = refreshArgs.GetAtkValues();
        if (values.Length != valueCount)
        {
            _logger.LogDebug("[OnGrandCompanyExchangePreRefresh] Expected {count} AtkValues, found {actualCount}. Aborting.", valueCount, values.Length);
            return;
        }

        for (var i = 0; i < 50; i++)
        {
            ref var itemIdValue = ref values[317 + i];
            ref var iconIdValue = ref values[167 + i];

            if (!itemIdValue.IsUInt || !iconIdValue.IsUInt || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = _itemService.GetItemIcon(itemIdValue.UInt);
        }
    }

    private void OnInclusionShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleInclusionShop || args is not AddonRefreshArgs refreshArgs)
            return;

        // second to last function called in AgentInclusionShop_Update
        const int valueCount = 2939;

        var values = refreshArgs.GetAtkValues();
        if (values.Length != valueCount)
        {
            _logger.LogDebug("[OnInclusionShopPreRefresh] Expected {count} AtkValues, found {actualCount}. Aborting.", valueCount, values.Length);
            return;
        }

        if (!values[298].TryGetUInt(out var itemCount))
            return;

        for (var i = 0; i < itemCount; i++)
        {
            ref var itemIdValue = ref values[300 + i * 18];
            ref var iconIdValue = ref values[300 + i * 18 + 1];

            if (!itemIdValue.IsUInt || !iconIdValue.IsUInt || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = _itemService.GetItemIcon(itemIdValue.UInt);
        }
    }

    private void OnFreeShopPreRefresh(AddonEvent type, AddonArgs args)
    {
        if (!_config.HandleFreeShop || args is not AddonRefreshArgs refreshArgs)
            return;

        // found in AgentFreeShop_Show
        const int valueCount = 565;

        var values = refreshArgs.GetAtkValues();
        if (values.Length != valueCount)
        {
            _logger.LogDebug("[OnFreeShopPreRefresh] Expected {count} AtkValues, found {actualCount}. Aborting.", valueCount, values.Length);
            return;
        }

        if (!values[76].TryGetUInt(out var itemCount))
        {
            _logger.LogDebug("[OnFreeShopPreRefresh] Could not read item count.");
            return;
        }

        for (var i = 0; i < itemCount; i++)
        {
            ref var itemIdValue = ref values[138 + i];
            ref var iconIdValue = ref values[199 + i];

            if (!itemIdValue.IsUInt || !iconIdValue.IsUInt || itemIdValue.UInt == 0)
                continue;

            iconIdValue.UInt = _itemService.GetItemIcon(itemIdValue.UInt);
        }
    }
}
