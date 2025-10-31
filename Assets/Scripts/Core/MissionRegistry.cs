using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton registry for all available missions.
/// Provides centralized access to mission configurations and enables cross-mission awareness.
/// Specialists can query this to recommend related missions to users.
/// </summary>
public class MissionRegistry : MonoBehaviour
{
    public static MissionRegistry Instance { get; private set; }

    [Header("Mission Database")]
    [Tooltip("All available mission configurations")]
    public List<MissionConfig> missions = new List<MissionConfig>();

    [Header("Default Settings")]
    [Tooltip("Default mission to load if none specified")]
    public string defaultMissionId = "ISS";

    private Dictionary<string, MissionConfig> _missionLookup;

    void Awake()
    {
        // Singleton pattern with DontDestroyOnLoad
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeRegistry();
        }
        else
        {
            Debug.Log("[MissionRegistry] Instance already exists, destroying duplicate");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Initialize mission lookup dictionary
    /// </summary>
    private void InitializeRegistry()
    {
        _missionLookup = new Dictionary<string, MissionConfig>();

        foreach (var mission in missions)
        {
            if (mission != null)
            {
                _missionLookup[mission.missionId] = mission;
                Debug.Log($"[MissionRegistry] Registered mission: {mission.missionId} ({mission.missionName})");
            }
        }

        Debug.Log($"[MissionRegistry] Initialized with {_missionLookup.Count} missions");
    }

    /// <summary>
    /// Get mission configuration by ID
    /// </summary>
    public MissionConfig GetMission(string missionId)
    {
        if (_missionLookup == null)
        {
            InitializeRegistry();
        }

        if (_missionLookup.TryGetValue(missionId, out MissionConfig config))
        {
            return config;
        }

        Debug.LogWarning($"[MissionRegistry] Mission '{missionId}' not found");
        return null;
    }

    /// <summary>
    /// Get all missions in a specific category
    /// </summary>
    public List<MissionConfig> GetMissionsByCategory(MissionCategory category)
    {
        if (_missionLookup == null)
        {
            InitializeRegistry();
        }

        return _missionLookup.Values.Where(m => m.category == category).ToList();
    }

    /// <summary>
    /// Get list of all mission IDs
    /// </summary>
    public List<string> GetAllMissionIds()
    {
        if (_missionLookup == null)
        {
            InitializeRegistry();
        }

        return new List<string>(_missionLookup.Keys);
    }

    /// <summary>
    /// Get list of all mission names (for display)
    /// </summary>
    public List<string> GetAllMissionNames()
    {
        if (_missionLookup == null)
        {
            InitializeRegistry();
        }

        return _missionLookup.Values.Select(m => m.missionName).ToList();
    }

    /// <summary>
    /// Check if mission exists
    /// </summary>
    public bool HasMission(string missionId)
    {
        if (_missionLookup == null)
        {
            InitializeRegistry();
        }

        return _missionLookup.ContainsKey(missionId);
    }

    /// <summary>
    /// Get missions related to a given mission
    /// </summary>
    public List<MissionConfig> GetRelatedMissions(string missionId)
    {
        MissionConfig mission = GetMission(missionId);
        if (mission == null || mission.relatedMissions == null || mission.relatedMissions.Length == 0)
        {
            return new List<MissionConfig>();
        }

        List<MissionConfig> related = new List<MissionConfig>();
        foreach (string relatedId in mission.relatedMissions)
        {
            MissionConfig relatedMission = GetMission(relatedId);
            if (relatedMission != null)
            {
                related.Add(relatedMission);
            }
        }

        return related;
    }

    /// <summary>
    /// Get formatted string of available missions for specialist context
    /// </summary>
    public string GetAvailableMissionsString()
    {
        if (_missionLookup == null)
        {
            InitializeRegistry();
        }

        if (_missionLookup.Count == 0)
        {
            return "No missions available.";
        }

        var missionList = _missionLookup.Values
            .Select(m => $"{m.missionId} ({m.category})")
            .ToList();

        return $"Available missions: {string.Join(", ", missionList)}";
    }

    /// <summary>
    /// Get default mission configuration
    /// </summary>
    public MissionConfig GetDefaultMission()
    {
        return GetMission(defaultMissionId);
    }
}
