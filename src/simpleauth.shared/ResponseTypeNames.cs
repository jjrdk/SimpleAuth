namespace SimpleAuth.Shared
{
    public static class ResponseTypeNames
    {
        public const string Code = "code";
        public const string Token = "token";
        public const string IdToken = "id_token";
        public static readonly string[] All = { Code, IdToken, Token };
    }
}