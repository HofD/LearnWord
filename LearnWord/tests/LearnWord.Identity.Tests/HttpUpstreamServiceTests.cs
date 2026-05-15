using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using LearnWord.BL.Models.Dto;
using LearnWord.BL.Models.Errors;
using LearnWord.Identity.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LearnWord.Identity.Tests;

public class HttpUpstreamServiceTests
{
    [Fact]
    public async Task CollectionsHttpService_Add_PostsJsonToCollectionsRouteAndReturnsCollection()
    {
        await using var server = await TestHttpServer.Start(async context =>
        {
            Assert.Equal("POST", context.Request.HttpMethod);
            Assert.Equal("/collections", context.Request.Url!.AbsolutePath);
            using var body = await ReadJsonBody(context.Request);
            Assert.Equal("Spanish", body.RootElement.GetProperty("name").GetString());

            await WriteJson(context.Response, new CollectionDto { Id = 17, Name = "Spanish", Cards = [] }, HttpStatusCode.Created);
        });
        var service = new CollectionsHttpService(CreateConfiguration("CollectionsRoute", server.Url("/collections")));

        var result = await service.Add(new CollectionCreateDto { Name = "Spanish" });

        Assert.Equal(17, result.Id);
        Assert.Equal("Spanish", result.Name);
    }

    [Fact]
    public async Task CollectionsHttpService_GetList_SendsIdsAsQueryString()
    {
        await using var server = await TestHttpServer.Start(async context =>
        {
            Assert.Equal("GET", context.Request.HttpMethod);
            Assert.Equal("/collections", context.Request.Url!.AbsolutePath);
            Assert.Equal("?&ids=3&ids=5", context.Request.Url.Query);

            await WriteJson(context.Response, new CollectionListDto
            {
                Collections = [new CollectionListEntityDto { Id = 3, Name = "Spanish" }]
            });
        });
        var service = new CollectionsHttpService(CreateConfiguration("CollectionsRoute", server.Url("/collections")));

        var result = await service.GetList([3, 5]);

        Assert.Single(result.Collections);
        Assert.Equal(3, result.Collections.First().Id);
    }

    [Fact]
    public async Task CollectionsHttpService_UnsuccessfulStatus_ThrowsUpstreamServiceException()
    {
        await using var server = await TestHttpServer.Start(context =>
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Close();
            return Task.CompletedTask;
        });
        var service = new CollectionsHttpService(CreateConfiguration("CollectionsRoute", server.Url("/collections")));

        var exception = await Assert.ThrowsAsync<UpstreamServiceException>(() => service.Get(17));

