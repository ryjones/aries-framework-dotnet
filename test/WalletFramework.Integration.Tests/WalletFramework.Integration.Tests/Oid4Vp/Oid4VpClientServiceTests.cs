using System.Net;
using FluentAssertions;
using Hyperledger.Aries.Agents;
using Hyperledger.Aries.Storage;
using Hyperledger.TestHarness.Mock;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SD_JWT.Roles.Implementation;
using WalletFramework.Core.Cryptography.Models;
using WalletFramework.Core.Functional;
using WalletFramework.MdocLib.Device.Abstractions;
using WalletFramework.Oid4Vc.Oid4Vci.Abstractions;
using WalletFramework.Oid4Vc.Oid4Vci.AuthFlow.Abstractions;
using WalletFramework.Oid4Vc.Oid4Vp.Models;
using WalletFramework.Oid4Vc.Oid4Vp.PresentationExchange.Services;
using WalletFramework.Oid4Vc.Oid4Vp.Services;
using WalletFramework.SdJwtVc.Models.Records;
using WalletFramework.SdJwtVc.Services.SdJwtVcHolderService;

namespace WalletFramework.Integration.Tests.Oid4Vp;

public class Oid4VpClientServiceTests : IAsyncLifetime
{
    private const string AuthRequestWithRequestUri =
        "haip://?client_id=https%3A%2F%2Fnc-sd-jwt.lambda.d3f.me%2Findex.php%2Fapps%2Fssi_login%2Foidc%2Fcallback&request_uri=https%3A%2F%2Fnc-sd-jwt.lambda.d3f.me%2Findex.php%2Fapps%2Fssi_login%2Foidc%2Frequestobject%2F4ba20ad0cb08545830aa549ab4305c03";

    private const string ExpectedRedirectUrl =
        "https://client.example.org/cb#response_code=091535f699ea575c7937fa5f0f454aee";

    private const string KeyBindingJwtMock =
        "eyJhbGciOiJFUzI1NiIsInR5cCI6ImtiK2p3dCJ9.eyJhdWQiOiJodHRwczovL3ZlcmlmaWVyLnNzaS50aXIuYnVkcnUuZGUvcHJlc2VudGF0aW9uL2F1dGhvcml6YXRpb24tcmVzcG9uc2UiLCJub25jZSI6IkxIc1lGRnlpMnNXQzZIM3ZSWVFsNFQiLCJpYXQiOjE2OTY0MjcwNzR9.Kxj1e7ucZeAnFnfOjo05QnW-DYeEprciDqkOhe6fhXIWprEYd1NJ6a0gpZJ66oTJsv49ExvDOKTLOzt6R75gcg";

