using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("UI & Global Sounds")]
    public AudioClip ruleChangedClip;
    public AudioClip ruleErasedBrushClip; // Specific brush sound for erasing
    public AudioClip menuClickClip;
    public AudioClip[] suspenseClips;

    [Header("Ambient Loops")]
    public AudioClip ambianceLoop;
    public AudioClip windLoop;
    public AudioClip fireLoop;
    public AudioClip buzzLoop;
    
    private AudioSource globalSource;
    private AudioSource ambianceSource;
    private AudioSource windSource;
    private AudioSource fireSource;
    private AudioSource buzzSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        globalSource = gameObject.AddComponent<AudioSource>();
        globalSource.playOnAwake = false;

        // Setup Ambient Sources
        ambianceSource = CreateLoopingSource("AmbianceSource", ambianceLoop, 0.4f);
        windSource = CreateLoopingSource("WindSource", windLoop, 0.3f);
        fireSource = CreateLoopingSource("FireSource", fireLoop, 0.2f);
        buzzSource = CreateLoopingSource("BuzzSource", buzzLoop, 0.1f);
    }

    private AudioSource CreateLoopingSource(string name, AudioClip clip, float volume)
    {
        AudioSource source = new GameObject(name).AddComponent<AudioSource>();
        source.transform.SetParent(transform);
        source.clip = clip;
        source.loop = true;
        source.volume = volume;
        if (clip != null) source.Play();
        return source;
    }

    public void PlayRuleChanged(bool wasErased)
    {
        if (wasErased && ruleErasedBrushClip != null)
            globalSource.PlayOneShot(ruleErasedBrushClip);
        else if (ruleChangedClip != null)
            globalSource.PlayOneShot(ruleChangedClip);
    }

    public void PlaySuspense()
    {
        if (suspenseClips != null && suspenseClips.Length > 0)
        {
            AudioClip clip = suspenseClips[Random.Range(0, suspenseClips.Length)];
            globalSource.PlayOneShot(clip, 0.5f);
        }
    }

    public void PlayOneShot(AudioClip clip, float volume = 1f)
    {
        if (clip != null)
            globalSource.PlayOneShot(clip, volume);
    }
}
