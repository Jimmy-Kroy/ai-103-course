using System;
using System.Collections.Generic;
using System.Text;

namespace chat_completion
{
    /// <summary>
    /// Strongly-typed settings bound from the "AzureOpenAI" section of configuration
    /// (appsettings.json, environment variables, user-secrets, Key Vault, etc.).
    /// </summary>
    public sealed class AzureOpenAIOptions
    {
        /// <summary>
        /// The configuration section name this class binds to.
        /// </summary>
        public const string SectionName = "AzureOpenAI";

        /// <summary>
        /// The deployed model/deployment name, e.g. "gpt-5.2".
        /// </summary>
        public required string DeploymentName { get; init; }

        /// <summary>
        /// The Azure OpenAI (Foundry Models) v1 endpoint, e.g.
        /// "https://{resource}.services.ai.azure.com/openai/v1".
        /// </summary>
        public required string Endpoint { get; init; }

        /// <summary>
        /// The API key used to authenticate. Never commit a real value to source
        /// control — supply it via user-secrets, environment variables, or a
        /// secret store such as Azure Key Vault.
        /// </summary>
        public required string ApiKey { get; init; }

        /// <summary>
        /// The scope used for authentication.
        /// </summary>
        public required string Scope { get; init; }

        private bool _useKey;
        /// <summary>
        /// Whether to use an API key for authentication.
        /// </summary>
        public required bool UseKey { get; init; }

        /// <summary>
        /// Validates that all required values are present. Called eagerly so
        /// misconfiguration fails fast at startup rather than on first use.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(DeploymentName))
            {
                throw new InvalidOperationException(
                    $"Configuration '{SectionName}:{nameof(DeploymentName)}' is missing or empty.");
            }

            if (string.IsNullOrWhiteSpace(Endpoint))
            {
                throw new InvalidOperationException(
                    $"Configuration '{SectionName}:{nameof(Endpoint)}' is missing or empty.");
            }

            if (!Uri.TryCreate(Endpoint, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    $"Configuration '{SectionName}:{nameof(Endpoint)}' is not a valid absolute URI: '{Endpoint}'.");
            }

            // Validate authentication configuration depending on UseKey
            if (UseKey)
            {
                // When using an API key, ApiKey must be provided
                if (string.IsNullOrWhiteSpace(ApiKey))
                {
                    throw new InvalidOperationException(
                        $"Configuration '{SectionName}:{nameof(ApiKey)}' is missing or empty when '{SectionName}:{nameof(UseKey)}' is true.");
                }
            }
            else
            {
                // When not using an API key, Scope must be provided and be a valid absolute URI
                if (string.IsNullOrWhiteSpace(Scope))
                {
                    throw new InvalidOperationException(
                        $"Configuration '{SectionName}:{nameof(Scope)}' is missing or empty when '{SectionName}:{nameof(UseKey)}' is false.");
                }

                if (!Uri.TryCreate(Scope, UriKind.Absolute, out _))
                {
                    throw new InvalidOperationException(
                        $"Configuration '{SectionName}:{nameof(Scope)}' is not a valid absolute URI: '{Scope}'.");
                }
            }
        }
    }
}
