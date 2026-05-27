using JobSearchBuilder.Interfaces;
using System;

namespace JobSearchBuilder.Services.Providers
{
    public static class LlmProviderFactory
    {
        public static ILlmProvider Create(AppSettings settings, string providerOverride = null)
        {
            if (settings == null)
                throw new ArgumentNullException("settings");

            if (settings.Ai == null)
                throw new InvalidOperationException("Ai settings are missing from configuration.");

            string provider = string.IsNullOrWhiteSpace(providerOverride)
                ? settings.Ai.Provider
                : providerOverride;
            if (string.IsNullOrWhiteSpace(provider))
                throw new InvalidOperationException("Ai.Provider is missing from configuration.");

            if (string.Equals(provider, "Anthropic", StringComparison.OrdinalIgnoreCase))
                return new AnthropicProvider(settings);

            if (string.Equals(provider, "OpenAI", StringComparison.OrdinalIgnoreCase))
                return new OpenAiProvider(settings);

            if (string.Equals(provider, "Gemini", StringComparison.OrdinalIgnoreCase))
                return new GeminiProvider(settings);

            throw new InvalidOperationException("Unknown AI provider: " + provider);
        }
    }
}
