﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using IdentityServer4Extras;
using IdentityServer4Extras.Extensions;
using IdentityServer4Extras.Services;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ArbitraryIdentityExtensionGrant
{
    public class TokenResponseGeneratorHook : ITokenResponseGeneratorHook
    {
        private IClientStore Clients { get; set; }
        private IResourceStore Resources { get; }
        private ITokenService TokenService { get; set; }
        private ILogger Logger { get; set; }

        public TokenResponseGeneratorHook(IClientStore clients, IResourceStore resources, ITokenService tokenService,
            ILogger<TokenResponseGeneratorHook> logger)
        {
            Clients = clients;
            Resources = resources;
            TokenService = tokenService;
            Logger = logger;
        }

        public async Task<TokenResponse> ProcessAsync(ITokenResponseGeneratorHookHost host,
            TokenRequestValidationResult request)
        {
            var grantType = request.ValidatedRequest.GrantType;
            if (grantType != Constants.ArbitraryIdentity)
            {
                return null;
            }

            Logger.LogTrace($"Creating response for {Constants.ArbitraryIdentity} request");
            var subject = request.ValidatedRequest.Subject;
            //////////////////////////
            // access token
            /////////////////////////
            (var accessToken, var refreshToken) = await host.CreateAccessTokenAsync(request.ValidatedRequest);
            var response = new TokenResponse
            {
                AccessToken = accessToken,
                AccessTokenLifetime = request.ValidatedRequest.AccessTokenLifetime,
                Custom = request.CustomResponse
            };
            //////////////////////////
            // refresh token
            /////////////////////////
            if (refreshToken.IsPresent())
            {
                response.RefreshToken = refreshToken;
            }

            // load the client that belongs to the authorization code
            Client client = null;
            if (request.ValidatedRequest.Client.ClientId != null)
            {
                client = await Clients.FindEnabledClientByIdAsync(request.ValidatedRequest.Client.ClientId);
            }

            if (client == null)
            {
                throw new InvalidOperationException("Client does not exist anymore.");
            }

            // var resources = await Resources.FindEnabledResourcesByScopeAsync(request.ValidatedRequest.AuthorizationCode.RequestedScopes);

            var tokenRequest = new TokenCreationRequest
            {
                Subject = subject,
                Resources = request.ValidatedRequest.ValidatedScopes.RequestedResources,
                Nonce = null,
                AccessTokenToHash = response.AccessToken,
                ValidatedRequest = request.ValidatedRequest,
                IncludeAllIdentityClaims = true
            };

            var idToken = await TokenService.CreateIdentityTokenAsync(tokenRequest);

            var arbitraryClaims = request.ValidatedRequest.Raw[Constants.ArbitraryClaims];
            if (!string.IsNullOrWhiteSpace(arbitraryClaims))
            {
                var values =
                    JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(arbitraryClaims);
                var finalClaims = 
                    from item in values
                    from c in item.Value
                    select new Claim(item.Key, c);
                foreach (var claim in finalClaims)
                {
                    idToken.Claims.Add(claim);
                }
            }

            var amr = request.ValidatedRequest.Raw[Constants.ArbitraryAmrs];
            if (!string.IsNullOrWhiteSpace(amr))
            {
                var values =
                    JsonConvert.DeserializeObject<List<string>>(amr);
                var finalClaims = from item in values
                    select new Claim(JwtClaimTypes.AuthenticationMethod, item);

                foreach (var claim in finalClaims)
                {
                    idToken.Claims.Add(claim);
                }
            }
            var audiences = request.ValidatedRequest.Raw[Constants.ArbitraryAudiences];
            if (!string.IsNullOrWhiteSpace(audiences))
            {
                var values =
                    JsonConvert.DeserializeObject<List<string>>(audiences);
                var finalClaims = from item in values
                    select new Claim(JwtClaimTypes.Audience, item);

                foreach (var claim in finalClaims)
                {
                    idToken.Claims.Add(claim);
                }
            }
            var clientExtra = request.ValidatedRequest.Client as ClientExtra;
            if (!string.IsNullOrEmpty(clientExtra.Watermark))
            {
                idToken.Claims.Add(new Claim("nudibranch_watermark", clientExtra.Watermark));
            }
            var jwt = await TokenService.CreateSecurityTokenAsync(idToken);
            response.IdentityToken = jwt;
            return response;

        }
    }
}
