using Claudia;
using R3;

var anthropic = new Anthropic();


anthropic.HttpClient.BaseAddress = new Uri("http://myproxy/v25");

var txt = await anthropic.Messages.CreateAsync(new()
{
    MaxTokens = 1024,
    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
    Model = "claude-3-opus-20240229"
});

Console.WriteLine(txt);




//// convert to array.
//var array = await stream.ToObservable().ToArrayAsync();

//// filterling and execute.
//await stream.ToObservable()
//    .OfType<IMessageStreamEvent, ContentBlockDelta>()
//    .Where(x => x.Delta.Text != null)
//    .ForEachAsync(x =>
//    {
//        Console.WriteLine(x.Delta.Text);
//    });

//// branching query
//var branch = stream.ToObservable().Publish();

//var messageStartTask = branch.OfType<IMessageStreamEvent, MessageStart>().FirstAsync();
//var messageDeltaTask = branch.OfType<IMessageStreamEvent, MessageDelta>().FirstAsync();

//branch.Connect(); // start consume stream

//Console.WriteLine((await messageStartTask));
//Console.WriteLine((await messageDeltaTask));







//Console.WriteLine("---");

//Console.WriteLine(sb.ToString());

// Counting Tokens
//var anthropic = new Anthropic();

//var msg = await anthropic.Messages.CreateAsync(new()
//{
//    Model = Models.Claude3Opus,
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude." }]
//});

//// Usage { InputTokens = 11, OutputTokens = 18 }
//Console.WriteLine(msg.Usage);



//Messages = [new() { Role = "user", Content = "Hello, Claude. Responses, please break line after each word." }]


//// error

//try
//{
//    var msg = await anthropic.Messages.CreateAsync(new()
//    {
//        Model = Models.Claude3Opus,
//        MaxTokens = 1024,
//        Messages = [new() { Role = "user", Content = "Hello, Claude" }]
//    });
//}
//catch (ClaudiaException ex)
//{
//    Console.WriteLine((int)ex.Status); // 400(ErrorCode.InvalidRequestError)
//    Console.WriteLine(ex.Name);        // invalid_request_error
//    Console.WriteLine(ex.Message);     // Field required. Input:...
//}

// retry

// Configure the default for all requests:
//var anthropic = new Anthropic
//{
//    MaxRetries = 0, // default is 2
//};

//// Or, configure per-request:
//await anthropic.Messages.CreateAsync(new()
//{
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
//    Model = "claude-3-opus-20240229"
//}, new()
//{
//    MaxRetries = 5
//});

// timeout

//// Configure the default for all requests:
//var anthropic = new Anthropic
//{
//    Timeout = TimeSpan.FromSeconds(20) // 20 seconds (default is 10 minutes)
//};

//// Override per-request:
//await anthropic.Messages.CreateAsync(new()
//{
//    MaxTokens = 1024,
//    Messages = [new() { Role = "user", Content = "Hello, Claude" }],
//    Model = "claude-3-opus-20240229"
//}, new()
//{
//    Timeout = TimeSpan.FromSeconds(5)
//});