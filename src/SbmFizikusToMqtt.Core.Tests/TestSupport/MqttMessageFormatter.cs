using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using SbmFizikusToMqtt.Core.Ports;

namespace SbmFizikusToMqtt.Core.Tests.TestSupport;

/// <summary>
///     Renders published messages as a readable snapshot: one header per message with its delivery flags, followed
///     by the pretty-printed JSON payload, so wire-format changes are easy to review in a diff.
/// </summary>
internal static class MqttMessageFormatter
{
    private static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Format(IEnumerable<MqttMessage> messages)
    {
        var builder = new StringBuilder();
        foreach (var message in messages)
        {
            builder.Append("=== ").Append(message.Topic)
                .Append(" (retain: ").Append(message.Retain ? "yes" : "no")
                .Append(", qos: ").Append(message.Qos).AppendLine(")");
            builder.AppendLine(FormatPayload(message.Payload)).AppendLine();
        }

        return builder.ToString();
    }

    private static string FormatPayload(string payload)
    {
        if (payload.Length == 0)
            return "<empty>";

        try
        {
            return JsonNode.Parse(payload)?.ToJsonString(PrettyJson) ?? "null";
        }
        catch (JsonException)
        {
            return payload;
        }
    }
}