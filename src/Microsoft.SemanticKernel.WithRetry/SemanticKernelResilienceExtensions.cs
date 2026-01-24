using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed requests. Must be greater than zero. The default is 5.</param>
    /// <returns>The service collection with the OpenAI chat completion service registered.</returns>
    public static IServiceCollection AddOpenAIChatCompletionWithRetry(this IServiceCollection services, string modelId, string apiKey, int maxRetryAttempts = 5)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        return services.AddOpenAIChatCompletion(modelId, CreateOpenAIClient(services, apiKey, maxRetryAttempts));
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
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed requests. Must be greater than zero. The default is 5.</param>
    /// <returns>The service collection with the OpenAI chat client registered.</returns>
    public static IServiceCollection AddOpenAIChatClientWithRetry(this IServiceCollection services, string modelId, string apiKey, int maxRetryAttempts = 5)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

        return services.AddOpenAIChatClient(modelId, apiKey, httpClient: CreateHttpClient(services, maxRetryAttempts));
    }

    /// <summary>
    /// Adds an OpenAI embedding generator to the service collection with built-in retry logic for failed requests.
    /// </summary>
    /// <param name="services">The service collection to which the embedding generator will be added.</param>
    /// <param name="modelId">The OpenAI model id.</param>
    /// <param name="apiKey">The API key used to authenticate requests to the OpenAI service.</param>
    /// <param name="dimensions">The number of dimensions the resulting output embeddings should have. Only supported in "text-embedding-3" and later models.</param>
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed API requests. Must be greater than zero. The default is 5.</param>
    /// <returns>The service collection with the OpenAI embedding generator and retry logic registered.</returns>
    public static IServiceCollection AddOpenAIEmbeddingGeneratorWithRetry(this IServiceCollection services, string modelId, string apiKey, int? dimensions = null, int maxRetryAttempts = 5)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

#pragma warning disable SKEXP0010
        return services.AddOpenAIEmbeddingGenerator(modelId, apiKey, dimensions: dimensions, httpClient: CreateHttpClient(services, maxRetryAttempts));
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
    /// <param name="maxRetryAttempts">The maximum number of retry attempts for failed audio-to-text requests. Must be greater than zero. The default
    /// is 5.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance that was provided, with audio-to-text services configured.</returns>
    public static IServiceCollection AddOpenAIAudioToTextWithRetry(this IServiceCollection services, string modelId, string apiKey, int maxRetryAttempts = 5)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelId);
        ArgumentException.ThrowIfNullOrEmpty(apiKey);

#pragma warning disable SKEXP0010
        return services.AddOpenAIAudioToText(modelId, CreateOpenAIClient(services, apiKey, maxRetryAttempts));
#pragma warning restore SKEXP0010
    }

    private static HttpClient CreateHttpClient(IServiceCollection services, int maxRetryAttempts)
    {
        var httpClientName = GenerateName();

        services
            .AddHttpClient(httpClientName)
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

    private static OpenAIClient CreateOpenAIClient(IServiceCollection services, string apiKey, int maxRetryAttempts)
    {
        var clientOptions = new OpenAIClientOptions
        {
            Transport = new HttpClientPipelineTransport(CreateHttpClient(services, maxRetryAttempts)),
            NetworkTimeout = TimeSpan.FromSeconds(100),
            RetryPolicy = new ClientRetryPolicy(maxRetryAttempts)
        };

        return new OpenAIClient(new ApiKeyCredential(apiKey), clientOptions);
    }

    private static string GenerateName() => $"openai-{Guid.NewGuid():N}";
}