using System.ClientModel;
using System.ClientModel.Primitives;
using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using OpenAI;
using Polly;

namespace Microsoft.SemanticKernel;

/// <summary>
/// Extension methods for IServiceCollection to add OpenAI chat completion with resilience and retry policies.
/// </summary>
public static class SemanticKernelResilienceExtensions
{
    /// <summary>
    /// Adds an OpenAI chat completion service to the service collection with built-in retry logic for failed requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="modelId">OpenAI model name, see https://platform.openai.com/docs/models</param>
    /// <param name="apiKey">OpenAI API key, see https://platform.openai.com/account/api-keys</param>
    /// <param name="serviceId">A local identifier for the given AI service.</param>
    /// <param name="httpClientBuilderAction">Optional action for <see cref="IHttpClientBuilder"/>. Can be used to register <see href="https://github.com/StefH/SanitizedHttpLogger">SanitizedHttpLogger</see>.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed API requests. Must be greater than zero. The default is 5.</param>
    /// <returns>The service collection with the OpenAI chat completion service registered.</returns>
    public static IServiceCollection AddOpenAIChatCompletionWithRetry(
        this IServiceCollection services,
        string modelId,
        string apiKey,
        string? serviceId = null,
        int maxRetryAttempts = 5,
        Action<IHttpClientBuilder>? httpClientBuilderAction = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        return services.AddOpenAIChatCompletion(
            modelId,
            CreateOpenAIClient(services, apiKey, maxRetryAttempts, httpClientBuilderAction),
            serviceId
        );
    }

    /// <summary>
    /// Adds an OpenAI chat client to the service collection with built-in retry logic for failed requests.
    /// </summary>
    /// <remarks>This method configures the OpenAI chat client to automatically retry failed requests up to
    /// the specified number of attempts. Use this method to improve resilience when communicating with the OpenAI
    /// API.</remarks>
    /// <param name="services">The service collection to which the OpenAI chat client will be added.</param>
    /// <param name="modelId">The identifier of the OpenAI model to use for chat completions.</param>
    /// <param name="apiKey">The API key used to authenticate requests to the OpenAI service.</param>
    /// <param name="serviceId">A local identifier for the given AI service.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed API requests. Must be greater than zero. The default is 5.</param>
    /// <param name="httpClientBuilderAction">Optional action for <see cref="IHttpClientBuilder"/>. Can be used to register <see href="https://github.com/StefH/SanitizedHttpLogger">SanitizedHttpLogger</see>.</param>
    /// <returns>The service collection with the OpenAI chat client registered.</returns>
    public static IServiceCollection AddOpenAIChatClientWithRetry(
        this IServiceCollection services,
        string modelId,
        string apiKey,
        string? serviceId = null,
        int maxRetryAttempts = 5,
        Action<IHttpClientBuilder>? httpClientBuilderAction = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        return services.AddOpenAIChatClient(
            modelId,
            apiKey,
            httpClient: CreateHttpClient(services, maxRetryAttempts, httpClientBuilderAction),
            serviceId: serviceId
        );
    }

    /// <summary>
    /// Adds an OpenAI embedding generator to the service collection with built-in retry logic for failed requests.
    /// </summary>
    /// <param name="services">The service collection to which the embedding generator will be added.</param>
    /// <param name="modelId">The OpenAI model id.</param>
    /// <param name="apiKey">The API key used to authenticate requests to the OpenAI service.</param>
    /// <param name="dimensions">The number of dimensions the resulting output embeddings should have. Only supported in "text-embedding-3" and later models.</param>
    /// <param name="serviceId">A local identifier for the given AI service.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed API requests. Must be greater than zero. The default is 5.</param>
    /// <param name="httpClientBuilderAction">Optional action for <see cref="IHttpClientBuilder"/>. Can be used to register <see href="https://github.com/StefH/SanitizedHttpLogger">SanitizedHttpLogger</see>.</param>
    /// <returns>The service collection with the OpenAI embedding generator and retry logic registered.</returns>
    public static IServiceCollection AddOpenAIEmbeddingGeneratorWithRetry(
        this IServiceCollection services,
        string modelId,
        string apiKey,
        int? dimensions = null,
        string? serviceId = null,
        int maxRetryAttempts = 5,
        Action<IHttpClientBuilder>? httpClientBuilderAction = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

#pragma warning disable SKEXP0010
        return services.AddOpenAIEmbeddingGenerator(
            modelId,
            apiKey,
            dimensions: dimensions,
            serviceId: serviceId,
            httpClient: CreateHttpClient(services, maxRetryAttempts, httpClientBuilderAction)
        );
#pragma warning restore SKEXP0010
    }

