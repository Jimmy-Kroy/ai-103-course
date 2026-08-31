using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Responses;
using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.Identity;

#pragma warning disable OPENAI001 // Responses API is currently marked as evaluation-only in the SDK.

namespace chat_completion
{
    /// <summary>
    /// Wraps the OpenAI/Azure OpenAI Responses API for this application.
    /// Configuration (deployment name, endpoint, API key) is read from
    /// <see cref="AzureOpenAIOptions"/> via the options pattern rather than
    /// being hard-coded, so it can come from appsettings.json, environment
    /// variables, user-secrets, or Key Vault without any code changes.
    /// </summary>
    /// <remarks>
    /// Named <c>OpenAIClient</c> per request. Note this deliberately shares a
    /// name with <c>OpenAI.OpenAIClient</c> from the SDK — since this type lives
    /// in the <c>OpenAIClientDemo</c> namespace, there's no collision in
    /// practice, but if you ever add "using OpenAI;" to this same file you'll
    /// need to alias one of the two (e.g. <c>using SdkOpenAIClient = OpenAI.OpenAIClient;</c>).
    /// </remarks>
    public sealed class MyOpenAIClient : IOpenAIClient
    {
        private readonly ResponsesClient _responsesClient;
        private readonly string _deploymentName;
        private readonly string _endpoint;
        private readonly string _apiKey;
        private readonly string _scope;
        private readonly bool _useKey;
        private readonly ILogger<MyOpenAIClient> _logger;

        /// <summary>
        /// Creates a new <see cref="MyOpenAIClient"/>, validating configuration eagerly.
        /// </summary>
        /// <param name="options">Bound "AzureOpenAI" configuration section.</param>
        /// <param name="logger">Logger (use <see cref="NullLogger{T}"/> if logging isn't set up).</param>
        public MyOpenAIClient(IOptionsMonitor<AzureOpenAIOptions> options, ILogger<MyOpenAIClient> logger)
        {
            ResponsesClient client;
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            AzureOpenAIOptions settings = options.CurrentValue;
            settings.Validate();

            _logger = logger;
            _deploymentName = settings.DeploymentName;
            _endpoint = settings.Endpoint;
            _apiKey = settings.ApiKey;
            _scope = settings.Scope;
            _useKey = settings.UseKey;

            // Print the values of the three variables to the console
            Console.WriteLine($"DeploymentName: {_deploymentName}");
            Console.WriteLine($"Endpoint: {_endpoint}");
            Console.WriteLine($"ApiKey: {_apiKey}");
            Console.WriteLine($"Scope: {_scope}");
            Console.WriteLine($"UseKey: {_useKey}");

            // Use ResponsesClientOptions instead of OpenAIClientOptions
            ResponsesClientOptions _clientOptions = new ResponsesClientOptions()
            {
                Endpoint = new Uri(_endpoint)
            };

            if (_useKey)
            {
                Console.WriteLine($"Using API Key");
                // Create the ResponsesClient using the API Key credential and client options
                client = new ResponsesClient(
                    new ApiKeyCredential(_apiKey),
                    _clientOptions
                );
            }
            else
            {
                Console.WriteLine($"Using Bearer Token");
                BearerTokenPolicy tokenProvider = new(new DefaultAzureCredential(), _scope);
                // Create the ResponsesClient using the API Key credential and client options
                client = new ResponsesClient(
                    authenticationPolicy: tokenProvider,
                    _clientOptions
                );
            }

            _responsesClient = client;
        }

        /// <inheritdoc />
        public async Task<string> GetResponseAsync(string userMessage, CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

            var requestOptions = new CreateResponseOptions
            {
                Model = _deploymentName, // Pass your Azure deployment name here
                InputItems =
                {
                    ResponseItem.CreateUserMessageItem(userMessage),
                },
            };

            _logger.LogInformation("Sending request to deployment {DeploymentName}.", _deploymentName);

            try
            {
                ResponseResult response = await _responsesClient
                    .CreateResponseAsync(requestOptions, cancellationToken)
                    .ConfigureAwait(false);

                return response.GetOutputText();
            }
            catch (ClientResultException ex)
            {
                // System.ClientModel wraps HTTP-level failures (auth, rate limit,
                // bad deployment name, etc.) in this exception type.
                _logger.LogError(ex, "Azure OpenAI request failed for deployment {DeploymentName}.", _deploymentName);
                throw;
            }
        }
    }
}
