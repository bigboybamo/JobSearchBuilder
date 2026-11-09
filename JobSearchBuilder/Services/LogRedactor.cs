using System.Text.RegularExpressions;

namespace JobSearchBuilder.Services
{
    /// <summary>
    /// Removes credentials and connection details from text before it is logged.
    /// </summary>
    public static class LogRedactor
    {
        private const string Redacted = "[REDACTED]";
        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant;

        private static readonly Regex AuthorizationValue = new Regex(
            @"\b(authorization)([""']?\s*[:=]\s*[""']?)(?:(?:bearer|basic)\s+)?[^\s""',;&]+", Options);

        private static readonly Regex ApiKeyValue = new Regex(
            @"([\w-]*api[_-]?key)([""']?\s*[:=]\s*[""']?)[^\s""',;&]+", Options);

        private static readonly Regex SecretQueryParameter = new Regex(
            @"([?&](?:key|api_key|access_token|token)=)[^&\s""']+", Options);

        private static readonly Regex BearerToken = new Regex(
            @"\bbearer\s+[A-Za-z0-9._~+/=-]{8,}", Options);

        private static readonly Regex ConnectionStringValue = new Regex(
            @"\b(server|data source|initial catalog|database|user id|uid|password|pwd)(\s*=\s*)[^;\r\n""']*", Options);

        private static readonly Regex KnownKeyFormat = new Regex(
            @"\b(?:sk-[A-Za-z0-9_-]{16,}|AIza[0-9A-Za-z_-]{20,})", Options);

        public static string Redact(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string result = AuthorizationValue.Replace(text, "$1$2" + Redacted);
            result = ApiKeyValue.Replace(result, "$1$2" + Redacted);
            result = SecretQueryParameter.Replace(result, "$1" + Redacted);
            result = BearerToken.Replace(result, "Bearer " + Redacted);
            result = ConnectionStringValue.Replace(result, "$1$2" + Redacted);
            return KnownKeyFormat.Replace(result, Redacted);
        }
    }
}
