using OnlineShop.Client.Core;

namespace OnlineShop.Client.Web;

public class ClientWebSettings : ClientCoreSettings
{

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var validationResults = base.Validate(validationContext).ToList();


        return validationResults;
    }
}

