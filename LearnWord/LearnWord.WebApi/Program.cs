using LearnWord.BL.Abstractions;
using LearnWord.BL.Mapping;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Repositories;
using LearnWord.WebApi.Abstractions;
using LearnWord.WebApi.Middleware;
using LearnWord.WebApi.Options;
using LearnWord.WebApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Db configuration
var connectionString = builder.Configuration.GetConnectionString("LwConnection");
builder.Services.AddDbContext<WordsDbContext>(options =>
    options.UseNpgsql(connectionString));



// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddTransient<CollectionRepository>();
builder.Services.AddTransient<ICollectionService, CollectionService>();
builder.Services.AddTransient<CardRepository>();
builder.Services.AddTransient<ICardService, CardService>();
builder.Services.AddTransient<ISpacedRepetitionScheduler, SpacedRepetitionScheduler>();
builder.Services.AddTransient<WordRepository>();
builder.Services.AddTransient<IWordService, WordService>();
builder.Services.AddTransient<IWordEditService, WordEditService>();
builder.Services.AddSingleton<ObjectMapper>();
builder.Services.Configure<AiCardGenerationOptions>(builder.Configuration.GetSection("AiCardGeneration"));
builder.Services.AddTransient<IAiCardGenerationService, AiCardGenerationService>();
builder.Services.AddTransient<FakeAiCardGenerationProvider>();
builder.Services.AddTransient<IAiCardGenerationProvider, ConfiguredAiCardGenerationProvider>();
builder.Services.AddHttpClient<OpenRouterAiCardGenerationProvider>((serviceProvider, httpClient) =>
{
    var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AiCardGenerationOptions>>().Value.OpenRouter;
    httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
