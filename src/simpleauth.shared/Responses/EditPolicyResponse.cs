﻿namespace SimpleAuth.Shared.Responses
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines the view model for editing policies.
    /// </summary>
    [DataContract]
    public record EditPolicyResponse
    {
        /// <summary>
        /// Gets or sets the resource id.
        /// </summary>
        [DataMember(Name = "id")]
        public string Id { get; init; } = null!;

        /// <summary>
        /// Gets or sets the authorization policies.
        /// </summary>
        [DataMember(Name = "rules")]
        public PolicyRuleViewModel[] Rules { get; init; } = Array.Empty<PolicyRuleViewModel>();
    }
}