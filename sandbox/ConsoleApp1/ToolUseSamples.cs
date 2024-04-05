using Claudia;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1;

public static partial class ToolUseSamples
{
    // https://github.com/anthropics/anthropic-cookbook/blob/main/tool_use/calculator_tool.ipynb
    public static async Task TryCalculateAsync()
    {
        var anthropic = new Anthropic();
        anthropic.HttpClient.DefaultRequestHeaders.Add("anthropic-beta", "tools-2024-04-04");

        await ChatWithClaude("What is the result of 1,984,135 * 9,343,116?");
        await ChatWithClaude("Calculate (12851 - 593) * 301 + 76");
        await ChatWithClaude("What is 15910385 divided by 193053?");

        async Task ChatWithClaude(string userMessage)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine($"User Message: {userMessage}");
            Console.WriteLine("==================================================");

            var message = await anthropic.Messages.CreateAsync(new()
            {
                Model = Models.Claude3Haiku,
                MaxTokens = 1024,
                Tools = [ToolUseSamples.Tools.Calculator],
                Messages = [new() { Role = Roles.User, Content = userMessage }]
            });

            Console.WriteLine("Initial Response:");
            Console.WriteLine($"Stop Reason: {message.StopReason}");
            Console.WriteLine($"Content: {message.Content}");

            var toolResult = await ToolUseSamples.InvokeToolAsync(message);

            var response = await anthropic.Messages.CreateAsync(new()
            {
                Model = Models.Claude3Haiku,
                MaxTokens = 1024,
                Tools = [ToolUseSamples.Tools.Calculator],
                Messages = [
                    new() { Role = Roles.User, Content = userMessage },
                    new() { Role = Roles.Assistant, Content = message.Content },
                    new() { Role = Roles.User, Content = toolResult! }
                ],
            });

            Console.WriteLine(response.Content.ToString());
        }
    }


    /// <summary>
    /// A simple calculator that performs basic arithmetic operations.
    /// </summary>
    /// <param name="expression">The mathematical expression to evaluate (e.g., '2 + 3 * 4').</param>
    [ClaudiaFunction]
    static double Calculator(string expression)
    {
        // cheap calculator, only calc 32bit.
        var dt = new DataTable();
        return Convert.ToDouble(dt.Compute(expression, ""));
    }

    // https://github.com/anthropics/anthropic-cookbook/blob/main/tool_use/customer_service_agent.ipynb



    ///// <summary>
    ///// Retrieves customer information based on their customer ID. Returns the customer's name, email, and phone number.
    ///// </summary>
    ///// <param name="customerId">The unique identifier for the customer.</param>
    //static string GetCustomerInfo(string customerId)
    //{
    //    // Simulated customer data
    //    var customers = new Dictionary<string, Customer>  {
    //        { "C1", new Customer(name: "John Doe", email: "john@example.com", phone: "123-456-7890") },
    //        { "C2", new Customer(name: "Jane Smith", email: "jane@example.com", phone: "987-654-3210") },
    //    };

    //    return customers.TryGetValue(customerId, out var customer) ? customer.ToString() : "Customer not found";
    //}


    ///// <summary>
    ///// Retrieves the details of a specific order based on the order ID. Returns the order ID, product name, quantity, price, and order status.
    ///// </summary>
    ///// <param name="orderId">Retrieves the details of a specific order based on the order ID. Returns the order ID, product name, quantity, price, and order status.</param>
    //static string GetOrderDetails(string orderId)
    //{
    //}

    ///// <summary>
    ///// The unique identifier for the order to be cancelled.
    ///// </summary>
    ///// <param name="orderId">Cancels an order based on the provided order ID. Returns a confirmation message if the cancellation is successful.</param>
    //static string CancelOrder(string orderId)
    //{
    //}

    //record class Customer(string name, string email, string phone);
}
