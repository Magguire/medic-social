namespace Shared.Data.Entities;

public class ContentPage
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string CssContent { get; set; } = string.Empty;
    public string? SourceType { get; set; } = "Html";
    public string? DocumentFileName { get; set; }
    public string? DocumentContentType { get; set; }
    public string? DocumentUrl { get; set; }
    public long? DocumentSizeBytes { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
