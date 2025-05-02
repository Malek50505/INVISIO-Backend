using INVISIO.Middleware;
using INVISIO.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var firstError = context.ModelState
                .Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault();

            return new BadRequestObjectResult(new
            {
                code = 4001,
                message = firstError ?? "Validation failed."
            });
        };
    });

// MongoDB
builder.Services.AddSingleton<IMongoClient, MongoClient>(
    _ => new MongoClient(builder.Configuration["MongoDB:ConnectionString"])
);

// Custom services
builder.Services.AddSingleton<INVISIOService>();
builder.Services.AddSingleton<BlacklistService>();

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };

        // Handle invalid JWT format globally
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    code = 5005,
                    message = "Invalid or expired token."
                });
                return context.Response.WriteAsync(result);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Global handler for malformed JSON request bodies
app.Use(async (context, next) =>
{
    try
    {
        await next.Invoke();
    }
    catch (JsonException)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            code = 4002,
            message = "Invalid JSON format."
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapControllers();
app.Run();
