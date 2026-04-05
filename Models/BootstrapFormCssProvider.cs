using Microsoft.AspNetCore.Components.Forms;

public class BootstrapFormCssProvider : FieldCssClassProvider
{
    public override string GetFieldCssClass(EditContext editContext,
        in FieldIdentifier fieldIdentifier)
    {
        var isValid = editContext.IsValid(fieldIdentifier);
        return isValid ? "valid" : "is-invalid";
    }
}