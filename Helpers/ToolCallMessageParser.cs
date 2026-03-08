using System;

namespace OpenMeido.Helpers
{
    internal sealed class ToolCallMessageData
    {
        public string ToolName { get; init; } = "";
        public string Parameters { get; init; } = "";
        public string Result { get; init; } = "";
        public bool IsSuccess { get; init; }
    }

    internal static class ToolCallMessageParser
    {
        public static ToolCallMessageData Parse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return null;
            }

            string toolName = "";
            string parameters = "";
            string result = "";
            bool isSuccess = false;

            foreach (var line in message.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith(ToolCallMessageMarkers.ToolCallStart))
                {
                    toolName = trimmedLine.Substring(ToolCallMessageMarkers.ToolCallStart.Length).Trim();
                }
                else if (trimmedLine.StartsWith(ToolCallMessageMarkers.ToolParams))
                {
                    parameters = trimmedLine.Substring(ToolCallMessageMarkers.ToolParams.Length).Trim();
                }
                else if (trimmedLine.StartsWith(ToolCallMessageMarkers.ToolResultSuccess))
                {
                    result = trimmedLine.Substring(ToolCallMessageMarkers.ToolResultSuccess.Length).Trim();
                    isSuccess = true;
                }
                else if (trimmedLine.StartsWith(ToolCallMessageMarkers.ToolResultFailed))
                {
                    result = trimmedLine.Substring(ToolCallMessageMarkers.ToolResultFailed.Length).Trim();
                    isSuccess = false;
                }
            }

            return string.IsNullOrEmpty(toolName)
                ? null
                : new ToolCallMessageData
                {
                    ToolName = toolName,
                    Parameters = parameters,
                    Result = result,
                    IsSuccess = isSuccess
                };
        }
    }
}