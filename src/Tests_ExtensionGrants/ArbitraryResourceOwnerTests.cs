using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityModel.Client;
using IdentityModelExtras;
using IdentityServer4;
using IdentityServer4.HostApp;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace Tests_ExtensionGrants
{
    public class TestDefaultHttpClientFactory : IDefaultHttpClientFactory
    {
        public static TestServer TestServer { get; set; }
        public HttpMessageHandler HttpMessageHandler => TestServer.CreateHandler();
        public HttpClient HttpClient => TestServer.CreateClient();
    }
    [TestClass]
    public class ArbitraryResourceOwnerTests
    {
        private readonly TestServer _server;
        private static string ClientId => "arbitrary-resource-owner-client";
        private static string ClientSecret => "secret";

        public ArbitraryResourceOwnerTests()
        {
            // Arrange
            _server = new TestServer(new WebHostBuilder()
                .UseEnvironment("UnitTest") // You can set the environment you want (development, staging, production)
                .UseConfiguration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile("appsettings.UnitTest.json")
                    .Build()
                )
                .ConfigureServices(services =>
                {
                    services.TryAddTransient<IDefaultHttpClientFactory, TestDefaultHttpClientFactory>();
                })
                .UseStartup<Startup>());
            TestDefaultHttpClientFactory.TestServer = _server;
        }

        [TestMethod]
        public async Task Validate_Discovery_Endpoint()
        {
            using (HttpClient httpClient = _server.CreateClient())
            {
                var response = await httpClient.GetAsync($"https://p7identityserver4.azurewebsites.net/.well-known/openid-configuration");
                response = await httpClient.GetAsync($"{_server.BaseAddress}.well-known/openid-configuration");

                DiscoveryClient dc = new DiscoveryClient(_server.BaseAddress.AbsoluteUri, _server.CreateHandler());
                var discoveryResonse = await dc.GetAsync();
                discoveryResonse.ShouldNotBeNull();
            }
          

        }

        [TestMethod]
        public async Task Mint_arbitrary_resource_owner_missing_subject_and_token()
        {
            var client = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"
                },
               
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };
            var result = await client.RequestAsync(paramaters);
            result.Error.ShouldNotBeNullOrEmpty();
            result.Error.ShouldBe(OidcConstants.TokenErrors.InvalidRequest);
        }
        [TestMethod]
        public async Task Mint_arbitrary_resource_owner_remint_with_access_token()
        {
            var client = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"

                },
                {
                    ArbitraryResourceOwnerExtensionGrant.Constants.Subject,
                    "Ratt"
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };
            var result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNullOrEmpty();
            result.ExpiresIn.ShouldNotBeNull();

            var jwtSecurityToken = new JwtSecurityTokenHandler()
                .ReadToken(result.AccessToken) as JwtSecurityToken;
            jwtSecurityToken.ShouldNotBeNull();

            var authTimeQueryClaim = (from item in jwtSecurityToken.Claims
                                      where item.Type == JwtClaimTypes.AuthenticationTime
                select item).FirstOrDefault();
            authTimeQueryClaim.ShouldNotBeNull();



            // remint, but pass in the access_token from above
            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"

                },
                {
                    OidcConstants.TokenTypes.AccessToken,
                    result.AccessToken
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };
            result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNullOrEmpty();
            result.ExpiresIn.ShouldNotBeNull();

            jwtSecurityToken = new JwtSecurityTokenHandler()
                .ReadToken(result.AccessToken) as JwtSecurityToken;
            jwtSecurityToken.ShouldNotBeNull();

            var originAuthTimeClaim = (from item in jwtSecurityToken.Claims
                where item.Type == $"origin_{JwtClaimTypes.AuthenticationTime}"
                select item).FirstOrDefault();
            originAuthTimeClaim.ShouldNotBeNull();
            originAuthTimeClaim.Value.ShouldBe(authTimeQueryClaim.Value);
        }

        [TestMethod]
        public async Task Mint_arbitrary_resource_owner_with_offline_access()
        {
            var client = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"

                },
                {
                    ArbitraryResourceOwnerExtensionGrant.Constants.Subject,
                    "Ratt"
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };
            var result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNullOrEmpty();
            result.ExpiresIn.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task Mint_arbitrary_resource_owner_with_no_offline_access()
        {
            var client = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"

                },
                {
                    ArbitraryResourceOwnerExtensionGrant.Constants.Subject,
                    "Ratt"
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };

            var result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldBeNull();
            result.ExpiresIn.ShouldNotBeNull();
        }
        [TestMethod]
        public async Task Mint_arbitrary_resource_owner()
        {
            var client = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"
                },
                {
                    ArbitraryResourceOwnerExtensionGrant.Constants.Subject,
                    "Ratt"
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };

            var result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();
 
        }

        [TestMethod]
        public async Task Mint_arbitrary_resource_owner_and_refresh()
        {
            var client = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"
                },
                {
                    ArbitraryResourceOwnerExtensionGrant.Constants.Subject,
                    "Ratt"
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };

            var result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();

            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken},
                {OidcConstants.TokenRequest.RefreshToken, result.RefreshToken}
            };
            result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();
        }

        [TestMethod]
        public async Task Mint_arbitrary_resource_owner_and_refresh_and_revoke()
        {
            /*
                grant_type:arbitrary_resource_owner
                client_id:arbitrary-resource-owner-client
                client_secret:secret
                scope:offline_access nitro metal
                arbitrary_claims:{"some-guid":"1234abcd","In":"Flames"}
                subject:Ratt
                access_token_lifetime:3600000
             */

            var client = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"
                },
                {
                    ArbitraryResourceOwnerExtensionGrant.Constants.Subject,
                    "Ratt"
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };
            var result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();

            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken},
                {OidcConstants.TokenRequest.RefreshToken, result.RefreshToken}
            };
            result = await client.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();

            var revocationTokenClient = new TokenClient(
                _server.BaseAddress + "connect/revocation",
                ClientId,
                ClientSecret,
                _server.CreateHandler());
            paramaters = new Dictionary<string, string>()
            {
                {"token_type_hint", OidcConstants.TokenTypes.RefreshToken},
                {"token", result.RefreshToken}
            };
            await revocationTokenClient.RequestAsync(paramaters);

            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken},
                {OidcConstants.TokenRequest.RefreshToken, result.RefreshToken}
            };
            result = await client.RequestAsync(paramaters);
            result.Error.ShouldNotBeNullOrEmpty();
            result.Error.ShouldBe(OidcConstants.TokenErrors.InvalidGrant);
        }
        [TestMethod]
        public async Task Mint_multi_arbitrary_resource_owner_and_refresh_and_revoke()
        {
            var tokenClient = new TokenClient(
                _server.BaseAddress + "connect/token",
                ClientId,
                _server.CreateHandler());

            Dictionary<string, string> paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.ClientSecret, ClientSecret},
                {OidcConstants.TokenRequest.GrantType, ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner},
                {
                    OidcConstants.TokenRequest.Scope,
                    $"{IdentityServerConstants.StandardScopes.OfflineAccess} nitro metal"
                },
                {
                    ArbitraryNoSubjectExtensionGrant.Constants.ArbitraryClaims,
                    "{'role': ['application', 'limited'],'query': ['dashboard', 'licensing'],'seatId': ['8c59ec41-54f3-460b-a04e-520fc5b9973d'],'piid': ['2368d213-d06c-4c2a-a099-11c34adc3579']}"

                },
                {
                    ArbitraryResourceOwnerExtensionGrant.Constants.Subject,
                    "Ratt"
                },
                {ArbitraryNoSubjectExtensionGrant.Constants.AccessTokenLifetime, "3600"}
            };
            var result = await tokenClient.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();

            // mint a duplicate, this should be 2 refresh tokens.
            var result2 = await tokenClient.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();

            // first refresh
            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken},
                {OidcConstants.TokenRequest.RefreshToken, result.RefreshToken}
            };
            result = await tokenClient.RequestAsync(paramaters);
            result.AccessToken.ShouldNotBeNullOrEmpty();
            result.RefreshToken.ShouldNotBeNull();
            result.ExpiresIn.ShouldNotBeNull();

            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken},
                {OidcConstants.TokenRequest.RefreshToken, result2.RefreshToken}
            };
            result2 = await tokenClient.RequestAsync(paramaters);
            result2.AccessToken.ShouldNotBeNullOrEmpty();
            result2.RefreshToken.ShouldNotBeNull();
            result2.ExpiresIn.ShouldNotBeNull();

            var revocationTokenClient = new TokenClient(
                _server.BaseAddress + "connect/revocation",
                ClientId,
                ClientSecret,
                _server.CreateHandler());
            paramaters = new Dictionary<string, string>()
            {
                {"token_type_hint", OidcConstants.TokenTypes.RefreshToken},
                {"token", result.RefreshToken}
            };
            await revocationTokenClient.RequestAsync(paramaters);

            // try refreshing, these should now fail the refresh.
            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken},
                {OidcConstants.TokenRequest.RefreshToken, result.RefreshToken}
            };
            result = await tokenClient.RequestAsync(paramaters);
            result.Error.ShouldNotBeNullOrEmpty();
            result.Error.ShouldBe(OidcConstants.TokenErrors.InvalidGrant);

            paramaters = new Dictionary<string, string>()
            {
                {OidcConstants.TokenRequest.ClientId, ClientId},
                {OidcConstants.TokenRequest.GrantType, OidcConstants.GrantTypes.RefreshToken},
                {OidcConstants.TokenRequest.RefreshToken, result2.RefreshToken}
            };
            result2 = await tokenClient.RequestAsync(paramaters);
            result2.Error.ShouldNotBeNullOrEmpty();
            result2.Error.ShouldBe(OidcConstants.TokenErrors.InvalidGrant);
        }
    }
}
