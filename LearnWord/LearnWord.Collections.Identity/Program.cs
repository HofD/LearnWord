using LearnWord.Collections.Identity.Abstactions;
using LearnWord.Collections.Identity.Authorization;
using LearnWord.Collections.Identity.DAL.Context;
using LearnWord.Collections.Identity.DAL.Repositories;
using LearnWord.Collections.Identity.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Db configuration
var connectionString = builder.Configuration.GetConnectionString("LwConnection");
builder.Services.AddDbContext<CollectionIdentityDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddTransient<JwtUtils>();
builder.Services.AddTransient<CollectionIdentityRepository>();
builder.Services.AddSingleton<ICollectionsHttpService, CollectionsHttpService>();
builder.Services.AddTransient<ICollectionIdentityService, CollectionIdentityService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.UseMiddleware<JwtMiddleware>();

app.MapControllers();

app.Run();
