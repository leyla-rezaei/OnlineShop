namespace OnlineShop.Shared.Controllers;

[Route("api/[controller]/[action]/"), AuthorizedApi]
public interface IAttachmentController : IAppController
{
    [HttpDelete]
    Task DeleteUserProfilePicture(CancellationToken cancellationToken);

}
