using Claudia;
using ConsoleApp1;
using System;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;



var anthropic = new Anthropic();

var message = await anthropic.Messages.CreateAsync(new()
{
    Model = Claudia.Models.Claude3_5Sonnet, // you can use Claudia.Models.Claude3Opus string constant
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }]
});

Console.WriteLine(message);


//anthropic.HttpClient.DefaultRequestHeaders.Add("anthropic-beta", "tools-2024-04-04");

//var input = new Message { Role = Roles.User, Content = "What time is it in Los Angeles?" };
//var message = await anthropic.Messages.CreateAsync(new()
//{
//    Model = Models.Claude3Haiku,
//    MaxTokens = 1024,
//    Tools = FunctionTools.AllTools, // use generated Tools
//    Messages = [input],
//});

//var toolResult = await FunctionTools.InvokeToolAsync(message);

//var response = await anthropic.Messages.CreateAsync(new()
//{
//    Model = Models.Claude3Haiku,
//    MaxTokens = 1024,
//    Tools = [ToolUseSamples.Tools.Calculator],
//    Messages = [
//        input,
//        new() { Role = Roles.Assistant, Content = message.Content },
//        new() { Role = Roles.User, Content = toolResult! }
//    ],
//});

//// The current time in Los Angeles is 10:45 AM.
//Console.WriteLine(response.Content.ToString());


public static partial class FunctionTools
{
    /// <summary>
    /// Retrieve the current time of day in Hour-Minute-Second format for a specified time zone. Time zones should be written in standard formats such as UTC, US/Pacific, Europe/London.
    /// </summary>
    /// <param name="timeZone">The time zone to get the current time for, such as UTC, US/Pacific, Europe/London.</param>
    [ClaudiaFunction]
    public static string TimeOfDay(string timeZone)
    {
        var time = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, timeZone);
        return time.ToString("HH:mm:ss");
    }

    // Sample of https://github.com/anthropics/anthropic-cookbook/blob/main/function_calling/function_calling.ipynb

    /// <summary>
    /// Calculator function for doing basic arithmetic. 
    /// Supports addition, subtraction, multiplication
    /// </summary>
    /// <param name="firstOperand">First operand (before the operator)</param>
    /// <param name="secondOperand">Second operand (after the operator)</param>
    /// <param name="operator">The operation to perform. Must be either +, -, *, or /</param>
    [ClaudiaFunction]
    static double DoPairwiseArithmetic(double firstOperand, double secondOperand, string @operator)
    {
        return @operator switch
        {
            "+" => firstOperand + secondOperand,
            "-" => firstOperand - secondOperand,
            "*" => firstOperand * secondOperand,
            "/" => firstOperand / secondOperand,
            _ => throw new ArgumentException("Operation not supported")
        };
    }

    /// <summary>
    /// Retrieves the HTML from the specified URL.
    /// </summary>
    /// <param name="url">The URL to retrieve the HTML from.</param>
    [ClaudiaFunction]
    static async Task<string> GetHtmlFromWeb(string url)
    {
        using var client = new HttpClient();
        return await client.GetStringAsync(url);
    }

    /// <summary>
    /// Sum of two parameters.
    /// </summary>
    /// <param name="x">x.</param>
    /// <param name="y">y.</param>
    [ClaudiaFunction]
    static int Sum(int x, int y = 100)
    {
        return x + y;
    }

    /// <summary>
    /// Choose which fruits
    /// </summary>
    /// <param name="basket">Fruits basket.</param>
    /// <param name="more">Fruits basket2.</param>
    [ClaudiaFunction]
    static string ChooseFruit(Fruits basket, Fruits more = Fruits.Grape)
    {
        return basket.ToString();
    }
}


public enum Fruits
{
    Orange, Grape
}

