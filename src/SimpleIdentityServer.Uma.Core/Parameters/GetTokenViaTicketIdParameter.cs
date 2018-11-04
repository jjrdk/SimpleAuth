﻿namespace SimpleIdentityServer.Uma.Core.Parameters
{
    using SimpleIdentityServer.Core.Parameters;

    public class GetTokenViaTicketIdParameter : GrantTypeParameter
    {
        public string Ticket { get; set; }
        public string ClaimToken { get; set; }
        public string ClaimTokenFormat { get; set; }
        public string Pct { get; set; }
        public string Rpt { get; set; }
    }
}
