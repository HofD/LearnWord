using LearnWord.BL.Models.Errors;
using Microsoft.AspNetCore.Mvc;

namespace LearnWord.Identity.Middleware
{
    public class ApiExceptionMiddleware
    {
        private readonly RequestDelegate next;
        private readonly ILogger<ApiExceptionMiddleware> logger;

        public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
        {
            this.next = next;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (ApiException exception)
            {
                await WriteProblemDetails(context, exception.StatusCode, exception.Title, exception.ErrorCode, exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                await WriteProblemDetails(context, StatusCodes.Status403Forbidden, "Forbidden", "forbidden", exception.Message);
            }
            catch (HttpRequestException exception)
            {
                logger.LogError(exception, "Upstream service request failed");
                await WriteProblemDetails(
                    context,
                    StatusCodes.Status502BadGateway,
                    "Upstream service error",
                    "upstream_service_error",
                    "An upstream service request failed.");
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled exception while processing request");
                await WriteProblemDetails(
                    context,
                    StatusCodes.Status500InternalServerError,
                    "Internal server error",
                    "internal_server_error",
                    "An unexpected error occurred.");
            }
        }

        private static async Task WriteProblemDetails(
            HttpContext context,
            int statusCode,
            string title,
            string errorCode,
            string detail)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = $"https://learnword/errors/{errorCode}",
                Instance = context.Request.Path
            };

            problemDetails.Extensions["code"] = errorCode;

            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
