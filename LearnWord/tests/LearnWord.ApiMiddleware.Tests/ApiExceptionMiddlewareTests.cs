using System.Text.Json;
using IdentityService.Authorization.Errors;
using LearnWord.BL.Models.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using IdentityApiExceptionMiddleware = IdentityService.WebApi.Middleware.ApiExceptionMiddleware;
using LearnWordIdentityApiExceptionMiddleware = LearnWord.Identity.Middleware.ApiExceptionMiddleware;
using LearnWordWebApiExceptionMiddleware = LearnWord.WebApi.Middleware.ApiExceptionMiddleware;

namespace LearnWord.ApiMiddleware.Tests;

public class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task LearnWordIdentityMiddleware_ApiException_WritesStableProblemDetails()
    {
        var context = CreateContext("/collections");
        var middleware = new LearnWordIdentityApiExceptionMiddleware(
            _ => throw new BadRequestException("Bad collection request.", "collection_bad_request"),
            NullLogger<LearnWordIdentityApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        AssertProblem(body, 400, "Bad request", "Bad collection request.", "https://learnword/errors/collection_bad_request", "/collections", "collection_bad_request");
    }

    [Fact]
    public async Task LearnWordIdentityMiddleware_UnknownException_WritesInternalServerProblemDetails()
    {
        var context = CreateContext("/collections");
        var middleware = new LearnWordIdentityApiExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<LearnWordIdentityApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        AssertProblem(body, 500, "Internal server error", "An unexpected error occurred.", "https://learnword/errors/internal_server_error", "/collections", "internal_server_error");
    }

    [Fact]
    public async Task LearnWordWebApiMiddleware_ApiException_WritesStableProblemDetails()
    {
        var context = CreateContext("/cards");
        var middleware = new LearnWordWebApiExceptionMiddleware(
            _ => throw new BadRequestException("Bad card request.", "card_bad_request"),
            NullLogger<LearnWordWebApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        AssertProblem(body, 400, "Bad request", "Bad card request.", "https://learnword/errors/card_bad_request", "/cards", "card_bad_request");
    }

    [Fact]
    public async Task LearnWordWebApiMiddleware_UnknownException_WritesInternalServerProblemDetails()
    {
        var context = CreateContext("/cards");
        var middleware = new LearnWordWebApiExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<LearnWordWebApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        AssertProblem(body, 500, "Internal server error", "An unexpected error occurred.", "https://learnword/errors/internal_server_error", "/cards", "internal_server_error");
    }

    [Fact]
    public async Task IdentityServiceMiddleware_InvalidRefreshToken_WritesBadRequestProblemDetails()
    {
        var context = CreateContext("/auth/revoke-token");
        var middleware = new IdentityApiExceptionMiddleware(
            _ => throw new InvalidRefreshTokenException(),
            NullLogger<IdentityApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        AssertProblem(body, 400, "Bad request", "Invalid token", "https://learnword/errors/invalid_refresh_token", "/auth/revoke-token", "invalid_refresh_token");
    }

    [Fact]
    public async Task IdentityServiceMiddleware_UnknownException_WritesInternalServerProblemDetails()
    {
        var context = CreateContext("/auth/login");
        var middleware = new IdentityApiExceptionMiddleware(
            _ => throw new InvalidOperationException("boom"),
            NullLogger<IdentityApiExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var body = await ReadProblemDetails(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        AssertProblem(body, 500, "Internal server error", "An unexpected error occurred.", "https://learnword/errors/internal_server_error", "/auth/login", "internal_server_error");
    }

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonElement> ReadProblemDetails(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return document.RootElement.Clone();
    }

    private static void AssertProblem(JsonElement body, int status, string title, string detail, string type, string instance, string code)
    {
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.Equal(title, body.GetProperty("title").GetString());
        Assert.Equal(detail, body.GetProperty("detail").GetString());
        Assert.Equal(type, body.GetProperty("type").GetString());
        Assert.Equal(instance, body.GetProperty("instance").GetString());
        Assert.Equal(code, body.GetProperty("code").GetString());
    }
}
