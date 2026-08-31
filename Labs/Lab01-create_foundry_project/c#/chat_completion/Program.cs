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
        var azureSection = context.Configuration.GetSection(AzureOpenAIOptions.SectionName);
        services.Configure<AzureOpenAIOptions>(azureSection);

        Console.WriteLine("Configuration section '{0}':", AzureOpenAIOptions.SectionName);
        foreach (var child in azureSection.GetChildren())
        {
            Console.WriteLine("{0} = {1}", child.Path, child.Value ?? "<null>");
        }

        services.AddSingleton<IOpenAIClient, MyOpenAIClient>();
    })
    .Build();

IOpenAIClient client = host.Services.GetRequiredService<IOpenAIClient>();

string answer = await client.GetResponseAsync("What is the capital of Austria.?");

Console.WriteLine($"[ASSISTANT]: {answer}");


Console.WriteLine("App finished!");