    /// <summary>
    /// Adds OpenAI audio-to-text transcription services to the dependency injection container with built-in retry logic
    /// for failed requests.
    /// </summary>
    /// <remarks>This method configures the audio-to-text service to automatically retry failed requests up to
    /// the specified number of attempts. Use this method to improve resilience when integrating OpenAI audio
    /// transcription into your application.</remarks>
    /// <param name="services">The service collection to which the audio-to-text services will be added.</param>
    /// <param name="modelId">The identifier of the OpenAI model to use for audio transcription.</param>
    /// <param name="apiKey">The API key used to authenticate requests to the OpenAI service.</param>
    /// <param name="serviceId">A local identifier for the given AI service.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed API requests. Must be greater than zero. The default is 5.</param>
    /// <param name="httpClientBuilderAction">Optional action for <see cref="IHttpClientBuilder"/>. Can be used to register <see href="https://github.com/StefH/SanitizedHttpLogger">SanitizedHttpLogger</see>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance that was provided, with audio-to-text services configured.</returns>
    public static IServiceCollection AddOpenAIAudioToTextWithRetry(
        this IServiceCollection services,
        string modelId,
        string apiKey,
        string? serviceId = null,
        int maxRetryAttempts = 5,
        Action<IHttpClientBuilder>? httpClientBuilderAction = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

#pragma warning disable SKEXP0010
        return services.AddOpenAIAudioToText(
            modelId,
            CreateOpenAIClient(services, apiKey, maxRetryAttempts, httpClientBuilderAction),
            serviceId
        );
#pragma warning restore SKEXP0010
    }

    /// <summary>
    /// Adds the <see cref="AzureOpenAIChatCompletionService"/> to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> instance to augment.</param>
    /// <param name="deploymentName">Azure OpenAI deployment name, see https://learn.microsoft.com/azure/cognitive-services/openai/how-to/create-resource</param>
    /// <param name="endpoint">Azure OpenAI deployment URL, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="apiKey">Azure OpenAI API key, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="serviceId">A local identifier for the given AI service</param>
    /// <param name="modelId">Model identifier, see https://learn.microsoft.com/azure/cognitive-services/openai/quickstart</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed API requests. Must be greater than zero. The default is 5.</param>
    /// <param name="httpClientBuilderAction">Optional action for <see cref="IHttpClientBuilder"/>. Can be used to register <see href="https://github.com/StefH/SanitizedHttpLogger">SanitizedHttpLogger</see>.</param>
    /// <returns>The same instance as <paramref name="services"/>.</returns>
    public static IServiceCollection AddAzureOpenAIChatCompletionWithRetry(
        this IServiceCollection services,
        string deploymentName,
        string endpoint,
        string apiKey,
        string? serviceId = null,
        string? modelId = null,
        int maxRetryAttempts = 5,
        Action<IHttpClientBuilder>? httpClientBuilderAction = null
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(deploymentName);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        return services.AddAzureOpenAIChatCompletion(
            deploymentName,
            CreateAzureOpenAIClient(services, endpoint, apiKey, maxRetryAttempts, httpClientBuilderAction),
            serviceId,
            modelId
        );
    }

    private static OpenAIClient CreateOpenAIClient(IServiceCollection services, string apiKey, int maxRetryAttempts, Action<IHttpClientBuilder>? httpClientBuilderAction)
    {
        var clientOptions = new OpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(CreateHttpClient(services, maxRetryAttempts, httpClientBuilderAction)),
            NetworkTimeout = TimeSpan.FromSeconds(100),
            RetryPolicy = new ClientRetryPolicy(maxRetryAttempts)
        };

        return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
    }

    private static AzureOpenAIClient CreateAzureOpenAIClient(IServiceCollection services, string apiKey, string endpoint, int maxRetryAttempts, Action<IHttpClientBuilder>? httpClientBuilderAction)
    {
        var clientOptions = new AzureOpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(CreateHttpClient(services, maxRetryAttempts, httpClientBuilderAction)),
            NetworkTimeout = TimeSpan.FromSeconds(100),
            RetryPolicy = new ClientRetryPolicy(maxRetryAttempts)
        };

        return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey), clientOptions);
    }

    private static HttpClient CreateHttpClient(IServiceCollection services, int maxRetryAttempts, Action<IHttpClientBuilder>? httpClientBuilderAction)
    {
        var httpClientName = GenerateName();

        var httpClientBuilder = services
            .AddHttpClient(httpClientName);

        if (httpClientBuilderAction is not null)
        {
            httpClientBuilderAction(httpClientBuilder);
        }

        httpClientBuilder
            .AddStandardResilienceHandler()
            .Configure(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromHours(1);

                options.Retry.Delay = TimeSpan.FromSeconds(30);
                options.Retry.BackoffType = DelayBackoffType.Exponential;
                options.Retry.MaxRetryAttempts = maxRetryAttempts;
            });

        var serviceProvider = services.BuildServiceProvider();
        var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        return httpClientFactory.CreateClient(httpClientName);
    }

    private static string GenerateName() => $"openai-{Guid.NewGuid():N}";
}