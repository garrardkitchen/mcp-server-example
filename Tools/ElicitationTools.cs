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
    
    [McpServerTool(IconSource = "https://raw.githubusercontent.com/microsoft/fluentui-emoji/main/assets/Game%20die/Flat/game_die_flat.svg")]
    [Description("A simple game where the user has to guess a number between 1 and 10. #elicitation")]
    public async Task<string> GuessTheNumber(McpServer server, CancellationToken token)
    {
        
        _logger.LogInformation("GuessTheNumber tool invoked");
        // Ask if they want to play
        var playResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Do you want to play a game?",
            RequestedSchema = new ElicitRequestParams.RequestSchema
            {
                Properties =
                {
                    ["Options"] = new ElicitRequestParams.TitledSingleSelectEnumSchema
                    { 
                        OneOf = new List<ElicitRequestParams.EnumSchemaOption>
                        {
                            new() { Const = "0-10", Title = "Low range (0–10)" },
                            new() { Const = "11-20", Title = "High range (11–20)" }
                        }
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
                    },
                    ["Options2"] = new ElicitRequestParams.TitledMultiSelectEnumSchema
                    { 
                        Description = "Select multiple options",
                        Items = new ElicitRequestParams.TitledEnumItemsSchema
                        {
                            AnyOf = new List<ElicitRequestParams.EnumSchemaOption>
                            {
                                new() { Const = "opt1", Title = "Option 1" },
                                new() { Const = "opt2", Title = "Option 2" },
                                new() { Const = "opt3", Title = "Option 3" }
                            }
                        }
   
                    },
                    
                }
            }
        }, token);
        
        _logger.LogInformation("playResponse");

        if (!playResponse.IsAccepted)
            return "Maybe next time!";

        
        _logger.LogInformation("playResponse IsAccepted");
        
        // Get game parameters and ask for guess
        var title = playResponse.Content?.TryGetValue("Title", out var titleEl) == true
            ? titleEl.GetString() ?? "Guess the number"
            : "Guess the number";
        var useHighRange = playResponse.Content?.TryGetValue("Options", out var optionsEl) == true
            && optionsEl.GetString() == "11-20";
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

        var guess = guessResponse.Content?.TryGetValue("Answer", out var answerEl) == true
            ? answerEl.GetInt32()
            : (int?)null;
        var correctAnswer = _random.Next(min, max + 1);

        _logger.LogInformation("Guess: {Guess}, Correct answer: {Answer}", guess, correctAnswer);

        return guess == correctAnswer 
            ? "You guessed correctly!" 
            : $"You guessed wrong! Correct answer was {correctAnswer}";
    }
}