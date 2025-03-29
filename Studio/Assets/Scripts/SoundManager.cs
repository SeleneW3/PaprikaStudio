using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    public static SoundManager Instance { get { return instance; } }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;  // 用于播放背景音乐
    [SerializeField] private AudioSource sfxSource;    // 用于播放音效

    [Header("Audio Clips")]
    [SerializeField] private Sound[] musicClips;  // 背景音乐列表
    [SerializeField] private Sound[] sfxClips;    // 音效列表

    [Header("Settings")]
    [SerializeField] private float musicVolume = 1f;
    [SerializeField] private float sfxVolume = 1f;
    
    private Dictionary<string, AudioClip> musicDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // 单例模式
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化音频源
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
        }
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        // 初始化音频字典
        foreach (Sound s in musicClips)
        {
            if (s.clip != null)
                musicDict[s.name] = s.clip;
        }

        foreach (Sound s in sfxClips)
        {
            if (s.clip != null)
                sfxDict[s.name] = s.clip;
        }
    }

    // 播放背景音乐
    public void PlayMusic(string name)
    {
        if (musicDict.ContainsKey(name))
        {
            musicSource.clip = musicDict[name];
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        else
        {
            Debug.LogWarning($"Music clip {name} not found!");
        }
    }

    // 播放音效
    public void PlaySFX(string name)
    {
        if (sfxDict.ContainsKey(name))
        {
            sfxSource.PlayOneShot(sfxDict[name], sfxVolume);
        }
        else
        {
            Debug.LogWarning($"SFX clip {name} not found!");
        }
    }

    // 停止背景音乐
    public void StopMusic()
    {
        musicSource.Stop();
    }

    // 暂停背景音乐
    public void PauseMusic()
    {
        musicSource.Pause();
    }

    // 继续播放背景音乐
    public void ResumeMusic()
    {
        musicSource.UnPause();
    }

    // 设置音乐音量
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
    }

    // 设置音效音量
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    // 淡入音乐
    public void FadeInMusic(string name, float duration)
    {
        if (musicDict.ContainsKey(name))
        {
            StartCoroutine(FadeInMusicCoroutine(name, duration));
        }
    }

    private System.Collections.IEnumerator FadeInMusicCoroutine(string name, float duration)
    {
        musicSource.clip = musicDict[name];
        musicSource.volume = 0;
        musicSource.Play();

        float timer = 0;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0, musicVolume, timer / duration);
            yield return null;
        }
    }

    // 淡出音乐
    public void FadeOutMusic(float duration)
    {
        StartCoroutine(FadeOutMusicCoroutine(duration));
    }

    private System.Collections.IEnumerator FadeOutMusicCoroutine(float duration)
    {
        float startVolume = musicSource.volume;
        float timer = 0;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, 0, timer / duration);
            yield return null;
        }

        musicSource.Stop();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