        Assert.Equal(StatusCodes.Status502BadGateway, exception.StatusCode);
        Assert.Contains("Failed to get collection 17", exception.Message);
        Assert.Contains("503", exception.Message);
    }

    [Fact]
    public async Task CardHttpService_Learn_PostsToLearnRouteAndReturnsCard()
    {
        await using var server = await TestHttpServer.Start(async context =>
        {
            Assert.Equal("POST", context.Request.HttpMethod);
            Assert.Equal("/cards/23/learn", context.Request.Url!.AbsolutePath);
            using var body = await ReadJsonBody(context.Request);
            Assert.Equal(23, body.RootElement.GetProperty("id").GetInt32());

            await WriteJson(context.Response, new CardDto { Id = 23, CollectionId = 17, Learnt = true, Words = [] });
        });
        var service = new CardHttpService(CreateConfiguration("CardsRoute", server.Url("/cards")));

        var result = await service.Learn(23);

        Assert.Equal(23, result.Id);
        Assert.True(result.Learnt);
    }

    [Fact]
    public async Task CardHttpService_Remove_UnsuccessfulStatus_ThrowsUpstreamServiceException()
    {
        await using var server = await TestHttpServer.Start(context =>
        {
            Assert.Equal("DELETE", context.Request.HttpMethod);
            Assert.Equal("/cards/23", context.Request.Url!.AbsolutePath);
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            context.Response.Close();
            return Task.CompletedTask;
        });
        var service = new CardHttpService(CreateConfiguration("CardsRoute", server.Url("/cards")));

        var exception = await Assert.ThrowsAsync<UpstreamServiceException>(() => service.Remove(23));

        Assert.Contains("Failed to remove card 23", exception.Message);
        Assert.Contains("409", exception.Message);
    }

    [Fact]
    public async Task WordHttpService_Add_PostsJsonUnderCardWordsRouteAndReturnsWord()
    {
        await using var server = await TestHttpServer.Start(async context =>
        {
            Assert.Equal("POST", context.Request.HttpMethod);
            Assert.Equal("/cards/23/words", context.Request.Url!.AbsolutePath);
            using var body = await ReadJsonBody(context.Request);
            Assert.Equal("cat", body.RootElement.GetProperty("value").GetString());
            Assert.Equal("kat", body.RootElement.GetProperty("transcription").GetString());
            Assert.Equal("cat-translation", body.RootElement.GetProperty("translation").GetString());

            await WriteJson(context.Response, new WordDto { Id = 31, Value = "cat", Transcription = "kat", Translation = "cat-translation" });
        });
        var service = new WordHttpService(CreateConfiguration("WordsRoute", server.Url("/cards")));

        var result = await service.Add(23, new WordCreateDto
        {
            Value = "cat",
            Transcription = "kat",
            Translation = "cat-translation"
        });

        Assert.Equal(31, result.Id);
        Assert.Equal("cat", result.Value);
    }

    [Fact]
    public async Task WordHttpService_Update_PutsJsonUnderCardWordRouteAndReturnsWord()
    {
        await using var server = await TestHttpServer.Start(async context =>
        {
            Assert.Equal("PUT", context.Request.HttpMethod);
            Assert.Equal("/cards/23/words/31", context.Request.Url!.AbsolutePath);
            using var body = await ReadJsonBody(context.Request);
            Assert.Equal("dog", body.RootElement.GetProperty("value").GetString());

            await WriteJson(context.Response, new WordDto { Id = 31, Value = "dog", Transcription = "dog", Translation = "dog-translation" });
        });
        var service = new WordHttpService(CreateConfiguration("WordsRoute", server.Url("/cards")));

        var result = await service.Update(23, 31, new WordUpdateDto
        {
            Value = "dog",
            Transcription = "dog",
            Translation = "dog-translation"
        });

        Assert.Equal("dog", result.Value);
    }

    [Fact]
    public async Task WordHttpService_UnsuccessfulStatus_ThrowsUpstreamServiceException()
    {
        await using var server = await TestHttpServer.Start(context =>
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.Close();
            return Task.CompletedTask;
        });
        var service = new WordHttpService(CreateConfiguration("WordsRoute", server.Url("/cards")));

        var exception = await Assert.ThrowsAsync<UpstreamServiceException>(() => service.Remove(31, 23));

        Assert.Contains("Failed to remove word 31", exception.Message);
        Assert.Contains("404", exception.Message);
    }

    private static IConfiguration CreateConfiguration(string routeKey, string routeValue)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"LwServicesRoutes:{routeKey}"] = routeValue
            })
            .Build();
    }

    private static async Task<JsonDocument> ReadJsonBody(HttpListenerRequest request)
    {
        return await JsonDocument.ParseAsync(request.InputStream);
    }

    private static async Task WriteJson(HttpListenerResponse response, object body, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = "application/json";
        var payload = JsonSerializer.Serialize(body, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var bytes = Encoding.UTF8.GetBytes(payload);
        response.ContentLength64 = bytes.Length;
        await response.OutputStream.WriteAsync(bytes);
        response.Close();
    }

    private sealed class TestHttpServer : IAsyncDisposable
    {
        private readonly HttpListener listener;
        private readonly CancellationTokenSource cancellation = new();
        private readonly Func<HttpListenerContext, Task> handler;
        private readonly Task listenLoop;

        private TestHttpServer(HttpListener listener, int port, Func<HttpListenerContext, Task> handler)
        {
            this.listener = listener;
            this.handler = handler;
            Port = port;
            listenLoop = ListenLoop();
        }

        private int Port { get; }

        public string Url(string path) => $"http://127.0.0.1:{Port}{path}";

        public static Task<TestHttpServer> Start(Func<HttpListenerContext, Task> handler)
        {
            var port = GetFreePort();
            var listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
            return Task.FromResult(new TestHttpServer(listener, port, handler));
        }

        private async Task ListenLoop()
        {
            try
            {
                while (!cancellation.IsCancellationRequested)
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => handler(context), cancellation.Token);
                }
            }
            catch (HttpListenerException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (cancellation.IsCancellationRequested)
            {
            }
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        public async ValueTask DisposeAsync()
        {
            cancellation.Cancel();
            listener.Stop();
            listener.Close();

            try
            {
                await listenLoop;
            }
            catch (HttpListenerException)
            {
            }

            cancellation.Dispose();
        }
    }
}
