using LearnWord.BL.Abstractions;
using LearnWord.BL.MappingProfiles;
using LearnWord.BL.Services;
using LearnWord.DAL;
using LearnWord.DAL.Repositories;
using LearnWord.WebApi.Middleware;
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
builder.Services.AddTransient<WordRepository>();
builder.Services.AddTransient<IWordService, WordService>();
builder.Services.AddTransient<IWordEditService, WordEditService>();

builder.Services.AddAutoMapper(options => options.AddProfile<CollectionMappingProfile>());
builder.Services.AddAutoMapper(options => options.AddProfile<CardMappingProfile>());
builder.Services.AddAutoMapper(options => options.AddProfile<WordMappingProfile>());

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
