using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenMeido.Helpers
{
    internal static class AiMessageDisplayHelper
    {
        public static readonly string SentenceSeparator = new string('\\', 3);

        public static bool ContainsSentenceSeparator(string message)
        {
            return !string.IsNullOrEmpty(message) && message.Contains(SentenceSeparator);
        }

        public static List<string> SplitMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return new List<string>();
            }

            return message.Split(new[] { SentenceSeparator }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public static int CalculateDelay(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                return 500;
            }

            int length = sentence.Trim().Length;
            int baseDelay = 800;
            int extraDelay;

            if (length <= 20)
            {
                extraDelay = length * 20;
            }
            else if (length <= 50)
            {
                extraDelay = 400 + (length - 20) * 25;
            }
            else
            {
                extraDelay = 1150 + (length - 50) * 30;
            }

            int totalDelay = baseDelay + extraDelay;
            return Math.Max(800, Math.Min(3500, totalDelay));
        }
    }
}
