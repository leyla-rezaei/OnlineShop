﻿using Maui.AppStores;

namespace OnlineShop.Client.Maui.Services;

public partial class MauiAppUpdateService : IAppUpdateService
{
    public async Task ForceUpdate()
    {
        await AppStoreInfo.Current.OpenApplicationInStoreAsync();
    }
}
