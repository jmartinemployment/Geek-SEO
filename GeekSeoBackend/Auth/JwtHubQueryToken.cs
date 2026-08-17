using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace GeekSeoBackend.Auth;

/// <summary>SignalR WebSockets send the JWT as <c>access_token</c> on the query string.</summary>
internal static class JwtHubQueryToken
{
    public static void AcceptAccessTokenFromQuery(JwtBearerOptions options)
    {
        var previous = options.Events?.OnMessageReceived;
        options.Events ??= new JwtBearerEvents();
        options.Events.OnMessageReceived = async context =>
        {
            if (previous is not null)
                await previous(context);
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
        };
    }
}
