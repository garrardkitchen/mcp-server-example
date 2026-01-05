using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EverythingServer.Tools;

[McpServerToolType]
public class ElicitationTools
{
    private readonly ILogger<ElicitationTools> _logger;
    private static readonly Random _random = Random.Shared;

    public ElicitationTools(ILogger<ElicitationTools> logger)
    {
        _logger = logger;
    }
    
    [McpServerTool]
    [Description("A simple game where the user has to guess a number between 1 and 10. #elicitation")]
    public async Task<string> GuessTheNumber(McpServer server, CancellationToken token)
    {
        // Ask if they want to play
        var playResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Do you want to play a game?",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["Answer"] = new ElicitRequestParams.BooleanSchema(),
                    ["Options"] = new ElicitRequestParams.EnumSchema 
                    { 
                        EnumNames = new List<string> { "0-10", "11-20" } 
                    },
                    ["Title"] = new ElicitRequestParams.StringSchema 
                    { 
                        MaxLength = 30, 
                        Description = "The title of the game" 
                    },
                    ["Date"] = new ElicitRequestParams.StringSchema 
                    { 
                        Description = "Enter a date: DD/MM/YYYY", 
                        Format = "date" 
                    }
                }
            }
        }, token);

        if (playResponse.Action != "accept" || playResponse.Content?["Answer"].ValueKind != JsonValueKind.True)
            return "Maybe next time!";

        // Get game parameters and ask for guess
        var title = playResponse.Content?["Title"].GetString() ?? "Guess the number";
        var useHighRange = playResponse.Content?["Options"].GetString() == "11-20";
        var (min, max) = useHighRange ? (11, 20) : (0, 10);

        _logger.LogInformation("Game title: {Title}, Range: {Min}-{Max}", title, min, max);

        var guessResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = title,
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["Answer"] = new ElicitRequestParams.NumberSchema
                    {
                        Minimum = min,
                        Maximum = max,
                        Description = $"Enter a value between {min} and {max}"
                    }
                }
            }
        }, token);

        var guess = guessResponse.Content?["Answer"].GetInt32();
        var correctAnswer = _random.Next(min, max + 1);

        _logger.LogInformation("Guess: {Guess}, Correct answer: {Answer}", guess, correctAnswer);

        return guess == correctAnswer 
            ? "You guessed correctly!" 
            : $"You guessed wrong! Correct answer was {correctAnswer}";
    }
}