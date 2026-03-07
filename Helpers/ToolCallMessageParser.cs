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
        private const string ToolCallStartPrefix = "TOOL_CALL_START:";
        private const string ToolParamsPrefix = "TOOL_PARAMS:";
        private const string ToolResultSuccessPrefix = "TOOL_RESULT_SUCCESS:";
        private const string ToolResultFailedPrefix = "TOOL_RESULT_FAILED:";

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
                if (trimmedLine.StartsWith(ToolCallStartPrefix))
                {
                    toolName = trimmedLine.Substring(ToolCallStartPrefix.Length).Trim();
                }
                else if (trimmedLine.StartsWith(ToolParamsPrefix))
                {
                    parameters = trimmedLine.Substring(ToolParamsPrefix.Length).Trim();
                }
                else if (trimmedLine.StartsWith(ToolResultSuccessPrefix))
                {
                    result = trimmedLine.Substring(ToolResultSuccessPrefix.Length).Trim();
                    isSuccess = true;
                }
                else if (trimmedLine.StartsWith(ToolResultFailedPrefix))
                {
                    result = trimmedLine.Substring(ToolResultFailedPrefix.Length).Trim();
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