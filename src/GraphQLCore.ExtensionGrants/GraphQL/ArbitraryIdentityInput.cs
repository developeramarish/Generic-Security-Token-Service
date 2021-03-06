﻿using GraphQL.Types;

namespace GraphQLCore.ExtensionGrants.GraphQL
{
    public class ArbitraryIdentityInput : InputObjectGraphType
    {
        /*
            client_id:arbitrary-resource-owner-client
            client_secret:secret
            scope:offline_access metal nitro aggregator_service.read_only
            arbitrary_claims:{ "role": ["application", "limited"],"query":["dashboard", "licensing"],"seatId":["1234abcd"]}
            subject:886bea3f-e025-4ab9-a811-e9b86f563668
            access_token_lifetime:3600
         */
        public ArbitraryIdentityInput()
        {
            Name = "arbitrary_identity";
            Field<NonNullGraphType<StringGraphType>>("client_id");
            Field<NonNullGraphType<StringGraphType>>("client_secret");
            Field<StringGraphType>("scope");
            Field<NonNullGraphType<StringGraphType>>("arbitrary_claims");
            Field<StringGraphType>("subject");
            Field<StringGraphType>("arbitrary_amrs");
            Field<StringGraphType>("arbitrary_audiences");
            Field<IntGraphType>("access_token_lifetime");

        }
    }
}