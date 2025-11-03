// filepath: c:\Users\garra\source\dotnet\mcp-server-example\Prompts\TextPrompts.cs
using System.ComponentModel;
using ModelContextProtocol.Server;

namespace EverythingServer.Prompts;

/// <summary>
/// A collection of simple text prompts exposed via MCP.
/// </summary>
[McpServerPromptType]
public class TextPrompts
{
    /// <summary>
    /// Creates a prompt that instructs the model to reverse a single word and output only the reversed result.
    /// </summary>
    /// <param name="word">The single word to reverse.</param>
    /// <returns>A prompt string.</returns>
    [McpServerPrompt, Description("Reverse a word and output only the reversed word")]
    public string ReverseWord(string word)
        => $"Reverse the following word exactly and output only the reversed word with no explanation or punctuation. Word: {word}";

    /// <summary>
    /// Creates a prompt that instructs the model to provide a one-sentence summary of the provided text.
    /// </summary>
    /// <param name="text">The input text to summarize.</param>
    /// <returns>A prompt string.</returns>
    [McpServerPrompt, Description("Provide a single concise sentence summarizing the provided text")]
    public string OneSentenceSummary(string text)
        => $"Provide a single concise sentence that summarizes the following content. Keep it under 25 words.\n\nContent:\n{text}";

    /// <summary>
    /// Creates a prompt that instructs the model to produce a brief summary, a bullet list of benefits, and a list of references.
    /// </summary>
    /// <param name="topic">The subject to summarize.</param>
    /// <returns>A prompt string.</returns>
    [McpServerPrompt, Description("Provide a short summary, a bullet list of benefits, and a list of references for the given topic")]
    public string SummaryBenefitsAndReferences(string topic)
        => "You are a helpful technical writer. Using the topic below, produce Markdown with: " +
           "1) a brief summary paragraph; 2) a bullet list of key benefits; 3) a bullet list of references (URLs or titles). " +
           "Be accurate, avoid speculation, and do not fabricate references. If unsure, say so.\n\n" +
           $"Topic:\n{topic}";
}

