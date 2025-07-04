using ImageMagick;
using FluentStorage.Blobs;
using OnlineShop.Shared.Controllers;
using OnlineShop.Server.Api.Services;
using OnlineShop.Server.Api.Models.Identity;
using OnlineShop.Server.Api.Models.Attachments;

namespace OnlineShop.Server.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AttachmentController : AppControllerBase, IAttachmentController
{
    [AutoInject] private IBlobStorage blobStorage = default!;
    [AutoInject] private UserManager<User> userManager = default!;




    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    public async Task<IActionResult> UploadUserProfilePicture(IFormFile? file, CancellationToken cancellationToken)
    {
        return await UploadAttachment(
             User.GetUserId(),
             [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal],
             file,
             cancellationToken);
    }


    [AllowAnonymous]
    [HttpGet("{attachmentId}/{kind}")]
    [AppResponseCache(MaxAge = 3600 * 24 * 7, UserAgnostic = true)]
    public async Task<IActionResult> GetAttachment(Guid attachmentId, AttachmentKind kind, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath(attachmentId, kind);

        if (await blobStorage.ExistsAsync(filePath, cancellationToken) is false)
            throw new ResourceNotFoundException();

        var mimeType = kind switch
        {
            _ => "image/webp" // Currently, all attachment types are images.
        };

        return File(await blobStorage.OpenReadAsync(filePath, cancellationToken), mimeType, enableRangeProcessing: true);
    }

    [HttpDelete]
    public async Task DeleteUserProfilePicture(CancellationToken cancellationToken)
    {
        await DeleteAttachment(User.GetUserId(), [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal], cancellationToken);
    }



    private async Task DeleteAttachment(Guid attachmentId, AttachmentKind[] kinds, CancellationToken cancellationToken)
    {
        var attachments = await DbContext.Attachments.Where(p => p.Id == attachmentId && kinds.Contains(p.Kind)).ToArrayAsync(cancellationToken);

        foreach (var attachment in attachments)
        {
            var filePath = attachment.Path;

            if (await blobStorage.ExistsAsync(filePath, cancellationToken) is false)
                throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ImageCouldNotBeFound)]);

            await blobStorage.DeleteAsync(filePath, cancellationToken);


            if (attachment.Kind is AttachmentKind.UserProfileImageOriginal)
            {
                var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
                user!.HasProfilePicture = false;

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

            }

            DbContext.Attachments.Remove(attachment);
            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IActionResult> UploadAttachment(Guid attachmentId, AttachmentKind[] kinds, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new BadRequestException();

        await DbContext.Attachments.Where(att => att.Id == attachmentId).ExecuteDeleteAsync(cancellationToken);

        foreach (var kind in kinds)
        {
            var attachment = new Attachment
            {
                Id = attachmentId,
                Kind = kind,
                Path = GetFilePath(attachmentId, kind, file.FileName),
            };

            if (await blobStorage.ExistsAsync(attachment.Path, cancellationToken))
            {
                await blobStorage.DeleteAsync(attachment.Path, cancellationToken);
            }

            (bool NeedsResize, uint Width, uint Height) imageResizeContext = kind switch
            {
                AttachmentKind.UserProfileImageSmall => (true, 256, 256),
                _ => (false, 0, 0)
            };

            byte[]? imageBytes = null;

            if (imageResizeContext.NeedsResize)
            {
                using MagickImage sourceImage = new(file.OpenReadStream());

                if (sourceImage.Width < imageResizeContext.Width || sourceImage.Height < imageResizeContext.Height)
                    return BadRequest(Localizer[nameof(AppStrings.ImageTooSmall), imageResizeContext.Width, imageResizeContext.Height, sourceImage.Width, sourceImage.Height].ToString());

                sourceImage.Resize(new MagickGeometry(imageResizeContext.Width, imageResizeContext.Height));

                await blobStorage.WriteAsync(attachment.Path, imageBytes = sourceImage.ToByteArray(MagickFormat.WebP), cancellationToken: cancellationToken);
            }
            else
            {
                await blobStorage.WriteAsync(attachment.Path, file.OpenReadStream(), cancellationToken: cancellationToken);
            }

            await DbContext.Attachments.AddAsync(attachment, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);


            if (kind is AttachmentKind.UserProfileImageSmall)
            {
                var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
                user!.HasProfilePicture = true;

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

            }
        }

        return Ok();
    }

    private string GetFilePath(Guid attachmentId, AttachmentKind kind, string? fileName = null)
    {
        var filePath = kind switch
        {
            AttachmentKind.UserProfileImageSmall => $"{AppSettings.UserProfileImagesDir}{attachmentId}_{kind}.webp",
            AttachmentKind.UserProfileImageOriginal => $"{AppSettings.UserProfileImagesDir}{attachmentId}_{kind}{Path.GetExtension(fileName)}",
            _ => throw new NotImplementedException()
        };

        filePath = Environment.ExpandEnvironmentVariables(filePath);

        return filePath;
    }
}
