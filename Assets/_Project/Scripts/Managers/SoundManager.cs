using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance => s_Instance;
    private static SoundManager s_Instance;

    [Serializable]
    public class SoundButtonClipPair
    {
        public ButtonUIType m_type;
        public AudioClip m_audioClip;
    }

    public enum ButtonUIType
    {
        regular,
        cancel,
        confirm,
        buy
    }

    [Header("Clips")]
    [SerializeField] private List<SoundButtonClipPair> _buttonClipPairs = new();
    [SerializeField] private List<AudioClip> _hitClips;
    [SerializeField] private List<AudioClip> _dieClips;
    [SerializeField] private AudioClip _turnCubeClip;
    [SerializeField] private AudioClip _turnSnakeClip;
    [SerializeField] private List<AudioClip> _collectSimpleClips;
    [SerializeField] private AudioClip _collectBigClip;
    [SerializeField] private AudioClip _ouchClip;
    [SerializeField] private List<AudioClip> _snakeStepClips;

    [Header("Music")]
    [SerializeField] private AudioClip _menuTheme;
    [SerializeField] private AudioClip _gameTheme;

    [Header("Mixer")]
    [SerializeField] private AudioMixer _audioMixer;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource _soundEffectSource;
    [SerializeField] private AudioSource _musicSource1;
    [SerializeField] private AudioSource _musicSource2; 

    [Header("Other")]
    [SerializeField] private float _fadeTime = 1.0f;
    [SerializeField] private float _soundInterval = 0.01f;

    private AudioSource _currentMusicSource;
    private AudioSource _nextMusicSource;
    private bool _isCrossfading;
    private Coroutine _crossfadeRoutine;
    private float _soundPlayedTime;

    private void OnEnable()
    {
        if (s_Instance != null && s_Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_Instance = this;

        DontDestroyOnLoad(this.gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        _currentMusicSource = _musicSource1;
        _nextMusicSource = _musicSource2;

        _currentMusicSource.loop = true;
        _nextMusicSource.loop = true;

        // Set initial volume levels
        SetMusicVolume(SaveManager.Instance.MusicVolume);
        SetSoundEffectVolume(SaveManager.Instance.EffectsVolume);

        //PlayMusic(_gameTheme);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Set initial volume levels
        SetMusicVolume(SaveManager.Instance.MusicVolume);
        SetSoundEffectVolume(SaveManager.Instance.EffectsVolume);
    }

    #region General


    // Play a sound effect

    private void SetSoundPlayedTime(float time)
    {
        _soundPlayedTime = time;
    }

    private bool CheckInterval()
    {
        bool check = _soundPlayedTime + _soundInterval > Time.time;
        if (Time.deltaTime == 0)
            return false;
        return check;
    }

    public void PlaySoundEffect(AudioClip clip, bool urgent = false)
    {
        if ((clip == null || CheckInterval()) && !urgent)
        {
            return;
        }
        _soundEffectSource.PlayOneShot(clip);
        SetSoundPlayedTime(Time.time);
    }

    public void PlaySoundEffect(AudioClip clip, float volume)
    {
        if (clip == null || CheckInterval())
        {
            return;
        }
        _soundEffectSource.PlayOneShot(clip, volume);
        SetSoundPlayedTime(Time.time);
    }

    public void PauseAllSounds()
    {
        // Pause music
        _musicSource1.Pause();
        _musicSource2.Pause();

        // Pause sound effects
        if (_soundEffectSource.isPlaying)
        {
            _soundEffectSource.Pause();
        }
    }

    public void UnPauseAllSounds()
    {
        // Unpause music
        _musicSource1.UnPause();
        _musicSource2.UnPause();

        // Unpause sound effects
        if (_soundEffectSource.clip != null)
        {
            _soundEffectSource.UnPause();
        }
    }


    #region Music
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
            return;

        Crossfade(clip);
    }

    public void PlayGameMusic()
    {
        if (_currentMusicSource.clip == _gameTheme)
            return;
        PlayMusic(_gameTheme);
    }

    public void PlayMenuMusic()
    {
        if (_currentMusicSource.clip == _menuTheme)
            return;
        PlayMusic(_menuTheme);
    }

    public void PauseMusic()
    {
        _musicSource1.Pause();
        _musicSource2.Pause();
    }

    public void UnPauseMusic()
    {
        _musicSource1.UnPause();
        _musicSource2.UnPause();
    }

    private void Crossfade(AudioClip musicClip)
    {
        if (musicClip == null)
            return;

        // If already playing this clip and not crossfading, skip
        if (!_isCrossfading && _currentMusicSource.clip == musicClip && _currentMusicSource.isPlaying)
            return;

        // If nothing is playing yet, start immediately without crossfade
        if (!_currentMusicSource.isPlaying || _currentMusicSource.clip == null)
        {
            _currentMusicSource.clip = musicClip;
            _currentMusicSource.volume = 1f;
            _currentMusicSource.Play();
            return;
        }

        if (_crossfadeRoutine != null)
        {
            StopCoroutine(_crossfadeRoutine);
            _crossfadeRoutine = null;
        }
        _crossfadeRoutine = StartCoroutine(CrossfadeCoroutine(musicClip));
    }

    public void PlayGameTheme()
    {
        if (_currentMusicSource.clip == _gameTheme)
            return;
        PlayMusic(_gameTheme);
    }

    public void PlayMenuTheme()
    {
        if (_currentMusicSource.clip == _menuTheme)
            return;
        PlayMusic(_menuTheme);
    }

    private IEnumerator CrossfadeCoroutine(AudioClip musicClip)
    {
        _isCrossfading = true;

        // Ensure the next music source is playing
        _nextMusicSource.Stop();
        _nextMusicSource.clip = musicClip;
        _nextMusicSource.volume = 0f;
        _nextMusicSource.Play();

        // Fade out the current music source and fade in the next music source simultaneously
        float currentTime = 0.0f;
        float duration = Mathf.Max(0.0001f, _fadeTime);
        float startCurrentVol = _currentMusicSource.volume;
        const float targetVol = 1f;
        while (currentTime < duration)
        {
            currentTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(currentTime / duration);
            _currentMusicSource.volume = Mathf.Lerp(startCurrentVol, 0f, t);
            _nextMusicSource.volume = Mathf.Lerp(0f, targetVol, t);
            yield return null;
        }

        // Stop the current music source and set the volume back to its original value
        _currentMusicSource.Stop();
        _currentMusicSource.volume = targetVol;

        // Swap the current and next music sources
        AudioSource temp = _currentMusicSource;
        _currentMusicSource = _nextMusicSource;
        _nextMusicSource = temp;

        _isCrossfading = false;
        _crossfadeRoutine = null;
    }

    #endregion

    #region Set Volume

    // Set the volume of sound effects
    public void SetSoundEffectVolume(float volume)
    {
        float adjustedVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        _audioMixer.SetFloat("EffectsVolume", Mathf.Log10(adjustedVolume) * 20);
        SaveManager.Instance.EffectsVolume = volume;
    }

    // Set the volume of background music
    public void SetMusicVolume(float volume)
    {
        float adjustedVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        _audioMixer.SetFloat("MusicVolume", Mathf.Log10(adjustedVolume) * 20);
        SaveManager.Instance.MusicVolume = volume;
    }
    #endregion

    #endregion


    #region Play Sound Effects

    public void PlayButtonSound(ButtonUIType type)
    {
        AudioClip _buttonClip = _buttonClipPairs.Find(x => x.m_type == type).m_audioClip;
        PlaySoundEffect(_buttonClip);
        //Vibrate();
    }

    public void PlayDieSound()
    {
        AudioClip audioClip = _dieClips[UnityEngine.Random.Range(0, _dieClips.Count)];
        PlaySoundEffect(audioClip);
    }

    public void PlayHitSound()
    {
        AudioClip audioClip = _hitClips[UnityEngine.Random.Range(0, _hitClips.Count)];
        PlaySoundEffect(audioClip);

    }

    public void Vibrate(int durationMilis = 10)
    {
        if (SaveManager.Instance.Vibration == 0)
            return;

        long[] vibrationPattern = { 0, durationMilis };

        /*if (SaveManager.Instance.Vibration == 1)*/
        if (Application.platform == RuntimePlatform.Android)
        {

            // Get the current activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Get the vibrator service from the current activity
            AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");

            // Check if the vibrator service exists
            if (vibrator != null)
            {
                // Vibrate with the specified pattern
                vibrator.Call("vibrate", vibrationPattern, -1);
            }
            else
            {
                Debug.LogWarning("Vibrator service not found.");
            }
        }
        else
        {
            //Debug.LogWarning("Vibration only supported on Android.");
        }
    }

    #endregion

    #region New Gameplay Sounds

    public void PlayTurnCubeSound()
    {
        PlaySoundEffect(_turnCubeClip);
    }

    public void PlayTurnSnakeSound()
    {
        PlaySoundEffect(_turnSnakeClip);
    }

    public void PlayCollectSimpleSound()
    {
        if (_collectSimpleClips != null && _collectSimpleClips.Count > 0)
        {
            var clip = _collectSimpleClips[UnityEngine.Random.Range(0, _collectSimpleClips.Count)];
            PlaySoundEffect(clip);
        }
    }

    public void PlayCollectBigSound()
    {
        PlaySoundEffect(_collectBigClip);
    }

    public void PlayOuchSound()
    {
        PlaySoundEffect(_ouchClip);
    }

    #endregion

    public void PlaySnakeStepSound()
    {
        if (_snakeStepClips != null && _snakeStepClips.Count > 0)
        {
            var clip = _snakeStepClips[UnityEngine.Random.Range(0, _snakeStepClips.Count)];
            PlaySoundEffect(clip);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            PlayMusic(_gameTheme);
        }
        else
        {
            PlayMusic(_menuTheme);
        }
    }


}
