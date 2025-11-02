using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that persists across scene transitions.
/// Maintains conversation context, routing information, and mission visit history.
/// Ensures specialists and Mission Control remember what was discussed.
/// </summary>
public class MissionContext : MonoBehaviour
{
    public static MissionContext Instance { get; private set; }

    [Header("Current State")]
    [Tooltip("Current location: Hub, ISS, Voyager, or Hubble")]
    public string currentLocation = "Hub";

    [Tooltip("Why was user routed to current location?")]
    public string routingReason = "";

    [Header("Conversation Tracking")]
    [Tooltip("Recent conversation exchanges (last 10)")]
    public List<ConversationExchange> recentHistory = new List<ConversationExchange>();

    [Tooltip("Missions user has visited this session")]
    public HashSet<string> visitedMissions = new HashSet<string>();

    private const int MAX_HISTORY_SIZE = 10;

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[MissionContext] ╔══════════════════════════════════════════════════════════");
            Debug.Log($"[MissionContext] ║ INITIALIZED - Will persist across scenes");
            Debug.Log($"[MissionContext] ║ Current Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Debug.Log($"[MissionContext] ║ Initial currentLocation: '{currentLocation}'");
            Debug.Log($"[MissionContext] ║ Initial routingReason: '{routingReason}'");
            Debug.Log($"[MissionContext] ╚══════════════════════════════════════════════════════════");
        }
        else
        {
            Debug.Log($"[MissionContext] Instance already exists (currentLocation: '{Instance.currentLocation}'), destroying duplicate from {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set context when routing to a new location
    /// </summary>
    public void SetRoutingContext(string destination, string reason)
    {
        string previousLocation = currentLocation;
        currentLocation = destination;
        routingReason = reason;

        if (destination != "Hub")
        {
            visitedMissions.Add(destination);
        }

        Debug.Log($"[MissionContext] ╔══════════════════════════════════════════════════════════");
        Debug.Log($"[MissionContext] ║ ROUTING CONTEXT UPDATED");
        Debug.Log($"[MissionContext] ║ Previous Location: '{previousLocation}'");
        Debug.Log($"[MissionContext] ║ New Location: '{currentLocation}'");
        Debug.Log($"[MissionContext] ║ Routing Reason: '{routingReason}'");
        Debug.Log($"[MissionContext] ╚══════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Add a conversation exchange to history
    /// </summary>
    public void AddConversationExchange(string userMessage, string agentResponse, string toolExecuted = null, string location = null)
    {
        if (location == null)
        {
            location = currentLocation;
        }

        var exchange = new ConversationExchange
        {
            userMessage = userMessage,
            agentResponse = agentResponse,
            toolExecuted = toolExecuted,
            location = location,
            timestamp = System.DateTime.Now
        };

        recentHistory.Add(exchange);

        // Trim history to max size
        if (recentHistory.Count > MAX_HISTORY_SIZE)
        {
            recentHistory.RemoveAt(0);
        }

        Debug.Log($"[MissionContext] Added exchange in {location}" + (toolExecuted != null ? $" (tool: {toolExecuted})" : ""));
    }

    /// <summary>
    /// Get formatted context for specialist introduction
    /// Includes routing reason and recent conversation summary
    /// </summary>
    public string GetContextForSpecialist()
    {
        string context = $"Mission Control routed you here because: {routingReason}";

        // Add recent conversation summary (last 3 exchanges)
        if (recentHistory.Count > 0)
        {
            context += "\n\nRecent conversation:\n";
            int startIndex = Mathf.Max(0, recentHistory.Count - 3);
            for (int i = startIndex; i < recentHistory.Count; i++)
            {
                var exchange = recentHistory[i];
                context += $"[{exchange.location}] User: {exchange.userMessage}\n";
                context += $"[{exchange.location}] Agent: {TruncateResponse(exchange.agentResponse, 100)}\n";
            }
        }

        return context;
    }

    /// <summary>
    /// Get context for Mission Control when user returns from a mission
    /// </summary>
    public string GetReturnContext()
    {
        if (visitedMissions.Count == 0)
        {
            return "";
        }

        string lastVisited = currentLocation != "Hub" ? currentLocation : "a mission";
        return $"User just returned from {lastVisited} Mission Space.";
    }

    /// <summary>
    /// Get detailed context for Mission Control when user returns from a mission
    /// Includes routing reason and recent conversation summary
    /// </summary>
    public string GetContextForMissionControl()
    {
        System.Text.StringBuilder context = new System.Text.StringBuilder();

        // Add routing context
        if (!string.IsNullOrEmpty(routingReason))
        {
            context.AppendLine($"User just returned: {routingReason}");
        }

        // Add recent conversation summary
        if (recentHistory.Count > 0)
        {
            context.AppendLine("\nRecent conversation:");

            // Get last 2-3 exchanges
            int startIndex = Mathf.Max(0, recentHistory.Count - 3);
            for (int i = startIndex; i < recentHistory.Count; i++)
            {
                var exchange = recentHistory[i];
                string location = !string.IsNullOrEmpty(exchange.location) ? $"[{exchange.location}]" : "";
                context.AppendLine($"{location} User: {exchange.userMessage}");

                // Truncate long responses
                string response = exchange.agentResponse;
                if (response.Length > 100)
                {
                    response = response.Substring(0, 100) + "...";
                }
                context.AppendLine($"{location} Agent: {response}");
            }
        }

        return context.ToString();
    }

    /// <summary>
    /// Check if user has visited a specific mission
    /// </summary>
    public bool HasVisited(string missionName)
    {
        return visitedMissions.Contains(missionName);
    }

    /// <summary>
    /// Get list of all visited missions
    /// </summary>
    public List<string> GetVisitedMissions()
    {
        return new List<string>(visitedMissions);
    }

    /// <summary>
    /// Get formatted conversation history for use in prompts
    /// </summary>
    public string GetFormattedHistory(int lastNExchanges = 5)
    {
        if (recentHistory.Count == 0)
            return "No previous conversation.";

        int startIndex = Mathf.Max(0, recentHistory.Count - lastNExchanges);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Recent conversation history:");

        for (int i = startIndex; i < recentHistory.Count; i++)
        {
            var exchange = recentHistory[i];
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
    /// Get condensed context summary for efficient prompt injection
    /// </summary>
    public string GetContextSummary(int lastNExchanges = 3)
    {
        if (recentHistory.Count == 0)
            return "";

        int startIndex = Mathf.Max(0, recentHistory.Count - lastNExchanges);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("Conversation context:");

        for (int i = startIndex; i < recentHistory.Count; i++)
        {
            var exchange = recentHistory[i];
            string truncatedMsg = TruncateResponse(exchange.userMessage, 80);
            sb.AppendLine($"User asked: \"{truncatedMsg}\"");
            if (!string.IsNullOrEmpty(exchange.toolExecuted))
            {
                sb.AppendLine($"→ Executed: {exchange.toolExecuted}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Get the count of conversation exchanges
    /// </summary>
    public int GetExchangeCount()
    {
        return recentHistory.Count;
    }

    /// <summary>
    /// Get the last user message
    /// </summary>
    public string GetLastUserMessage()
    {
        if (recentHistory.Count == 0)
            return null;

        return recentHistory[recentHistory.Count - 1].userMessage;
    }

    /// <summary>
    /// Clear all context (for testing or reset)
    /// </summary>
    public void ClearContext()
    {
        currentLocation = "Hub";
        routingReason = "";
        recentHistory.Clear();
        visitedMissions.Clear();
        Debug.Log("[MissionContext] Context cleared");
    }

    private string TruncateResponse(string response, int maxLength)
    {
        if (response.Length <= maxLength)
        {
            return response;
        }
        return response.Substring(0, maxLength) + "...";
    }
}

/// <summary>
/// Represents a single conversational exchange
/// </summary>
[System.Serializable]
public class ConversationExchange
{
    public string userMessage;
    public string agentResponse;
    public string toolExecuted; // Tool executed during this exchange (null if none)
    public string location; // Where this exchange happened (Hub, ISS, etc.)
    public System.DateTime timestamp;
}
