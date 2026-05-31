using System.Text.Json;
using GeekSeoBackend.Models;

namespace GeekSeoBackend.Tests;

public sealed class GoogleTokenResponseTests
{
    [Fact]
    public void DeserializesGoogleSnakeCaseTokenPayload()
    {
        const string raw =
            """{"access_token":"ya29.test","refresh_token":"1//refresh","expires_in":3599,"token_type":"Bearer","scope":"https://www.googleapis.com/auth/webmasters.readonly"}""";

        var token = JsonSerializer.Deserialize<GoogleTokenResponse>(raw);

        Assert.NotNull(token);
        Assert.Equal("ya29.test", token.AccessToken);
        Assert.Equal("1//refresh", token.RefreshToken);
    }
}
