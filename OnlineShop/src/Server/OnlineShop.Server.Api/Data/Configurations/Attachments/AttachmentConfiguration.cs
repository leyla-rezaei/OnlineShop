using OnlineShop.Server.Api.Models.Attachments;

namespace OnlineShop.Server.Api.Data.Configurations.Attachments;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.HasKey(attachment => new { attachment.Id, attachment.Kind });
    }
}
