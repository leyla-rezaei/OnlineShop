namespace OnlineShop.Client.Core.Components.Layout;

public partial class MainLayout
{
    private List<BitNavItem> navPanelItems = [];

    [AutoInject] protected IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] protected IAuthorizationService authorizationService = default!;

    private async Task SetNavPanelItems(ClaimsPrincipal authUser)
    {
        navPanelItems =
        [
            new()
            {
                Text = localizer[nameof(AppStrings.Home)],
                IconName = BitIconName.Home,
                Url = Urls.HomePage,
            }
        ];




        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.Terms)],
            IconName = BitIconName.EntityExtraction,
            Url = Urls.TermsPage,
        });

        navPanelItems.Add(new()
        {
            Text = localizer[nameof(AppStrings.About)],
            IconName = BitIconName.Info,
            Url = Urls.AboutPage,
        });

        var (manageRoles, manageUsers, manageAiPrompt) = await (authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageRoles),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageUsers),
            authorizationService.IsAuthorizedAsync(authUser!, AppFeatures.Management.ManageAiPrompt));

        if (manageRoles || manageUsers || manageAiPrompt)
        {
            BitNavItem managementItem = new()
            {
                Text = localizer[nameof(AppStrings.Management)],
                IconName = BitIconName.SettingsSecure,
                ChildItems = []
            };

            navPanelItems.Add(managementItem);

            if (manageRoles)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.UserGroups)],
                    IconName = BitIconName.WorkforceManagement,
                    Url = Urls.RolesPage,
                });
            }

            if (manageUsers)
            {
                managementItem.ChildItems.Add(new()
                {
                    Text = localizer[nameof(AppStrings.Users)],
                    IconName = BitIconName.SecurityGroup,
                    Url = Urls.UsersPage,
                });
            }

        }

        if (authUser.IsAuthenticated())
        {
            navPanelItems.Add(new()
            {
                Text = localizer[nameof(AppStrings.Settings)],
                IconName = BitIconName.Equalizer,
                Url = Urls.SettingsPage,
                AdditionalUrls =
                [
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Profile}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Account}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Tfa}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.Sessions}",
                    $"{Urls.SettingsPage}/{Urls.SettingsSections.UpgradeAccount}",
                ]
            });
        }
    }
}
