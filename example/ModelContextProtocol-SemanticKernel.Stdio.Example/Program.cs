using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.SemanticKernel.Extensions;

var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

using var cts = new CancellationTokenSource();

var builder = Kernel.CreateBuilder();
builder.Services.AddLogging(c => c.AddDebug().SetMinimumLevel(LogLevel.Trace));

// Add this to use OpenAI service with retry logic
builder.Services.AddOpenAIChatCompletionWithRetry(
    modelId: "gpt-4o",
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
);
builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4o",
    apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!,
    endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!
);

var kernel = builder.Build();

await kernel.Plugins.AddMcpFunctionsFromStdioServerAsync(
    "AzureDevOps",
    "dnx",
    ["--yes", "mcpserver.azuredevops.stdio"],
    new Dictionary<string, string>
    {
        { "AZURE_DEVOPS_ORG_URL", "https://dev.azure.com/alfa1group" },
        { "AZURE_DEVOPS_AUTH_METHOD", "pat" },
        { "AZURE_DEVOPS_PAT", Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT")! }
    },
    cancellationToken: cts.Token);

await kernel.Plugins.AddMcpFunctionsFromStdioServerAsync("Everything", "dnx", ["--yes", "mcpserver.everything.stdio"], cancellationToken: cts.Token);

await kernel.Plugins.AddMcpFunctionsFromStdioServerAsync("GitHub", "npx", ["-y", "@modelcontextprotocol/server-github"], cancellationToken: cts.Token);

var executionSettings = new OpenAIPromptExecutionSettings
{
    Temperature = 0.1,
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var result = await kernel.InvokePromptAsync("Which tools are currently registered? And what are the functions?", new(executionSettings));
Console.WriteLine($"\n\nTools:\n{result}");

var promptEcho = "Please call the echo tool with the string 'Hello Stef!' and give me the response as-is.";
var resultEcho = await kernel.InvokePromptAsync(promptEcho, new(executionSettings));
Console.WriteLine($"\n\n{promptEcho}\n{resultEcho}");

var promptComplex = "Use the Everything tool and call the add_complex function to add these complex numbers: 1 + 2i and 3 - 7i";
var resultComplex = await kernel.InvokePromptAsync(promptComplex, new(executionSettings));
Console.WriteLine($"\n\n{promptComplex}\n{resultComplex}");

var promptAzureDevops =
    """
    For the Azure Devops project 'mstack-skills' and repository 'mstack-skills-blazor', get 2 latest commits with all details.
    """;
var resultAzureDevops = await kernel.InvokePromptAsync(promptAzureDevops, new(executionSettings));
Console.WriteLine($"\n\n{promptAzureDevops}\n{resultAzureDevops}");

var promptGitHub = "Summarize the last 3 commits to the StefH/FluentBuilder repository.";
var resultGitHub = await kernel.InvokePromptAsync(promptGitHub, new(executionSettings));
Console.WriteLine($"\n\n{promptGitHub}\n{resultGitHub}");

await cts.CancelAsync();