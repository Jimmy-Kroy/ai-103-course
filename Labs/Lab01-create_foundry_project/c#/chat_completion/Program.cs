using chat_completion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;


Console.WriteLine("App started!");
// Generic Host wires up configuration (appsettings.json + appsettings.{Environment}.json
// + user-secrets in Development + environment variables, in that precedence order) and DI.
using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services
            .AddOptions<AzureOpenAIOptions>()
            .Bind(context.Configuration.GetSection(AzureOpenAIOptions.SectionName))
            .ValidateOnStart(); // fails fast at startup instead of on first call

        services.AddSingleton<IOpenAIClient, MyOpenAIClient>();
    })
    .Build();

IOpenAIClient client = host.Services.GetRequiredService<IOpenAIClient>();

string answer = await client.GetResponseAsync("What is the capital of Austria.?");

Console.WriteLine($"[ASSISTANT]: {answer}");


Console.WriteLine("App finished!");
