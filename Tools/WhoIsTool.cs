using System.ComponentModel;
using ModelContextProtocol.Server;

/// <summary>
/// Provides functionality to retrieve personalized statements about a person based on their full name.
/// </summary>
[McpServerToolType]
public class WhoIsTool
{
    /// <summary>
    /// Retrieves information about a person based on their full name and returns a personalized statement
    /// with a randomly selected positive attribute.
    /// </summary>
    /// <param name="fullname">The full name of the person to generate information for.</param>
    /// <returns>A string containing the person's name followed by a randomly selected positive description.</returns>
    [McpServerTool,
     Description(
         "Retrieves information about a person based on their full name and returns a personalized statement with a randomly selected positive attribute")]
    public string WhoIs(string fullname) => $"{fullname.ToCapitalize()} is {_superlatives[_random.Next(_superlatives.Length)]}!";
    
    private static readonly Random _random = new Random();

    private static readonly string[] _superlatives = new string[]
    {
        "an incredibly creative individual",
        "a remarkably intelligent person",
        "an exceptionally talented human being",
        "a genuinely compassionate soul",
        "an absolutely brilliant mind",
        "a truly inspirational character",
        "an amazingly resourceful problem-solver",
        "a wonderfully thoughtful individual",
        "an extraordinarily perceptive thinker",
        "a remarkably resilient person",
        "an impressively dedicated worker",
        "a genuinely kind-hearted individual",
        "an exceptionally insightful person",
        "a fantastically positive influence",
        "an incredibly determined achiever",
        "a profoundly wise individual",
        "a spectacularly talented professional",
        "a delightfully witty conversationalist",
        "an admirably courageous person",
        "a tremendously reliable colleague",
        "a brilliantly innovative thinker",
        "an astoundingly quick learner",
        "a deeply empathetic listener",
        "a marvelously enthusiastic participant",
        "a powerfully persuasive communicator",
        "a refreshingly honest individual",
        "a consistently dependable ally",
        "a strikingly original thinker",
        "a charmingly authentic character",
        "an uncommonly generous soul",
        "a remarkably patient teacher",
        "an exceptionally motivated achiever",
        "a wonderfully optimistic presence",
        "a truly extraordinary talent",
        "an impressively adaptable individual",
        "a genuinely humble leader",
        "a fascinatingly complex personality",
        "a refreshingly straightforward communicator",
        "an admirably persistent problem-solver",
        "a delightfully curious mind",
        "a genuinely thoughtful colleague",
        "a remarkably intuitive decision-maker",
        "an exceptionally collaborative team member",
        "a wonderfully supportive friend",
        "a truly visionary thinker",
        "an impressively detail-oriented professional",
        "a consistently reliable performer",
        "a remarkably versatile individual",
        "a genuinely passionate enthusiast",
        "a truly outstanding human being"
    };
}