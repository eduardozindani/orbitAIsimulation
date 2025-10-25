using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Agents.Core
{
    /// <summary>
    /// Manages conversation history between user and agent.
    /// Tracks recent exchanges to maintain context across multi-turn dialogues.
    /// </summary>
    [Serializable]
    public class ConversationHistory
    {
        [Tooltip("Maximum number of exchanges to remember")]
        public int maxHistorySize = 10;

        [Tooltip("Current location in the system (Hub or Mission Space)")]
        public string currentLocation = "Hub";

        // Internal storage
        private List<ConversationExchange> exchanges = new List<ConversationExchange>();

        /// <summary>
        /// Add a new exchange to the history
        /// </summary>
        public void AddExchange(string userMessage, string agentResponse, string toolExecuted = null)
        {
            var exchange = new ConversationExchange
            {
                timestamp = DateTime.Now,
                userMessage = userMessage,
                agentResponse = agentResponse,
                toolExecuted = toolExecuted,
                location = currentLocation
            };

            exchanges.Add(exchange);

            // Trim to max size (keep most recent)
            if (exchanges.Count > maxHistorySize)
            {
                exchanges.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get formatted history for inclusion in prompts
        /// </summary>
        public string GetFormattedHistory(int lastNExchanges = 5)
        {
            if (exchanges.Count == 0)
                return "No previous conversation.";

            var recentExchanges = exchanges
                .Skip(Math.Max(0, exchanges.Count - lastNExchanges))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Recent conversation history:");

            foreach (var exchange in recentExchanges)
            {
                sb.AppendLine($"[{exchange.timestamp:HH:mm:ss}] @ {exchange.location}");
                sb.AppendLine($"User: {exchange.userMessage}");
                if (!string.IsNullOrEmpty(exchange.toolExecuted))
                {
                    sb.AppendLine($"Tool executed: {exchange.toolExecuted}");
                }
                sb.AppendLine($"Agent: {exchange.agentResponse}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get condensed context summary for prompts (more efficient)
        /// </summary>
        public string GetContextSummary(int lastNExchanges = 3)
        {
            if (exchanges.Count == 0)
                return "";

            var recentExchanges = exchanges
                .Skip(Math.Max(0, exchanges.Count - lastNExchanges))
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Conversation context:");

            foreach (var exchange in recentExchanges)
            {
                sb.AppendLine($"User asked: \"{TruncateString(exchange.userMessage, 80)}\"");
                if (!string.IsNullOrEmpty(exchange.toolExecuted))
                {
                    sb.AppendLine($"→ Executed: {exchange.toolExecuted}");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get the last user message
        /// </summary>
        public string GetLastUserMessage()
        {
            if (exchanges.Count == 0)
                return null;

            return exchanges[exchanges.Count - 1].userMessage;
        }

        /// <summary>
        /// Get the last tool executed
        /// </summary>
        public string GetLastToolExecuted()
        {
            for (int i = exchanges.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrEmpty(exchanges[i].toolExecuted))
                    return exchanges[i].toolExecuted;
            }
            return null;
        }

        /// <summary>
        /// Check if user has been asking vague questions without creating orbits
        /// (Useful for detecting when to suggest Mission Spaces)
        /// </summary>
        public bool HasRecentVagueQuestions(int checkLastN = 3)
        {
            if (exchanges.Count < checkLastN)
                return false;

            var recent = exchanges
                .Skip(Math.Max(0, exchanges.Count - checkLastN))
                .ToList();

            // Count exchanges where no tool was executed
            int nonToolExchanges = recent.Count(e => string.IsNullOrEmpty(e.toolExecuted));

            // If most recent exchanges had no tool execution, might be vague questions
            return nonToolExchanges >= (checkLastN - 1);
        }

        /// <summary>
        /// Clear all history (for testing or reset)
        /// </summary>
        public void Clear()
        {
            exchanges.Clear();
            Debug.Log("[ConversationHistory] History cleared");
        }

        /// <summary>
        /// Set current location in the system
        /// </summary>
        public void SetLocation(string location)
        {
            currentLocation = location;
            Debug.Log($"[ConversationHistory] Location changed to: {location}");
        }

        /// <summary>
        /// Get total number of exchanges
        /// </summary>
        public int GetExchangeCount()
        {
            return exchanges.Count;
        }

        /// <summary>
        /// Get all exchanges (for debugging)
        /// </summary>
        public List<ConversationExchange> GetAllExchanges()
        {
            return new List<ConversationExchange>(exchanges);
        }

        // Helper method
        private string TruncateString(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }
    }

    /// <summary>
    /// Represents a single exchange in the conversation
    /// </summary>
    [Serializable]
    public class ConversationExchange
    {
        public DateTime timestamp;
        public string userMessage;
        public string agentResponse;
        public string toolExecuted; // null if no tool was executed
        public string location; // "Hub", "ISS_Space", etc.
    }
}
