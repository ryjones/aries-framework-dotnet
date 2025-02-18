
using WalletFramework.Core.Functional;

namespace WalletFramework.Core.Encoding.Errors;

public record CouldNotParseToUtf8StringError(Exception E) 
    : Error("Could not parse the byte array to an UTF-8 string", E);
