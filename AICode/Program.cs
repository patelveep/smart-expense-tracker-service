using AICode.Database;
using AICode.Endpoints;
using AICode.Entities;
using AICode.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddCookie(IdentityConstants.ApplicationScheme)
    .AddBearerToken(IdentityConstants.BearerScheme);
builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddApiEndpoints();
builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Cache"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseCors(builder => builder
			.AllowCredentials()
			.AllowAnyOrigin()
			.AllowAnyHeader()
			.AllowAnyMethod());
	app.UseSwagger();
    app.UseSwaggerUI();
    app.ApplyMigrations();
}

app.MapIdentityApi<User>();
app.MapProductEndpoints();

app.Run();