using OnlineShop.Server.Api.Models.Identity;

namespace OnlineShop.Server.Api.Services.Identity;

public partial class AppUserConfirmation : IUserConfirmation<User>
{
    public async Task<bool> IsConfirmedAsync(UserManager<User> manager, User user)
    {
        return user.EmailConfirmed ||
            user.PhoneNumberConfirmed ||
            (await manager.GetLoginsAsync(user)).Any();
    }
}
