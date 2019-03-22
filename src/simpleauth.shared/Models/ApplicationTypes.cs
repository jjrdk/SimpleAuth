﻿namespace SimpleAuth.Shared.Models
{
    using System.Net;

    /// <summary>
    /// Defines the application types.
    /// </summary>
    public static class ApplicationTypes
    {
        /// <summary>
        /// Native application
        /// </summary>
        public const string Native = "native";

        /// <summary>
        /// Web application
        /// </summary>
        public const string Web = "web";
    }

    /// <summary>
    /// A machine-readable format for specifying errors in HTTP API responses based on https://tools.ietf.org/html/rfc7807.
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// A short, human-readable summary of the problem type.It SHOULD NOT change from occurrence to occurrence
        /// of the problem, except for purposes of localization(e.g., using proactive content negotiation;
        /// see[RFC7231], Section 3.4).
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The HTTP status code([RFC7231], Section 6) generated by the origin server for this occurrence of the problem.
        /// </summary>
        public HttpStatusCode Status { get; set; } = HttpStatusCode.BadRequest;

        /// <summary>
        /// A human-readable explanation specific to this occurrence of the problem.
        /// </summary>
        public string Detail { get; set; }
    }
}