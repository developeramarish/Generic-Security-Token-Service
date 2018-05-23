﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using IdentityServer4.Hosting;
using IdentityServer4Extras.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace IdentityServer4.HostApp.Redis.Controllers
{
    [Produces("application/json")]
    [Route("api/TokenEndpointExtra")]
    public class TokenEndpointExtraController : Controller
    {
        private IEndpointHandlerExtra _endpointHandlerExtra;

        public TokenEndpointExtraController(IEndpointHandlerExtra endpointHandlerExtra)
        {
            _endpointHandlerExtra = endpointHandlerExtra;
        }

        // POST: api/Default
        [HttpPost]
        public async Task<IEndpointResult> PostAsync()
        {
            var arbitraryClaims = new Dictionary<string, string>
            {
                {"some_guid", Guid.NewGuid().ToString()},
                {"in", "flames"}
            };
            var jsonArbitraryClaims = JsonConvert.SerializeObject(arbitraryClaims);

            IFormCollection formCollection = new FormCollection(new Dictionary<string, StringValues>()
            {
                {"grant_type", "arbitrary_resource_owner"},
                {"client_id", "arbitrary-resource-owner-client"},
                {"client_secret", "secret"},
                {"scope", "offline_access nitro metal"},
                {"arbitrary_claims", jsonArbitraryClaims},
                {"subject", "Ratt"},
                {"access_token_lifetime", "3600"},
            });
          

            var request = A.Fake<HttpRequest>();
            A.CallTo(() => request.Method).Returns("POST");
            IHeaderDictionary headerDictionary = new HeaderDictionary();
            A.CallTo(() => request.Headers).Returns(headerDictionary);
            A.CallTo(() => request.Form).Returns(formCollection);
            A.CallTo(() => request.ReadFormAsync(A<CancellationToken>._)).Returns(Task.FromResult(formCollection));
            
            A.CallTo(() => request.HasFormContentType).Returns(true);
           
            var httpContext = A.Fake<HttpContext>();
            A.CallTo(() => httpContext.Request).Returns(request);

            return await _endpointHandlerExtra.ProcessAsync(httpContext);
        }
    }
}