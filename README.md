# Microsoft.SemanticKernel.WithRetry
This is an extension for the [Microsoft.SemanticKernel](https://github.com/microsoft/semantic-kernel/tree/main/dotnet) to handle 'Rate limit reached' exceptions using retries.

## Info
Semantic Kernel is a model-agnostic SDK that empowers developers to build, orchestrate, and deploy AI agents and multi-agent systems.
Whether you're building a simple chatbot or a complex multi-agent workflow, Semantic Kernel provides the tools you need with enterprise-grade reliability and flexibility.

This project can be used to handle exceptions like:

``` plaintext
HTTP 429 (tokens: rate_limit_exceeded)

Rate limit reached for gpt-4o in organization org-xxxxxxxxxxxxxxxxxxxxxxxx on tokens per min (TPM): Limit 30000, Used 27855, Requested 5405. Please try again in 6.52s. Visit https://platform.openai.com/account/rate-limits to learn more.
```

## NuGet
[![NuGet Badge](https://img.shields.io/nuget/v/Stef.Microsoft.SemanticKernel.WithRetry)](https://www.nuget.org/packages/Stef.Microsoft.SemanticKernel.WithRetry)<br>


## Usage for `OpenAI`

### Before
```csharp
var builder = Kernel.CreateBuilder();

builder.Services.AddOpenAIChatCompletion(
    serviceId: "openai",
    modelId: "gpt-4o",
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
);
```

### After
```csharp
var builder = Kernel.CreateBuilder();

builder.Services.AddOpenAIChatCompletionWithRetry(
    modelId: "gpt-4o",
    apiKey: Environment.GetEnvironmentVariable("OPENAI_API_KEY")!
);
```

## Usage for `Azure OpenAI`

### Before
```csharp
var builder = Kernel.CreateBuilder();

builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: "gpt-4o",
    apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!,
    endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!
);
```

### After
```csharp
var builder = Kernel.CreateBuilder();

builder.Services.AddAzureOpenAIChatCompletionWithRetry(
    deploymentName: "gpt-4o",
    apiKey: Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")!,
    endpoint: Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")!
);
```

---

### Sponsors

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=StefH) and [Dapper Plus](https://dapper-plus.net/?utm_source=StefH) are major sponsors and proud to contribute to the development of **Blazor.DownloadFileFast**.

[![Entity Framework Extensions](https://raw.githubusercontent.com/StefH/resources/main/sponsor/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert?utm_source=StefH)

[![Dapper Plus](https://raw.githubusercontent.com/StefH/resources/main/sponsor/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert?utm_source=StefH)