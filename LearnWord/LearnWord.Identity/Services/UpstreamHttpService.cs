using System.Diagnostics;
using LearnWord.BL.Models.Errors;
using Microsoft.Extensions.Logging.Abstractions;

namespace LearnWord.Identity.Services
{
    public abstract class UpstreamHttpService<TService>
    {
        private readonly ILogger<TService> logger;

        protected UpstreamHttpService(ILogger<TService>? logger)
        {
            this.logger = logger ?? NullLogger<TService>.Instance;
        }

        protected HttpClient HttpClient { get; } = new HttpClient();

        protected async Task<TResult> SendForJson<TResult>(
            string upstreamService,
            string operation,
            string targetUrl,
            Func<Task<HttpResponseMessage>> send,
            string emptyResponseCode,
            string failureMessage)
        {
            using var response = await Send(upstreamService, operation, targetUrl, send);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResult>();

                if (result == null)
                {
                    logger.LogWarning(
                        "Upstream {UpstreamService} returned an empty response for {Operation}.",
                        upstreamService,
                        operation);
                    throw new UpstreamServiceException(
                        $"{upstreamService} service returned an empty response.",
                        emptyResponseCode);
                }

                return result;
            }

            throw new UpstreamServiceException($"{failureMessage} {upstreamService} service returned {(int)response.StatusCode}.");
        }

        protected async Task<bool> SendForSuccess(
            string upstreamService,
            string operation,
            string targetUrl,
            Func<Task<HttpResponseMessage>> send,
            string failureMessage)
        {
            using var response = await Send(upstreamService, operation, targetUrl, send);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            throw new UpstreamServiceException($"{failureMessage} {upstreamService} service returned {(int)response.StatusCode}.");
        }

        private async Task<HttpResponseMessage> Send(
            string upstreamService,
            string operation,
            string targetUrl,
            Func<Task<HttpResponseMessage>> send)
        {
            var elapsed = Stopwatch.StartNew();
            logger.LogInformation(
                "Calling upstream {UpstreamService}: {Operation} {TargetUrl}.",
                upstreamService,
                operation,
                targetUrl);

            try
            {
                var response = await send();
                elapsed.Stop();

                if (response.IsSuccessStatusCode)
                {
                    logger.LogInformation(
                        "Upstream {UpstreamService} completed {Operation} with {StatusCode} in {ElapsedMilliseconds}ms.",
                        upstreamService,
                        operation,
                        (int)response.StatusCode,
                        elapsed.ElapsedMilliseconds);
                }
                else
                {
                    logger.LogWarning(
                        "Upstream {UpstreamService} failed {Operation} with {StatusCode} in {ElapsedMilliseconds}ms.",
                        upstreamService,
                        operation,
                        (int)response.StatusCode,
                        elapsed.ElapsedMilliseconds);
                }

                return response;
            }
            catch (Exception exception) when (exception is not UpstreamServiceException)
            {
                elapsed.Stop();
                logger.LogError(
                    exception,
                    "Upstream {UpstreamService} threw during {Operation} after {ElapsedMilliseconds}ms.",
                    upstreamService,
                    operation,
                    elapsed.ElapsedMilliseconds);
                throw new UpstreamServiceException(
                    $"Failed to call {upstreamService} service during {operation}.",
                    "upstream_service_unavailable");
            }
        }
    }
}
