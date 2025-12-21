using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EverythingServer.Tools;

public class ElicitationTools
{
    private readonly ILogger<ElicitationTools> _logger;
    private Random _random = new Random();

    public ElicitationTools(ILogger<ElicitationTools> logger)
    {
        _logger = logger;
    }
    
    [McpServerTool, Description("A simple game where the user has to guess a number between 1 and 10. #elicitation")]
    public async Task<string> GuessTheNumber(
        McpServer server, // Get the McpServer from DI container
        CancellationToken token
    )
    {
        
        // First ask the user if they want to play
        var playSchema = new ElicitRequestParams.RequestSchema
        {
            Properties =
            {
                ["Answer"] = new ElicitRequestParams.BooleanSchema(),
                ["Options"] = new ElicitRequestParams.EnumSchema()
                {
                     EnumNames = new List<string> { "0-10", "11-20" }
                },
                ["Title"] = new ElicitRequestParams.StringSchema()
                {
                    MaxLength = 10,
                    Description = "The title of the game",
                    Type = "string"
                },
                 ["Date"] = new ElicitRequestParams.StringSchema()
                 {
                     Description = "Enter a date: DD/MM/YYYY",
                     Title = "Date",
                     Format = "date"
                }
            }
        };

        var playResponse = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = "Do you want to play a game?",
            RequestedSchema = playSchema
        }, token);

        var title = playResponse.Content?["Title"].GetString() ?? "Guess the number";
        var option = playResponse.Content?["Options"].GetString();
        var isOption1 = option == "0-10";
        
        _logger.LogInformation("User response for Title: {Response}", title);

        // Check if user wants to play
        if (playResponse.Action != "accept" || playResponse.Content?["Answer"].ValueKind != JsonValueKind.True)
        {
            return "Maybe next time!";
        }
        
        var step2Schema = new ElicitRequestParams.RequestSchema
        {
            Properties =
            {
                ["Answer"] = new ElicitRequestParams.NumberSchema()
                {
                    Minimum = isOption1 ? 0 : 11,
                    Maximum = isOption1 ? 10 : 20,
                    Description = isOption1 ? "Enter a value between 0 and 10" :  "Enter a value between 11 and 20",
                    Type = "number"
                },
            }
        };
        
        var step2Response = await server.ElicitAsync(new ElicitRequestParams
        {
            Message = title,
            RequestedSchema = step2Schema
        }, token);

        var answer = step2Response.Content?["Answer"].GetInt32();
        var correctAnswer = _random.Next(isOption1 ? 0 : 11, isOption1 ? 10 : 20);
        
        _logger.LogInformation("Your guess is : {Response}", answer);

        if (answer == correctAnswer)
        {
            return "You guessed correctly!";
        }

        return $"You guessed wrong! Correct answer was {correctAnswer}";
    }
}