    private const string RequestUriResponse =
        "eyJ4NWMiOlsiTUlJQ0x6Q0NBZFdnQXdJQkFnSUJCREFLQmdncWhrak9QUVFEQWpCak1Rc3dDUVlEVlFRR0V3SkVSVEVQTUEwR0ExVUVCd3dHUW1WeWJHbHVNUjB3R3dZRFZRUUtEQlJDZFc1a1pYTmtjblZqYTJWeVpXa2dSMjFpU0RFS01BZ0dBMVVFQ3d3QlNURVlNQllHQTFVRUF3d1BTVVIxYm1sdmJpQlVaWE4wSUVOQk1CNFhEVEl6TURnd016QTROREkwTkZvWERUSTRNRGd3TVRBNE5ESTBORm93VlRFTE1Ba0dBMVVFQmhNQ1JFVXhIVEFiQmdOVkJBb01GRUoxYm1SbGMyUnlkV05yWlhKbGFTQkhiV0pJTVFvd0NBWURWUVFMREFGSk1Sc3dHUVlEVlFRRERCSlBjR1Z1U1dRMFZsQWdWbVZ5YVdacFpYSXdXVEFUQmdjcWhrak9QUUlCQmdncWhrak9QUU1CQndOQ0FBUnNoUzVDaVBrSzVXRUN1RHpybmN0SXBwYm1nc1lkOURzT1lEcElFeFpFczFmUWNOeXZrQjVFZU5Xc2MwU0ExUU5xd3dHVzRndUZLZzBJZjFKR0R4VWZvNEdITUlHRU1CMEdBMVVkRGdRV0JCUmZMQVBzeG1Mc3AxblEvRk12RkkzN0MzQmxZREFNQmdOVkhSTUJBZjhFQWpBQU1BNEdBMVVkRHdFQi93UUVBd0lIZ0RBa0JnTlZIUkVFSFRBYmdobDJaWEpwWm1sbGNpNXpjMmt1ZEdseUxtSjFaSEoxTG1SbE1COEdBMVVkSXdRWU1CYUFGRStXNno3YWpUdW1leCtZY0Zib05yVmVDMnRSTUFvR0NDcUdTTTQ5QkFNQ0EwZ0FNRVVDSUNWZURUMnNkZHhySEMrZ0ZJTUVmc3huc0lXRmdIdnZlZnBuWXZrb0RjbHdBaUVBMlFnRVRHV3hIWUVObWxsNDA2VUNwYnFRb1kzMzJPbE9qdDUwWjc2WHBtQT0iLCJNSUlDTFRDQ0FkU2dBd0lCQWdJVU1ZVUhoR0Q5aFUvYzBFbzZtVzhyamplSit0MHdDZ1lJS29aSXpqMEVBd0l3WXpFTE1Ba0dBMVVFQmhNQ1JFVXhEekFOQmdOVkJBY01Ca0psY214cGJqRWRNQnNHQTFVRUNnd1VRblZ1WkdWelpISjFZMnRsY21WcElFZHRZa2d4Q2pBSUJnTlZCQXNNQVVreEdEQVdCZ05WQkFNTUQwbEVkVzVwYjI0Z1ZHVnpkQ0JEUVRBZUZ3MHlNekEzTVRNd09USTFNamhhRncwek16QTNNVEF3T1RJMU1qaGFNR014Q3pBSkJnTlZCQVlUQWtSRk1ROHdEUVlEVlFRSERBWkNaWEpzYVc0eEhUQWJCZ05WQkFvTUZFSjFibVJsYzJSeWRXTnJaWEpsYVNCSGJXSklNUW93Q0FZRFZRUUxEQUZKTVJnd0ZnWURWUVFEREE5SlJIVnVhVzl1SUZSbGMzUWdRMEV3V1RBVEJnY3Foa2pPUFFJQkJnZ3Foa2pPUFFNQkJ3TkNBQVNFSHo4WWpyRnlUTkhHTHZPMTRFQXhtOXloOGJLT2drVXpZV2NDMWN2ckpuNUpnSFlITXhaYk5NTzEzRWgwRXIyNzM4UVFPZ2VSb1pNSVRhb2RrZk5TbzJZd1pEQWRCZ05WSFE0RUZnUVVUNWJyUHRxTk82WjdINWh3VnVnMnRWNExhMUV3SHdZRFZSMGpCQmd3Rm9BVVQ1YnJQdHFOTzZaN0g1aHdWdWcydFY0TGExRXdFZ1lEVlIwVEFRSC9CQWd3QmdFQi93SUJBREFPQmdOVkhROEJBZjhFQkFNQ0FZWXdDZ1lJS29aSXpqMEVBd0lEUndBd1JBSWdZMERlcmRDeHQ0ekdQWW44eU5yRHhJV0NKSHB6cTRCZGpkc1ZOMm8xR1JVQ0lCMEtBN2JHMUZWQjFJaUs4ZDU3UUFMK1BHOVg1bGRLRzdFa29BbWhXVktlIl0sImtpZCI6Ik1Hd3daNlJsTUdNeEN6QUpCZ05WQkFZVEFrUkZNUTh3RFFZRFZRUUhEQVpDWlhKc2FXNHhIVEFiQmdOVkJBb01GRUoxYm1SbGMyUnlkV05yWlhKbGFTQkhiV0pJTVFvd0NBWURWUVFMREFGSk1SZ3dGZ1lEVlFRRERBOUpSSFZ1YVc5dUlGUmxjM1FnUTBFQ0FRUT0iLCJ0eXAiOiJKV1QiLCJhbGciOiJFUzI1NiJ9.eyJyZXNwb25zZV90eXBlIjoidnBfdG9rZW4iLCJwcmVzZW50YXRpb25fZGVmaW5pdGlvbiI6eyJpZCI6IjE1ZDQwNjU0LWM2NTgtNDkzOC1hYzA3LWVjYjQxYzlhZmIxMCIsImlucHV0X2Rlc2NyaXB0b3JzIjpbeyJpZCI6IjUwYjZlNGYzLTYyMmEtNDk3NC1iMzMwLTVlNzIwZWM5MjJiZiIsImZvcm1hdCI6eyJ2YytzZC1qd3QiOnsicHJvb2ZfdHlwZSI6WyJKc29uV2ViU2lnbmF0dXJlMjAyMCJdfX0sImNvbnN0cmFpbnRzIjp7ImxpbWl0X2Rpc2Nsb3N1cmUiOiJyZXF1aXJlZCIsImZpZWxkcyI6W3sicGF0aCI6WyIkLnR5cGUiXSwiZmlsdGVyIjp7InR5cGUiOiJzdHJpbmciLCJjb25zdCI6IlZlcmlmaWVkRU1haWwifX0seyJwYXRoIjpbIiQuZW1haWwiXX1dfX1dfSwicmVzcG9uc2VfdXJpIjoiaHR0cHM6Ly92ZXJpZmllci5zc2kudGlyLmJ1ZHJ1LmRlL3ByZXNlbnRhdGlvbi9hdXRob3JpemF0aW9uLXJlc3BvbnNlIiwibm9uY2UiOiJZZjg4dGRlZzhZTTkyM3E0aFFBRzlPIiwiY2xpZW50X2lkIjoiaHR0cHM6Ly92ZXJpZmllci5zc2kudGlyLmJ1ZHJ1LmRlL3ByZXNlbnRhdGlvbi9hdXRob3JpemF0aW9uLXJlc3BvbnNlIiwicmVzcG9uc2VfbW9kZSI6ImRpcmVjdF9wb3N0In0.sdeLcG6Ta4ozfbDuHBr2Vq-Ro2WpdUIhJWy3BgazyvrgkQw27uTFGioPWXNCruK5H5E5nvHS420u5tv0671tjg";

