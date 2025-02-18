using LanguageExt;
using WalletFramework.Oid4Vc.Oid4Vp.Errors;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

public record AuthorizationRequestCancellation(
    Option<Uri> ResponseUri,
    List<VpError> Errors);
