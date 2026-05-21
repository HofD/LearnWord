using LearnWord.BL.Models.Dto;
using LearnWord.Identity.Abstactions;
using LearnWord.Identity.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LearnWord.Identity.Tests;

public class IdentityControllerTests
{
    private const string UserId = "user-1";

    [Fact]
    public async Task Collections_GetAll_WithoutResolvedUser_ReturnsUnauthorized()
    {
        var service = new StubCollectionIdentityService();
        var controller = new CollectionsIdentityController(service)
        {
            ControllerContext = CreateControllerContext()
        };

        var response = await controller.GetAll();

        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.False(service.WasCalled);
    }

    [Fact]
    public async Task Collections_Get_ReturnsNotFoundWhenLinkIsMissing()
    {
        var service = new StubCollectionIdentityService { GetResult = null };
        var controller = new CollectionsIdentityController(service)
        {
            ControllerContext = CreateControllerContext(UserId)
        };

        var response = await controller.Get(17);

        Assert.IsType<NotFoundResult>(response.Result);
        Assert.Equal((17, UserId), service.LastGet);
    }

    [Fact]
    public async Task Collections_Remove_ReturnsBadGatewayWhenUpstreamDeleteFails()
    {
        var service = new StubCollectionIdentityService { RemoveResult = false };
        var controller = new CollectionsIdentityController(service)
        {
            ControllerContext = CreateControllerContext(UserId)
        };

        var response = await controller.Remove(17);

        var status = Assert.IsType<StatusCodeResult>(response);
        Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
        Assert.Equal((17, UserId), service.LastRemove);
    }

    [Fact]
    public async Task Collections_Add_PassesCurrentUserAndReturnsCreatedCollection()
    {
        var createDto = new CollectionCreateDto { Name = "Spanish" };
        var collection = new CollectionDto { Id = 17, Name = "Spanish", Cards = [] };
        var service = new StubCollectionIdentityService { AddResult = collection };
        var controller = new CollectionsIdentityController(service)
        {
            ControllerContext = CreateControllerContext(UserId)
        };

        var response = await controller.Add(createDto);

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal(collection, created.Value);
        Assert.Equal((createDto, UserId), service.LastAdd);
    }

    [Fact]
    public async Task Cards_Add_WithoutResolvedUser_ReturnsUnauthorized()
    {
        var service = new StubCardIdentityService();
        var controller = new CardsIdentityController(service)
        {
            ControllerContext = CreateControllerContext()
        };

        var response = await controller.Add(new CardCreateDto { CollectionId = 17, Words = [] });

        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.False(service.WasCalled);
    }

    [Fact]
    public async Task Cards_Remove_ReturnsBadGatewayWhenUpstreamDeleteFails()
    {
        var service = new StubCardIdentityService { RemoveResult = false };
        var controller = new CardsIdentityController(service)
        {
            ControllerContext = CreateControllerContext(UserId)
        };

        var response = await controller.Remove(23);

        var status = Assert.IsType<StatusCodeResult>(response);
        Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
        Assert.Equal((23, UserId), service.LastRemove);
    }

    [Fact]
    public async Task Cards_Learn_PassesCurrentUserAndReturnsCard()
    {
        var card = new CardDto { Id = 23, CollectionId = 17, Words = [], Learnt = true };
        var service = new StubCardIdentityService { LearnResult = card };
        var controller = new CardsIdentityController(service)
        {
            ControllerContext = CreateControllerContext(UserId)
        };

        var response = await controller.Learn(23);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(card, ok.Value);
        Assert.Equal((23, UserId), service.LastLearn);
    }

    [Fact]
    public async Task Words_Update_PassesCardWordAndCurrentUser()
    {
        var updateDto = new WordUpdateDto
        {
            Value = "cat",
            Transcription = "kat",
            Translation = "cat-translation"
        };
        var word = new WordDto { Id = 31, Value = "cat", Transcription = "kat", Translation = "cat-translation" };
        var service = new StubWordIdentityService { UpdateResult = word };
        var controller = new WordsIdentityController(service)
        {
            ControllerContext = CreateControllerContext(UserId)
        };

        var response = await controller.Update(23, 31, updateDto);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal(word, ok.Value);
        Assert.Equal((23, 31, updateDto, UserId), service.LastUpdate);
    }

    [Fact]
    public async Task Words_Update_WithoutResolvedUser_ReturnsUnauthorized()
    {
        var service = new StubWordIdentityService();
        var controller = new WordsIdentityController(service)
        {
            ControllerContext = CreateControllerContext()
        };

        var response = await controller.Update(
            23,
            31,
            new WordUpdateDto { Value = "cat", Transcription = "kat", Translation = "cat-translation" });

        Assert.IsType<UnauthorizedResult>(response.Result);
        Assert.Null(service.LastUpdate);
    }

    [Fact]
    public async Task Words_Remove_ReturnsBadGatewayWhenUpstreamDeleteFails()
    {
        var service = new StubWordIdentityService { RemoveResult = false };
        var controller = new WordsIdentityController(service)
        {
            ControllerContext = CreateControllerContext(UserId)
        };

        var response = await controller.Remove(23, 31);

        var status = Assert.IsType<StatusCodeResult>(response);
        Assert.Equal(StatusCodes.Status502BadGateway, status.StatusCode);
        Assert.Equal((31, 23, UserId), service.LastRemove);
    }

