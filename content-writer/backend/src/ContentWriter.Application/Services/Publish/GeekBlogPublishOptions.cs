namespace ContentWriter.Application.Services.Publish;

public class GeekBlogPublishOptions
{
    public const string SectionName = "GeekBlog";

    public string ApiUrl { get; set; } = "https://api.geekatyourspot.com";

    public string ApiKey { get; set; } = string.Empty;

    public string SiteBaseUrl { get; set; } = "https://www.geekatyourspot.com";

    public string RevalidateSecret { get; set; } = string.Empty;
}
