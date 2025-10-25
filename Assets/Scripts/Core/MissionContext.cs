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
    [Tooltip("Current location: Hub, ISS, GPS, Voyager, or Hubble")]
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
            Debug.Log("[MissionContext] Initialized - will persist across scenes");
        }
        else
        {
            Debug.Log("[MissionContext] Instance already exists, destroying duplicate");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set context when routing to a new location
    /// </summary>
    public void SetRoutingContext(string destination, string reason)
    {
        currentLocation = destination;
        routingReason = reason;

        if (destination != "Hub")
        {
            visitedMissions.Add(destination);
        }

        Debug.Log($"[MissionContext] Routing to {destination}. Reason: {reason}");
    }

    /// <summary>
    /// Add a conversation exchange to history
    /// </summary>
    public void AddConversationExchange(string userMessage, string agentResponse, string location = null)
    {
        if (location == null)
        {
            location = currentLocation;
        }

        var exchange = new ConversationExchange
        {
            userMessage = userMessage,
            agentResponse = agentResponse,
            location = location,
            timestamp = System.DateTime.Now
        };

        recentHistory.Add(exchange);

        // Trim history to max size
        if (recentHistory.Count > MAX_HISTORY_SIZE)
        {
            recentHistory.RemoveAt(0);
        }

        Debug.Log($"[MissionContext] Added exchange in {location}");
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
    public string location; // Where this exchange happened (Hub, ISS, etc.)
    public System.DateTime timestamp;
}
