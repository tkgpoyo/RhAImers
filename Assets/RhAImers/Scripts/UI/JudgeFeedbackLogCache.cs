using System;
using UnityEngine;

namespace RhAImers.UI
{
    public static class JudgeFeedbackLogCache
    {
        private const string FeedbackLogPrefix = "[GameManager] Judge Feedback:";

        public static string LastFeedback { get; private set; } = string.Empty;

        public static bool HasFeedback => !string.IsNullOrWhiteSpace(LastFeedback);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            LastFeedback = string.Empty;
            Application.logMessageReceived -= HandleLogMessage;
            Application.logMessageReceived += HandleLogMessage;
        }

        public static bool TryGet(out string feedback)
        {
            feedback = LastFeedback;
            return !string.IsNullOrWhiteSpace(feedback);
        }

        public static void Clear()
        {
            LastFeedback = string.Empty;
        }

        private static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return;
            }

            int prefixIndex = condition.IndexOf(FeedbackLogPrefix, StringComparison.Ordinal);
            if (prefixIndex < 0)
            {
                return;
            }

            string feedback = condition.Substring(prefixIndex + FeedbackLogPrefix.Length).Trim();
            if (!string.IsNullOrWhiteSpace(feedback))
            {
                LastFeedback = feedback;
            }
        }
    }
}
