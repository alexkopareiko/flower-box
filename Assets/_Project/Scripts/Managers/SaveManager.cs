using UnityEngine;
using System.Collections.Generic;
#if UNITY_WEBGL
using Playgama;
#endif

public partial class SaveManager : MonoBehaviour
{
    // Leaderboards: change this to your configured ID in playgama-bridge-config.json
    public const string LeaderboardId = "best_score";
    public static SaveManager Instance => s_Instance;
    private static SaveManager s_Instance;

    const string k_SoundVolume = "SoundVolume";
    const string k_MusicVolume = "MusicVolume";
    const string k_Vibration = "Vibration";
    const string k_PostProcessing = "PostProcessing";
    const string k_Privacy = "Privacy";
    const string k_TutorialWatched = "TutorialWatched";
    const string k_RateUsClicked = "RateUsClicked";
    const string k_LosesCount = "LosesCount";
    const string k_MaxScore = "MaxScore";
    const string k_CurrentLevel = "CurrentLevel";

    private void OnEnable()
    {
        SetupInstance();
    }

    private void Awake()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitializeBridgeStorage();
#endif
    }

    private void SetupInstance()
    {
        gameObject.name = "SaveManager " + UnityEngine.Random.Range(0f, 1f);
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        s_Instance = this;
    }

    #region Reset Prefs

    public void Reset()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        var keys = new List<string>
        {
            k_SoundVolume,
            k_MusicVolume,
            k_Vibration,
            k_PostProcessing,
            k_Privacy,
            k_TutorialWatched,
            k_RateUsClicked,
            k_LosesCount,
            k_MaxScore,
            k_CurrentLevel
        };
        Bridge.storage.Delete(keys, null, null);
#endif
        PlayerPrefs.DeleteAll();
    }

    #endregion

    #region Sound Music Volume / Vibration / Post Processing

    public float EffectsVolume
    {
        get => GetFloat(k_SoundVolume, 1f);
        set => SetFloat(k_SoundVolume, value);
    }

    public float MusicVolume
    {
        get => GetFloat(k_MusicVolume, 1f);
        set => SetFloat(k_MusicVolume, value);
    }

    public int Vibration
    {
        get => GetInt(k_Vibration, 1);
        set => SetInt(k_Vibration, value);
    }

    public int PostProcessing
    {
        get => GetInt(k_PostProcessing, 1);
        set => SetInt(k_PostProcessing, value);
    }

    #endregion

    #region Privacy

    public bool Privacy
    {
        get => GetBool(k_Privacy, false);
        set => SetBool(k_Privacy, value);
    }

    #endregion

    #region TutorialWatched

    public bool TutorialWatched
    {
        get => GetBool(k_TutorialWatched, false);
        set => SetBool(k_TutorialWatched, value);
    }

    #endregion

    #region RateUsClicked

    public bool RateUsClicked
    {
        get => GetBool(k_RateUsClicked, false);
        set => SetBool(k_RateUsClicked, value);
    }

    #endregion

    #region LosesCount

    public int LosesCount
    {
        get => GetInt(k_LosesCount, 0);
        set => SetInt(k_LosesCount, value);
    }

    #endregion

    #region MaxScore

    public int MaxScore
    {
        get => GetInt(k_MaxScore, 0);
        set => SetInt(k_MaxScore, value);
    }
    #endregion

    #region CurrentLevel
    public static int CurrentLevel
    {
        get => Instance != null ? Instance.GetInt(k_CurrentLevel, 1) : PlayerPrefs.GetInt(k_CurrentLevel, 1);
        set { if (Instance != null) Instance.SetInt(k_CurrentLevel, value); else PlayerPrefs.SetInt(k_CurrentLevel, value); }
    }

    #endregion
}

#if UNITY_WEBGL && !UNITY_EDITOR
public partial class SaveManager
{
    private readonly Dictionary<string, string> _cache = new();

    private static readonly List<string> s_AllKeys = new()
    {
        k_SoundVolume,
        k_MusicVolume,
        k_Vibration,
        k_PostProcessing,
        k_Privacy,
        k_TutorialWatched,
        k_RateUsClicked,
        k_LosesCount,
        k_MaxScore,
        k_CurrentLevel
    };

    private void InitializeBridgeStorage()
    {
        // Load all keys once to keep getters synchronous
        Bridge.storage.Get(s_AllKeys, (success, values) =>
        {
            if (success && values != null)
            {
                for (int i = 0; i < s_AllKeys.Count && i < values.Count; i++)
                {
                    var key = s_AllKeys[i];
                    var val = values[i];
                    if (val != null)
                    {
                        _cache[key] = val;
                    }
                }
            }
        }, null);
    }

    private float GetFloat(string key, float def)
    {
        if (_cache.TryGetValue(key, out var str) && !string.IsNullOrEmpty(str))
        {
            if (float.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var f))
                return f;
        }
        return PlayerPrefs.GetFloat(key, def);
    }

    private void SetFloat(string key, float value)
    {
        var s = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _cache[key] = s;
        PlayerPrefs.SetFloat(key, value);
        Bridge.storage.Set(key, s, null, null);
    }

    private int GetInt(string key, int def)
    {
        if (_cache.TryGetValue(key, out var str) && !string.IsNullOrEmpty(str) && int.TryParse(str, out var i))
            return i;
        return PlayerPrefs.GetInt(key, def);
    }

    private void SetInt(string key, int value)
    {
        var s = value.ToString();
        _cache[key] = s;
        PlayerPrefs.SetInt(key, value);
        Bridge.storage.Set(key, s, null, null);
    }

    private bool GetBool(string key, bool def)
    {
        if (_cache.TryGetValue(key, out var str) && !string.IsNullOrEmpty(str) && bool.TryParse(str, out var b))
            return b;
        return PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;
    }

    private void SetBool(string key, bool value)
    {
        var s = value.ToString();
        _cache[key] = s;
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        Bridge.storage.Set(key, s, null, null);
    }
}
#else
public partial class SaveManager
{
    private float GetFloat(string key, float def) => PlayerPrefs.GetFloat(key, def);
    private void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
    private int GetInt(string key, int def) => PlayerPrefs.GetInt(key, def);
    private void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    private bool GetBool(string key, bool def) => PlayerPrefs.GetInt(key, def ? 1 : 0) == 1;
    private void SetBool(string key, bool value) => PlayerPrefs.SetInt(key, value ? 1 : 0);
}
#endif
