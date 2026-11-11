namespace api.DTOs.Common
{
    // Every error the API returns has this shape: { "error": "CODE", "message": "..." }.
    public sealed record ErrorInView(string Error, string Message);
}
