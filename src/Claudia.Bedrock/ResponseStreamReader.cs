using Amazon.BedrockRuntime.Model;
using System.Text.Json;
using System.Threading.Channels;

namespace Claudia;

internal static class ResponseStreamReader
{
    public static IAsyncEnumerable<IMessageStreamEvent> ToAsyncEnumerable(ResponseStream stream, CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<IMessageStreamEvent>(new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = true });

        stream.EventReceived += (_, e) =>
        {
            if (e.EventStreamEvent is PayloadPart p)
            {
                var ms = p.Bytes;
                IMessageStreamEvent? response = null;

                ms.Position = 9;
                var c = (char)ms.ReadByte();
                ms.Position = 0;

                if (c == 'c') // content_block_start/delta/stop
                {
                    ms.Position = 25;
                    switch (ms.ReadByte())
                    {
                        case (byte)'a': // st[a]rt
                            ms.Position = 0;
                            response = JsonSerializer.Deserialize<ContentBlockStart>(ms, AnthropicJsonSerializerContext.Default.Options)!;
                            break;
                        case (byte)'o': // st[o]p
                            ms.Position = 0;
                            response = JsonSerializer.Deserialize<ContentBlockStop>(ms, AnthropicJsonSerializerContext.Default.Options)!;
                            break;
                        case (byte)'l': // de[l]ta
                            ms.Position = 0;
                            response = JsonSerializer.Deserialize<ContentBlockDelta>(ms, AnthropicJsonSerializerContext.Default.Options)!;
                            break;
                        default:
                            break;
                    }
                    ms.Position = 0;
                }
                else if (c == 'm') // message_start/delta/stop
                {
                    ms.Position = 19;
                    switch (ms.ReadByte())
                    {
                        case (byte)'a': // st[a]rt
                            ms.Position = 0;
                            response = JsonSerializer.Deserialize<MessageStart>(ms, AnthropicJsonSerializerContext.Default.Options)!;
                            break;
                        case (byte)'o': // st[o]p
                            ms.Position = 0;
                            response = JsonSerializer.Deserialize<MessageStop>(ms, AnthropicJsonSerializerContext.Default.Options)!;
                            break;
                        case (byte)'l': // de[l]ta
                            ms.Position = 0;
                            response = JsonSerializer.Deserialize<MessageDelta>(ms, AnthropicJsonSerializerContext.Default.Options)!;
                            break;
                        default:
                            break;
                    }
                }
                else if (c == 'p') // ping
                {
                    response = JsonSerializer.Deserialize<Ping>(ms, AnthropicJsonSerializerContext.Default.Options)!;
                }
                else if (c == 'e') // error
                {
                    var error = JsonSerializer.Deserialize<ErrorResponseShape>(ms, AnthropicJsonSerializerContext.Default.Options);
                    var err = new ClaudiaException(error!.ErrorResponse.ToErrorCode(), error.ErrorResponse.Type, error.ErrorResponse.Message);
                    channel.Writer.Complete(err);
                }

                if (response != null)
                {
                    channel.Writer.TryWrite(response);
                }

                if (response is MessageStop)
                {
                    channel.Writer.TryComplete();
                }
            }
        };

        stream.ExceptionReceived += (_, e) =>
        {
            channel.Writer.Complete(e.EventStreamException);
        };

        stream.StartProcessing();
        return channel.Reader.ReadAllAsync(cancellationToken);
    }
}
