using System.Text.Json;
using System.Text.RegularExpressions;
using Mezon.Net.Internal.Realtime;

namespace WebApp.Integration.Mezon.Utils;

public static class MezonBotParser
{
    private static readonly Regex JoinCommandRegex = new(
        @"^/join\s+([a-zA-Z0-9]{4,16})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex ExitCommandRegex = new(
        @"^/exit$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex LeaderboardCommandRegex = new(
        @"^/leaderboard$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex QuizButtonRegex = new(
        @"^quiz:([0-9a-fA-F\-]{36}):q:(\d+):a:(\d+)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex QuizSubmitButtonRegex = new(
        @"^quiz:([0-9a-fA-F\-]{36}):q:(\d+):submit$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static bool TryParseJoinCode(string input, out string code)
    {
        code = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = JoinCommandRegex.Match(input.Trim());
        if (!match.Success)
        {
            return false;
        }

        code = match.Groups[1].Value.Trim().ToUpperInvariant();
        return code.Length > 0;
    }

    public static bool IsExitCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return ExitCommandRegex.IsMatch(input.Trim());
    }

    public static bool IsLeaderboardCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        return LeaderboardCommandRegex.IsMatch(input.Trim());
    }

    public static bool TryParseQuizButtonId(string input, out Guid sessionId, out int questionIndex, out int selectedOption)
    {
        sessionId = Guid.Empty;
        questionIndex = -1;
        selectedOption = -1;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = QuizButtonRegex.Match(input.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!Guid.TryParse(match.Groups[1].Value, out sessionId))
        {
            return false;
        }

        if (!int.TryParse(match.Groups[2].Value, out questionIndex) || questionIndex < 0)
        {
            return false;
        }

        if (!int.TryParse(match.Groups[3].Value, out selectedOption) || selectedOption < 0)
        {
            return false;
        }

        return true;
    }

    public static bool TryParseQuizSubmitButtonId(string input, out Guid sessionId, out int questionIndex)
    {
        sessionId = Guid.Empty;
        questionIndex = -1;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var match = QuizSubmitButtonRegex.Match(input.Trim());
        if (!match.Success)
        {
            return false;
        }

        if (!Guid.TryParse(match.Groups[1].Value, out sessionId))
        {
            return false;
        }

        if (!int.TryParse(match.Groups[2].Value, out questionIndex) || questionIndex < 0)
        {
            return false;
        }

        return true;
    }

    public static string ExtractButtonId(MessageButtonClicked clickEvent)
    {
        var buttonId = (clickEvent.ButtonId ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(buttonId))
        {
            return buttonId;
        }

        var extraData = (clickEvent.ExtraData ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(extraData))
        {
            return string.Empty;
        }

        try
        {
            using var doc = JsonDocument.Parse(extraData);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var candidate = TryGetStringProperty(doc.RootElement, "button_id")
                    ?? TryGetStringProperty(doc.RootElement, "buttonId")
                    ?? TryGetStringProperty(doc.RootElement, "id")
                    ?? TryGetStringProperty(doc.RootElement, "component_id")
                    ?? TryGetStringProperty(doc.RootElement, "componentId");

                return candidate?.Trim() ?? string.Empty;
            }

            if (doc.RootElement.ValueKind == JsonValueKind.String)
            {
                return (doc.RootElement.GetString() ?? string.Empty).Trim();
            }
        }
        catch (JsonException)
        {
            // Keep fallback behavior below for non-JSON extra_data.
        }

        return extraData;
    }

    public static string? TryGetStringProperty(JsonElement obj, string propertyName)
    {
        if (!obj.TryGetProperty(propertyName, out var node))
        {
            return null;
        }

        return node.ValueKind switch
        {
            JsonValueKind.String => node.GetString(),
            JsonValueKind.Number => node.ToString(),
            _ => null
        };
    }

    public static string ExtractMessageText(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return string.Empty;
        }

        var trimmed = rawContent.Trim();

        try
        {
            using var document = JsonDocument.Parse(trimmed);

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (document.RootElement.TryGetProperty("t", out var tNode) && tNode.ValueKind == JsonValueKind.String)
                {
                    return tNode.GetString() ?? string.Empty;
                }

                if (document.RootElement.TryGetProperty("text", out var textNode) && textNode.ValueKind == JsonValueKind.String)
                {
                    return textNode.GetString() ?? string.Empty;
                }
            }

            if (document.RootElement.ValueKind == JsonValueKind.String)
            {
                return document.RootElement.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
        }

        return trimmed;
    }
}
