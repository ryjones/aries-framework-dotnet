using System.Security.Cryptography;
using WalletFramework.Core.Base64Url;

namespace WalletFramework.Core.Encoding;

public readonly struct Sha256Hash
{
    private byte[] Value { get; }

    private Sha256Hash(byte[] value)
    {
        Value = value;
    }

    public byte[] AsBytes => Value;

    public override string ToString() => Value.ToString();

    public string AsString => Base64UrlString.CreateBase64UrlString(Value);
    
    public static implicit operator byte[](Sha256Hash sha256Hash) => sha256Hash.Value;

    public static Sha256Hash ComputeHash(byte[] value)
    {
        var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(value);
        return new Sha256Hash(hash);
    }
}    

