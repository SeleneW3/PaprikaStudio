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
        [Range(0f, 1f)]
        public float volume = 1f; // 每个音效的单独音量
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
    private Dictionary<string, float> sfxVolumeDict = new Dictionary<string, float>(); // 用于存储每个音效的单独音量

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
            {
                sfxDict[s.name] = s.clip;
                
                // 尝试从PlayerPrefs加载保存的音量，如果没有则使用默认值
                float savedVolume = PlayerPrefs.GetFloat($"SFXVolume_{s.name}", s.volume);
                sfxVolumeDict[s.name] = savedVolume;
            }
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
            //Debug.LogWarning($"Music clip {name} not found!");
        }
    }

    // 播放音效
    public void PlaySFX(string name)
    {
        if (sfxDict.ContainsKey(name))
        {
            // 使用全局音效音量乘以特定音效的单独音量
            float individualVolume = sfxVolumeDict.ContainsKey(name) ? sfxVolumeDict[name] : 1f;
            //Debug.Log($"Playing SFX: {name} with volume: {sfxVolume * individualVolume}");
            sfxSource.PlayOneShot(sfxDict[name], sfxVolume * individualVolume);
        }
        else
        {
            //Debug.LogWarning($"SFX clip {name} not found! Available clips: {string.Join(", ", sfxDict.Keys)}");
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

    // 设置单个音效的音量
    public void SetSFXVolumeForClip(string name, float volume)
    {
        volume = Mathf.Clamp01(volume); // 确保音量在0到1之间
        
        if (sfxVolumeDict.ContainsKey(name))
        {
            sfxVolumeDict[name] = volume;
            
            // 可选：保存到PlayerPrefs以便在游戏重启后保持设置
            PlayerPrefs.SetFloat($"SFXVolume_{name}", volume);
            PlayerPrefs.Save();
        }
        else
        {
            Debug.LogWarning($"Cannot set volume for SFX clip {name} - not found!");
        }
    }
    
    // 获取单个音效的音量
    public float GetSFXVolumeForClip(string name)
    {
        if (sfxVolumeDict.ContainsKey(name))
        {
            return sfxVolumeDict[name];
        }
        return 1f; // 默认音量
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
