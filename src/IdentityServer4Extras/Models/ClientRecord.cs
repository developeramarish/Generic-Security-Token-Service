﻿using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace IdentityServer4Extras.Models
{
    public partial class ClientRecord
    {
        [JsonIgnore]
        public string ClientId { get; set; }
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        [JsonProperty("secrets")]
        public List<string> Secrets { get; set; }

        [JsonProperty("allowedScopes")]
        public List<string> AllowedScopes { get; set; }

        [JsonProperty("AllowedGrantTypes")]
        public List<string> AllowedGrantTypes { get; set; }

        [JsonProperty("AccessTokenLifetime")]
        public int AccessTokenLifetime { get; set; }

        [JsonProperty("AbsoluteRefreshTokenLifetime")]
        public int AbsoluteRefreshTokenLifetime { get; set; }

        [JsonProperty("SlidingRefreshTokenLifetime")]
        public int SlidingRefreshTokenLifetime { get; set; }

        [JsonProperty("RefreshTokenUsage")]
        public long RefreshTokenUsage { get; set; }

        [JsonProperty("AccessTokenType")]
        public long AccessTokenType { get; set; }

        [JsonProperty("AllowOfflineAccess")]
        public bool AllowOfflineAccess { get; set; }

        [JsonProperty("RequireClientSecret")]
        public bool RequireClientSecret { get; set; }

        [JsonProperty("RequireRefreshClientSecret")]
        public bool RequireRefreshClientSecret { get; set; }

        [JsonProperty("ClientClaimsPrefix")]
        public string ClientClaimsPrefix { get; set; }
    }
}