    public Oid4VpClientServiceTests()
    {
        var holder = new Holder();
        var walletRecordService = new DefaultWalletRecordService();
        var pexService = new PexService(_agentProviderMock.Object, _mdocStorageMock.Object, _sdJwtVcHolderService!);
       
        _sdJwtVcHolderService = new SdJwtVcHolderService(holder, _sdJwtSignerService.Object, walletRecordService);
        var authRequestService = new AuthorizationRequestService(_httpClientFactoryMock.Object);
        var oid4VpHaipClient = new Oid4VpHaipClient(pexService);
        _oid4VpRecordService = new Oid4VpRecordService(walletRecordService);
        
        _oid4VpClientService = new Oid4VpClientService(
            _agentProviderMock.Object,
            authRequestService,
            _httpClientFactoryMock.Object,
            _loggerMock.Object,
            _mdocAuthenticationService.Object,
            oid4VpHaipClient,
            _oid4VpRecordService,
            _mdocStorageMock.Object,
            pexService,
            _authFlowSessionStorageMock.Object,
            _sdJwtVcHolderService);
    
        _sdJwtSignerService.Setup(keyStore =>
                keyStore.GenerateKbProofOfPossessionAsync(
                    It.IsAny<KeyId>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    Option<IEnumerable<string>>.None))
            .ReturnsAsync(KeyBindingJwtMock);
    }

    private readonly Mock<IAgentProvider> _agentProviderMock = new();
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly Mock<ILogger<Oid4VpClientService>> _loggerMock = new();
    private readonly Mock<IMdocAuthenticationService> _mdocAuthenticationService = new();
    private readonly Mock<IMdocStorage> _mdocStorageMock = new();
    private readonly Mock<ISdJwtSigner> _sdJwtSignerService = new();
    private readonly Mock<IAuthFlowSessionStorage> _authFlowSessionStorageMock = new();
    private readonly MockAgentRouter _router = new();
    private readonly Oid4VpClientService _oid4VpClientService;
    private readonly Oid4VpRecordService _oid4VpRecordService;
    private readonly SdJwtVcHolderService _sdJwtVcHolderService;
    private readonly WalletConfiguration _config1 = new() { Id = Guid.NewGuid().ToString() };
    private readonly WalletCredentials _cred = new() { Key = "2" };
    
    private MockAgent? _agent1;

    [Fact]
    public async Task CanExecuteOpenId4VpFlow()
    {
        //Arrange
        SetupHttpClient(RequestUriResponse);
    
        var sdJwt = new SdJwtRecord();
            
        await _sdJwtVcHolderService.AddAsync(_agent1!.Context, sdJwt);

        var uri = AuthorizationRequestUri
            .FromUri(new Uri(AuthRequestWithRequestUri))
            .UnwrapOrThrow();
        
        //Act
        var authRequestCandidates = 
            (await _oid4VpClientService.ProcessAuthorizationRequestUri(uri)).UnwrapOrThrow();
        var (authorizationRequest, credentials) =
            (authRequestCandidates.AuthorizationRequest, authRequestCandidates.Candidates.UnwrapOrThrow());
        
        var selectedCandidates = new SelectedCredential
        {
            InputDescriptorId = credentials.First().InputDescriptorId,
            Credential = credentials.First().CredentialSetCandidates.First().Credentials.First()
        };
        
        SetupHttpClient(
            "{'redirect_uri':'https://client.example.org/cb#response_code=091535f699ea575c7937fa5f0f454aee'}"
        );
        
        var response = await _oid4VpClientService.AcceptAuthorizationRequest(
            authorizationRequest,
            [selectedCandidates]);
        
        // Assert
        credentials.Length().Should().Be(1);
            
        response.UnwrapOrThrow().Should().BeEquivalentTo(new Uri(ExpectedRedirectUrl));
            
        (await _oid4VpRecordService.ListAsync(_agent1.Context)).Count.Should().Be(1);
    }

    public async Task DisposeAsync()
    {
        await _agent1!.Dispose();
    }

    public async Task InitializeAsync()
    {
        _agent1 = 
            await MockUtils.CreateAsync(
                "agent1",
                _config1,
                _cred,
                new MockAgentHttpHandler(
                    cb => _router.RouteMessage(cb.name, cb.data)
                )
            );
            
        _router.RegisterAgent(_agent1);
    }

    private void SetupHttpClient(string response)
    {
        var httpResponseMessage = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(response)
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => httpResponseMessage);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }
}
