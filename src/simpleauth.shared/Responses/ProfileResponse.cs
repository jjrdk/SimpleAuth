﻿namespace SimpleAuth.Shared.Responses
{
    using System;
    using System.Runtime.Serialization;


    /// <summary>
    /// Defines the profile response.
    /// </summary>
    [DataContract]
    public class ProfileResponse
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>
        /// The user identifier.
        /// </value>
        [DataMember(Name = "user_id")]
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the issuer.
        /// </summary>
        /// <value>
        /// The issuer.
        /// </value>
        [DataMember(Name = "issuer")]
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the create date time.
        /// </summary>
        /// <value>
        /// The create date time.
        /// </value>
        [DataMember(Name = "create_datetime")]
        public DateTime CreateDateTime { get; set; }

        /// <summary>
        /// Gets or sets the update time.
        /// </summary>
        /// <value>
        /// The update time.
        /// </value>
        [DataMember(Name = "update_datetime")]
        public DateTime UpdateTime { get; set; }
    }
}
