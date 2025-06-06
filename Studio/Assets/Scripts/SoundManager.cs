using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private AudioSource musicCrossfadeSource; // 用于音乐淡入淡出切换
    [SerializeField] private AudioSource sfxSource;    // 用于播放音效
    private Dictionary<string, AudioSource> activeSfxSources = new Dictionary<string, AudioSource>(); // 用于跟踪正在播放的音效

    [Header("Audio Clips")]
    [SerializeField] private Sound[] musicClips;  // 背景音乐列表
    [SerializeField] private Sound[] sfxClips;    // 音效列表

    [Header("Settings")]
    [SerializeField] private float musicVolume = 1f;
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float crossfadeDuration = 1.5f; // 音乐淡入淡出持续时间
    
    private Dictionary<string, AudioClip> musicDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();
    private Dictionary<string, float> sfxVolumeDict = new Dictionary<string, float>(); // 用于存储每个音效的单独音量
    
    // 当前播放的音乐名称
    private string currentMusic = "";
    private bool isCrossfading = false; // 是否正在进行淡入淡出

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
        
        if (musicCrossfadeSource == null)
        {
            musicCrossfadeSource = gameObject.AddComponent<AudioSource>();
            musicCrossfadeSource.loop = true;
            musicCrossfadeSource.volume = 0f; // 初始音量为0
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
        
        // 注册场景加载事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDestroy()
    {
        // 取消注册场景加载事件
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // 停止所有活跃的音效
        foreach (var kvp in activeSfxSources)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        activeSfxSources.Clear();
    }

    // 场景加载时的处理
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 根据场景名称播放对应音乐
        string sceneName = scene.name;
        Debug.Log($"[SoundManager] 场景加载: {sceneName}");
        
        switch (sceneName)
        {
            case "Start":
            case "Lobby":
                PlaySceneMusic("LevelChoose");
                break;
                
            case "LevelScene":
                PlaySceneMusic("LevelChoose");
                break;
                
            case "Game":
                // Game场景的音乐在LevelManager中根据关卡决定
                PlayGameSceneMusic();
                break;
                
            default:
                // 其他场景暂停音乐
                StopMusic();
                break;
        }
    }
    
    // 播放Game场景音乐（基于当前关卡）
    public void PlayGameSceneMusic()
    {
        // 检查LevelManager是否存在
        if (LevelManager.Instance == null)
        {
            Debug.LogWarning("[SoundManager] LevelManager.Instance为空，无法确定当前关卡音乐");
            return;
        }
        
        // 根据当前关卡和模式决定音乐
        LevelManager.Level currentLevel = LevelManager.Instance.currentLevel.Value;
        
        if (currentLevel == LevelManager.Level.Tutorial)
        {
            PlaySceneMusic("GameTutor");
        }
        else if (currentLevel >= LevelManager.Level.Level5A)
        {
            // Level5和Level6播放GameGun
            PlaySceneMusic("GameGun");
        }
        else
        {
            // Level1-4播放Game
            PlaySceneMusic("Game");
        }
    }
    
    // 播放场景音乐（如果与当前不同）
    private void PlaySceneMusic(string musicName)
    {
        // 如果已经在播放相同的音乐，则不重复播放
        if (currentMusic == musicName && musicSource.isPlaying && !isCrossfading)
        {
            return;
        }
        
        // 使用淡入淡出方式播放新音乐
        CrossfadeToNewMusic(musicName);
        currentMusic = musicName;
    }

    // 淡入淡出切换到新音乐
    private void CrossfadeToNewMusic(string newMusicName)
    {
        // 如果新音乐不存在，则直接返回
        if (!musicDict.ContainsKey(newMusicName))
        {
            Debug.LogWarning($"[SoundManager] 音乐 {newMusicName} 未找到！");
            return;
        }
        
        // 如果正在淡入淡出，先停止当前的淡入淡出
        if (isCrossfading)
        {
            StopAllCoroutines();
        }
        
        // 如果当前没有音乐在播放，直接播放新音乐
        if (!musicSource.isPlaying)
        {
            PlayMusic(newMusicName);
            return;
        }
        
        // 开始淡入淡出
        StartCoroutine(CrossfadeMusicCoroutine(newMusicName, crossfadeDuration));
    }
    
    // 淡入淡出协程
    private IEnumerator CrossfadeMusicCoroutine(string newMusicName, float duration)
    {
        isCrossfading = true;
        
        // 设置淡出源（当前正在播放的）
        AudioSource fadeOutSource = musicSource;
        AudioSource fadeInSource = musicCrossfadeSource;
        
        // 设置淡入源（将要播放的新音乐）
        fadeInSource.clip = musicDict[newMusicName];
        fadeInSource.volume = 0f;
        fadeInSource.Play();
        
        float startVolume = fadeOutSource.volume;
        float timer = 0f;
        
        // 执行淡入淡出
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            // 线性淡入淡出
            fadeOutSource.volume = Mathf.Lerp(startVolume, 0f, t);
            fadeInSource.volume = Mathf.Lerp(0f, musicVolume, t);
            
            yield return null;
        }
        
        // 确保最终状态
        fadeOutSource.Stop();
        fadeOutSource.volume = 0f;
        fadeInSource.volume = musicVolume;
        
        // 交换音源，使主音源始终是当前播放的音乐
        AudioSource temp = musicSource;
        musicSource = musicCrossfadeSource;
        musicCrossfadeSource = temp;
        
        isCrossfading = false;
        
        Debug.Log($"[SoundManager] 音乐淡入淡出完成: {newMusicName}");
    }

    // 播放背景音乐
    public void PlayMusic(string name)
    {
        if (musicDict.ContainsKey(name))
        {
            musicSource.clip = musicDict[name];
            musicSource.volume = musicVolume;
            musicSource.Play();
            Debug.Log($"[SoundManager] 播放音乐: {name}");
        }
        else
        {
            Debug.LogWarning($"[SoundManager] 音乐 {name} 未找到！");
        }
    }

    // 播放音效
    public void PlaySFX(string name)
    {
        PlaySFXWithPitch(name, 1.0f);
    }
    
    // 播放带有指定音调的音效
    public void PlaySFXWithPitch(string name, float pitch)
    {
        if (sfxDict.ContainsKey(name))
        {
            // 使用全局音效音量乘以特定音效的单独音量
            float individualVolume = sfxVolumeDict.ContainsKey(name) ? sfxVolumeDict[name] : 1f;
            //Debug.Log($"Playing SFX: {name} with volume: {sfxVolume * individualVolume} and pitch: {pitch}");
            
            // 对于需要循环播放或可能需要停止的音效，创建专用AudioSource
            if (name == "Type" || name.Contains("Loop"))
            {
                // 如果该音效已有专用AudioSource并且正在播放，则不重新创建
                if (activeSfxSources.ContainsKey(name) && activeSfxSources[name] != null)
                {
                    if (!activeSfxSources[name].isPlaying)
                    {
                        activeSfxSources[name].clip = sfxDict[name];
                        activeSfxSources[name].volume = sfxVolume * individualVolume;
                        activeSfxSources[name].pitch = pitch;
                        activeSfxSources[name].loop = true;
                        activeSfxSources[name].Play();
                    }
                    return;
                }
                
                // 创建新的AudioSource
                AudioSource newSource = gameObject.AddComponent<AudioSource>();
                newSource.clip = sfxDict[name];
                newSource.volume = sfxVolume * individualVolume;
                newSource.pitch = pitch;
                newSource.loop = true;
                newSource.Play();
                
                // 保存到字典中
                activeSfxSources[name] = newSource;
            }
            else
            {
                // 一次性音效使用共享的sfxSource
                float originalPitch = sfxSource.pitch;
                sfxSource.pitch = pitch;
                sfxSource.PlayOneShot(sfxDict[name], sfxVolume * individualVolume);
                // 在播放后恢复原始音调
                StartCoroutine(RestorePitchAfterPlayOneShot(originalPitch));
            }
        }
        else
        {
            Debug.LogWarning($"[SoundManager] 音效 {name} 未找到！可用音效: {string.Join(", ", sfxDict.Keys)}");
        }
    }
    
    // 在PlayOneShot完成后恢复原始音调
    private IEnumerator RestorePitchAfterPlayOneShot(float originalPitch)
    {
        // 等待一小段时间，确保音效开始播放
        yield return new WaitForSeconds(0.1f);
        sfxSource.pitch = originalPitch;
    }

    // 停止特定音效
    public void StopSFX(string name)
    {
        if (activeSfxSources.ContainsKey(name) && activeSfxSources[name] != null)
        {
            activeSfxSources[name].Stop();
        }
    }

    // 停止背景音乐（带淡出效果）
    public void StopMusic()
    {
        StopMusic(crossfadeDuration);
    }
    
    // 停止背景音乐（带淡出效果，指定持续时间）
    public void StopMusic(float fadeOutDuration)
    {
        if (musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusicCoroutine(fadeOutDuration));
        }
        currentMusic = "";
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
        
        // 如果正在淡入淡出，不直接修改音量
        if (!isCrossfading)
        {
            musicSource.volume = musicVolume;
        }
    }

    // 设置音效音量
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        
        // 更新所有活跃的音效源的音量
        foreach (var kvp in activeSfxSources)
        {
            if (kvp.Value != null)
            {
                string sfxName = kvp.Key;
                float individualVolume = sfxVolumeDict.ContainsKey(sfxName) ? sfxVolumeDict[sfxName] : 1f;
                kvp.Value.volume = sfxVolume * individualVolume;
            }
        }
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
        
        // 确保最终音量正确
        musicSource.volume = musicVolume;
    }

    // 淡出音乐
    public void FadeOutMusic(float duration)
    {
        if (musicSource.isPlaying)
        {
            StartCoroutine(FadeOutMusicCoroutine(duration));
        }
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
        musicSource.volume = 0;
    }

    // 设置单个音效的音量
    public void SetSFXVolumeForClip(string name, float volume)
    {
        volume = Mathf.Clamp01(volume); // 确保音量在0到1之间
        
        if (sfxVolumeDict.ContainsKey(name))
        {
            sfxVolumeDict[name] = volume;
            
            // 更新正在播放的该音效的音量
            if (activeSfxSources.ContainsKey(name) && activeSfxSources[name] != null)
            {
                activeSfxSources[name].volume = sfxVolume * volume;
            }
            
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
}
