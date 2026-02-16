namespace RusalProject.Models.Exceptions;

/// <summary>
/// Thrown when attempting to save a document version with content identical to an existing version.
/// </summary>
public class DuplicateContentException : Exception
{
    public DuplicateContentException(string message) : base(message)
    {
    }

    public DuplicateContentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
