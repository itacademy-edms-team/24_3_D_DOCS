namespace RusalProject.Models.Types;

/// <summary>
/// Scope of chat session: Global (main agent, no document) or Document (tied to specific document).
/// </summary>
public enum ChatScope
{
    Global = 0,
    Document = 1
}
