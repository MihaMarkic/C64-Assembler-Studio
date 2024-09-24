using System.Text.RegularExpressions;

namespace C64AssemblerStudio.Engine.BindingValidators;

/// <summary>
/// Validates IP address in format IP|localhost:PORT
/// </summary>
public partial class IpAddressValidator: BindingValidator
{
    [GeneratedRegex(@"^(?<Address>(?<Localhost>localhost)|(?<First>2[0-4]\d|25[0-5]|[01]?\d\d?)\.(?<Second>2[0-4]\d|25[0-5]|[01]?\d\d?)\.(?<Third>2[0-4]\d|25[0-5]|[01]?\d\d?)\.(?<Fourth>2[0-4]\d|25[0-5]|[01]?\d\d?)):(?<Port>\d{1,5})$", RegexOptions.Singleline)]
    private static partial Regex ViceAddressRegex();
    public IpAddressValidator(string sourcePropertyName) : base(sourcePropertyName)
    {
    }
    
    public override void Update(string? text)
    {
        if (!string.IsNullOrWhiteSpace(text) && !ViceAddressRegex().IsMatch(text))
        {
            SetError("Text doesn't match format localhost|IP:PORT");
        }
        else
        {
            ClearError();
        }
        base.Update(text);
    }
}