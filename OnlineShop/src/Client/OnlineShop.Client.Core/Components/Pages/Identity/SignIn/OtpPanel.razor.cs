using OnlineShop.Shared.Dtos.Identity;

namespace OnlineShop.Client.Core.Components.Pages.Identity.SignIn;

public partial class OtpPanel
{
    [Parameter] public bool IsWaiting { get; set; }

    [Parameter] public SignInRequestDto Model { get; set; } = default!;

    [Parameter] public EventCallback<string?> OnSignIn { get; set; }

    [Parameter] public EventCallback OnResendOtp { get; set; }
}
