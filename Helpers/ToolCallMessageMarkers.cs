namespace OpenMeido.Helpers
{
    internal static class ToolCallMessageMarkers
    {
        public const string ToolCallStart = "TOOL_CALL_START:";
        public const string ToolParams = "TOOL_PARAMS:";
        public const string ToolResultSuccess = "TOOL_RESULT_SUCCESS:";
        public const string ToolResultFailed = "TOOL_RESULT_FAILED:";
        public const string ToolCallEnd = "TOOL_CALL_END";

        public static bool ContainsAny(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            return message.Contains(ToolCallStart) ||
                   message.Contains(ToolParams) ||
                   message.Contains(ToolResultSuccess) ||
                   message.Contains(ToolResultFailed) ||
                   message.Contains(ToolCallEnd);
        }
    }
}
