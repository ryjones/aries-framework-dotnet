using LanguageExt;

namespace WalletFramework.Oid4Vc.Oid4Vp.Models;

public record AuthorizationRequestCandidates(
    AuthorizationRequest AuthorizationRequest,
    Option<List<PresentationCandidates>> Candidates);