    [Fact]
    public async Task Words_Remove_WithoutResolvedUser_ReturnsUnauthorized()
    {
        var service = new StubWordIdentityService();
        var controller = new WordsIdentityController(service)
        {
            ControllerContext = CreateControllerContext()
        };

        var response = await controller.Remove(23, 31);

        Assert.IsType<UnauthorizedResult>(response);
        Assert.Null(service.LastRemove);
    }

    private static ControllerContext CreateControllerContext(string? userId = null)
    {
        var httpContext = new DefaultHttpContext();

        if (userId != null)
        {
            httpContext.Items["UserId"] = userId;
        }

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private sealed class StubCollectionIdentityService : ICollectionIdentityService
    {
        public bool WasCalled { get; private set; }
        public CollectionDto AddResult { get; set; } = new() { Id = 1, Name = "Collection", Cards = [] };
        public CollectionDto? GetResult { get; set; } = new() { Id = 1, Name = "Collection", Cards = [] };
        public CollectionListDto GetAllResult { get; set; } = new();
        public bool? RemoveResult { get; set; } = true;
        public CollectionDto? RenameResult { get; set; } = new() { Id = 1, Name = "Collection", Cards = [] };
        public IEnumerable<CardDto> ReviewResult { get; set; } = [];
        public (CollectionCreateDto CreateDto, string UserId)? LastAdd { get; private set; }
        public (int Id, string UserId)? LastGet { get; private set; }
        public (int Id, string UserId)? LastRemove { get; private set; }

        public Task<CollectionDto> Add(CollectionCreateDto createDto, string userId)
        {
            WasCalled = true;
            LastAdd = (createDto, userId);
            return Task.FromResult(AddResult);
        }

        public Task<CollectionDto?> Get(int id, string userId)
        {
            WasCalled = true;
            LastGet = (id, userId);
            return Task.FromResult(GetResult);
        }

        public Task<CollectionListDto> GetAll(string userId)
        {
            WasCalled = true;
            return Task.FromResult(GetAllResult);
        }

        public Task<IEnumerable<CardDto>> GetCardsForReview(int collectionId, string userId)
        {
            WasCalled = true;
            return Task.FromResult(ReviewResult);
        }

        public Task<AiCardGenerationResponse> GenerateAiCards(int collectionId, AiCardGenerationRequest request, string userId)
        {
            WasCalled = true;
            return Task.FromResult(new AiCardGenerationResponse());
        }

        public Task<bool?> Remove(int id, string userId)
        {
            WasCalled = true;
            LastRemove = (id, userId);
            return Task.FromResult(RemoveResult);
        }

        public Task<CollectionDto?> Rename(int id, CollectionRenameDto renameDto, string userId)
        {
            WasCalled = true;
            return Task.FromResult(RenameResult);
        }
    }

    private sealed class StubCardIdentityService : ICardIdentityService
    {
        public bool WasCalled { get; private set; }
        public CardDto AddResult { get; set; } = new() { Id = 1, CollectionId = 1, Words = [] };
        public CardDto LearnResult { get; set; } = new() { Id = 1, CollectionId = 1, Words = [] };
        public CardDto ForgetResult { get; set; } = new() { Id = 1, CollectionId = 1, Words = [] };
        public bool RemoveResult { get; set; } = true;
        public (int Id, string UserId)? LastRemove { get; private set; }
        public (int Id, string UserId)? LastLearn { get; private set; }

        public Task<CardDto> Add(CardCreateDto cardCreateDto, string userId)
        {
            WasCalled = true;
            return Task.FromResult(AddResult);
        }

        public Task<CardDto> Forget(int id, string userId)
        {
            WasCalled = true;
            return Task.FromResult(ForgetResult);
        }

        public Task<CardDto> Learn(int id, string userId)
        {
            WasCalled = true;
            LastLearn = (id, userId);
            return Task.FromResult(LearnResult);
        }

        public Task<bool> Remove(int id, string userId)
        {
            WasCalled = true;
            LastRemove = (id, userId);
            return Task.FromResult(RemoveResult);
        }
    }

    private sealed class StubWordIdentityService : IWordIdentityService
    {
        public WordDto? AddResult { get; set; } = new() { Id = 1, Value = "cat", Transcription = "kat", Translation = "cat-translation" };
        public WordDto UpdateResult { get; set; } = new() { Id = 1, Value = "cat", Transcription = "kat", Translation = "cat-translation" };
        public bool RemoveResult { get; set; } = true;
        public (int Id, int CardId, string UserId)? LastRemove { get; private set; }
        public (int CardId, int Id, WordUpdateDto UpdateDto, string UserId)? LastUpdate { get; private set; }

        public Task<WordDto?> Add(int cardId, WordCreateDto wordCreateDto, string userId)
        {
            return Task.FromResult(AddResult);
        }

        public Task<bool> Remove(int id, int cardId, string userId)
        {
            LastRemove = (id, cardId, userId);
            return Task.FromResult(RemoveResult);
        }

        public Task<WordDto> Update(int cardId, int id, WordUpdateDto wordUpdateDto, string userId)
        {
            LastUpdate = (cardId, id, wordUpdateDto, userId);
            return Task.FromResult(UpdateResult);
        }
    }
}
