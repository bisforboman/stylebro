namespace Messy;

/// <summary>BRO1108 with BRO1107 off: the first item stays on the parenthesis line.</summary>
public class Articles
{
    /// <summary>Updates an article.</summary>
    /// <param name="slug">The slug.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The token.</param>
    /// <returns>The result.</returns>
    public int UpdateBySlug(string slug,
        int request, int cancellationToken) => slug.Length + request + cancellationToken;
}
