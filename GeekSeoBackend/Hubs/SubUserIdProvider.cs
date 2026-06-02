using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace GeekSeoBackend.Hubs;

/// <summary>
/// Maps JWT <c>sub</c> to SignalR user id so <c>Clients.User(userId)</c> delivery works.
/// </summary>
public sealed class SubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User?.FindFirst("sub")?.Value
        ?? connection.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
}
