//https://www.nuget.org/packages/OpenAI
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

public static class Program
{
    public static async Task Main()
    {
        try
        {
            // Clear the console when running interactively.
            if (!Console.IsOutputRedirected)
            {
                Console.Clear();
            }

            Console.WriteLine("App started");

            // Load configuration from JSON instead of a .env file.
            string settingsPath = Path.Combine(
                AppContext.BaseDirectory,
                "appsettings.json");

            string settingsJson =
                await File.ReadAllTextAsync(settingsPath);

            using JsonDocument settings =
                JsonDocument.Parse(settingsJson);

            string azureOpenAIEndpoint = GetRequiredSetting(
                settings.RootElement,
                "AZURE_OPENAI_ENDPOINT");

            string modelDeployment = GetRequiredSetting(
                settings.RootElement,
                "MODEL_DEPLOYMENT");

            string scope = GetRequiredSetting(
                settings.RootElement,
                "SCOPE");

            string apiKey = GetRequiredSetting(
                settings.RootElement,
                "API_KEY");


            if (!Uri.TryCreate(
                    azureOpenAIEndpoint,
                    UriKind.Absolute,
                    out Uri? endpoint) ||
                endpoint.Scheme != Uri.UriSchemeHttps)
            {
                throw new InvalidOperationException(
                    "AZURE_OPENAI_ENDPOINT must be a valid HTTPS URL.");
            }

            Console.WriteLine(
                $"azure_openai_endpoint = {azureOpenAIEndpoint}");
            Console.WriteLine(
                $"model_deployment = {modelDeployment}");
            Console.WriteLine(
                $"scope = {scope}");
            Console.WriteLine(
                $"api_key = {apiKey}");

            // Initialize the standard OpenAI client using an Azure API key.
            var openAIClient = new OpenAIClient(
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions
                {
                    Endpoint = endpoint
                });

            ChatClient chatClient =
                openAIClient.GetChatClient(modelDeployment);

            // Loop until the user wants to quit.
            while (true)
            {
                Console.Write(
                    "\nEnter a prompt (or type \"quit\" to exit): ");

                string? inputText = Console.ReadLine();

                if (inputText is null ||
                    inputText.Equals(
                        "quit",
                        StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (inputText.Length == 0)
                {
                    Console.WriteLine("Please enter a prompt.");
                    continue;
                }

                // Match the Python code: no conversation history.
                ChatMessage[] messages =
                [
                    new SystemChatMessage(
                        "You are a helpful AI assistant that answers " +
                        "questions and provides information."),

                    new UserChatMessage(inputText)
                ];

                // Call Chat Completions, not the Responses API.
                ChatCompletion completion =
                    await chatClient.CompleteChatAsync(messages);

                foreach (ChatMessageContentPart part in completion.Content)
                {
                    Console.Write(part.Text);
                }

                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

        private static string GetRequiredSetting(
        JsonElement root,
        string name)
    {
        if (!root.TryGetProperty(name, out JsonElement property) ||
            property.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException(
                $"Missing or invalid setting '{name}' in appsettings.json.");
        }

        string? value = property.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Setting '{name}' must not be empty in appsettings.json.");
        }

        return value;
    }

}