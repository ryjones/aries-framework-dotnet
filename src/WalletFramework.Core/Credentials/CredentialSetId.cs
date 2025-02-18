using WalletFramework.Core.Credentials.Errors;
using WalletFramework.Core.Functional;

namespace WalletFramework.Core.Credentials;

public readonly record struct CredentialSetId
{
    private string Value { get; }

    private CredentialSetId(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;

    public static implicit operator string(CredentialSetId credentialSetId) => credentialSetId.Value;

    public static CredentialSetId CreateCredentialSetId()
    {
        var id = Guid.NewGuid().ToString();
        return new CredentialSetId(id);
    }

    public static Validation<CredentialSetId> ValidCredentialSetId(string id)
    {
        var isValid = Guid.TryParse(id, out _);
        return isValid
            ? new CredentialSetId(id)
            : new CredentialIdError(id);
    }
};
