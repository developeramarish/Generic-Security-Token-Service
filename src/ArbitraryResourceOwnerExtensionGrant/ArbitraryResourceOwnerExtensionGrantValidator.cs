﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.ResponseHandling;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using IdentityServer4Extras;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;

namespace ArbitraryResourceOwnerExtensionGrant
{
    public class ArbitraryResourceOwnerExtensionGrantValidator : IExtensionGrantValidator
    {
        private readonly ILogger<ArbitraryResourceOwnerExtensionGrantValidator> _logger;
        private readonly IEventService _events;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resourceStore;
        private readonly IRawClientSecretValidator _clientSecretValidator;
        private readonly ITokenResponseGenerator _tokenResponseGenerator;
        private readonly IdentityServerOptions _options;
        private ValidatedTokenRequest _validatedRequest;
        private ISystemClock _clock;
        private ArbitraryResourceOwnerRequestValidator _arbitraryResourceOwnerRequestValidator;
        private PrincipalAugmenter _principalAugmenter;
        public ArbitraryResourceOwnerExtensionGrantValidator(
            IdentityServerOptions options,
            IClientStore clientStore,
            IRawClientSecretValidator clientSecretValidator,
            IResourceStore resourceStore,
            IEventService events,
            ISystemClock clock,
            ITokenResponseGenerator tokenResponseGenerator,
            ILogger<ArbitraryResourceOwnerExtensionGrantValidator> logger,
            ArbitraryResourceOwnerRequestValidator arbitraryResourceOwnerRequestValidator,
            PrincipalAugmenter principalAugmenter)
        {
            _logger = logger;
            _clock = clock;
            _events = events;
            _clientSecretValidator = clientSecretValidator;
            _options = options;
            _clientStore = clientStore;
            _resourceStore = resourceStore;
            _tokenResponseGenerator = tokenResponseGenerator;
            _arbitraryResourceOwnerRequestValidator = arbitraryResourceOwnerRequestValidator;
            _principalAugmenter = principalAugmenter;
        }

        public async Task ValidateAsync(ExtensionGrantValidationContext context)
        {
            _logger.LogDebug("Start token request validation");

            if (context == null) throw new ArgumentNullException(nameof(context));
            var raw = context.Request.Raw;
            _validatedRequest = new ValidatedTokenRequest
            {
                Raw = raw ?? throw new ArgumentNullException(nameof(raw)),
                Options = _options
            };
            var customTokenRequestValidationContext = new CustomTokenRequestValidationContext()
            {
                Result = new TokenRequestValidationResult(_validatedRequest)
            };
            await _arbitraryResourceOwnerRequestValidator.ValidateAsync(customTokenRequestValidationContext);
            if (customTokenRequestValidationContext.Result.IsError)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidRequest,
                    customTokenRequestValidationContext.Result.Error);
                return;
            }
            var clientValidationResult = await _clientSecretValidator.ValidateAsync(_validatedRequest.Raw);
            if (clientValidationResult == null) throw new ArgumentNullException(nameof(clientValidationResult));

            _validatedRequest.SetClient(clientValidationResult.Client, clientValidationResult.Secret);

            /////////////////////////////////////////////
            // check grant type
            /////////////////////////////////////////////
            var grantType = _validatedRequest.Raw.Get(OidcConstants.TokenRequest.GrantType);
            if (grantType.IsMissing())
            {
                LogError("Grant type is missing");
                context.Result = new GrantValidationResult(TokenRequestErrors.UnsupportedGrantType);
                return;
            }
            if (grantType.Length > _options.InputLengthRestrictions.GrantType)
            {
                LogError("Grant type is too long");
                context.Result = new GrantValidationResult(TokenRequestErrors.UnsupportedGrantType);
                return;
            }

            _validatedRequest.GrantType = grantType;
            var resource = await _resourceStore.GetAllResourcesAsync();
            // get user's identity
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, context.Request.Raw.Get("subject")),
                new Claim("sub", context.Request.Raw.Get("subject"))
            };

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));
            _principalAugmenter.AugmentPrincipal(principal);
            var userClaimsFinal = new List<Claim>()
            {
                new Claim(ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryClaims, raw[ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryClaims])
            };
            context.Result = new GrantValidationResult(principal.GetSubjectId(), ArbitraryResourceOwnerExtensionGrant.Constants.ArbitraryResourceOwner, userClaimsFinal);
        }
        
        private void LogError(string message = null, params object[] values)
        {
            if (message.IsPresent())
            {
                try
                {
                    _logger.LogError(message, values);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error logging {exception}", ex.Message);
                }
            }

            //  var details = new global::IdentityServer4.Logging.TokenRequestValidationLog(_validatedRequest);
            //  _logger.LogError("{details}", details);
        }
        public string GrantType => Constants.ArbitraryResourceOwner;
    }
}
