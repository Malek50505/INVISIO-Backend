using INVISIO.Services;
using System.Text.Json;

namespace INVISIO.Middleware
{
    public class TokenBlacklistMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenBlacklistMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, BlacklistService blacklistService)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (!string.IsNullOrEmpty(token))
            {
                var isBlacklisted = await blacklistService.IsBlacklistedAsync(token);
                if (isBlacklisted)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    var result = JsonSerializer.Serialize(new
                    {
                        code = 5006,
                        message = "Token is blacklisted."
                    });
                    await context.Response.WriteAsync(result);
                    return;
                }
            }

            await _next(context);
        }
    }

    public static class TokenBlacklistMiddlewareExtensions
    {
        public static IApplicationBuilder UseTokenBlacklist(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<TokenBlacklistMiddleware>();
        }
    }
}
