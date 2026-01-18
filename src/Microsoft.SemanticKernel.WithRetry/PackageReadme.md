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

## Usage

### Before
```csharp
var result = await kernel.InvokePromptAsync("Which tools are currently registered?");
Console.WriteLine($"\n\nTools:\n{result}");
```

### After
```csharp
var result = await kernel.InvokePromptAsync("Which tools are currently registered?").WithRetryAsync(logger, cts.Token);
Console.WriteLine($"\n\nTools:\n{result}");
```

---

### Sponsors

[Entity Framework Extensions](https://entityframework-extensions.net/?utm_source=StefH) and [Dapper Plus](https://dapper-plus.net/?utm_source=StefH) are major sponsors and proud to contribute to the development of **JsonConverter**.

[![Entity Framework Extensions](https://raw.githubusercontent.com/StefH/resources/main/sponsor/entity-framework-extensions-sponsor.png)](https://entityframework-extensions.net/bulk-insert?utm_source=StefH)

[![Dapper Plus](https://raw.githubusercontent.com/StefH/resources/main/sponsor/dapper-plus-sponsor.png)](https://dapper-plus.net/bulk-insert?utm_source=StefH)