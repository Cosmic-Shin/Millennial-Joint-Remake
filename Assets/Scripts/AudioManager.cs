using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Background Music")]
    [SerializeField] private AudioClip[] backgroundMusicArray;
    private int currentMusicIndex = -1;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip ingredientOnPlateSFX;
    [SerializeField] private AudioClip ingredientInTableSFX;
    [SerializeField] private AudioClip getIngredientFromBoxSFX;
    [SerializeField] private AudioClip serveFoodSFX;
    [SerializeField] private AudioClip throwSFX;
    [SerializeField] private AudioClip doneInteractingSFX;
    [SerializeField] private AudioClip getPlateAndCupSFX;
    [SerializeField] private AudioClip counterTopInteractSFX;

    [Header("Cooking Station Sounds")]
    [SerializeField] private AudioClip startCookingSFX;
    [SerializeField] private AudioClip finishedCookingSFX;
    [SerializeField] private AudioClip startFryingSFX;
    [SerializeField] private AudioClip finishedFryingSFX;
    [SerializeField] private AudioClip startDrinkCoffeeSFX;
    [SerializeField] private AudioClip finishedDrinkCoffeeSFX;
    [SerializeField] private AudioClip startChiliSFX;
    [SerializeField] private AudioClip finishedChiliSFX;

    [Header("Volume")]
    [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private float musicVolume = 0.7f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }

        sfxSource.volume = sfxVolume;
        sfxSource.loop = false;

        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("Music_Source");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
        }

        musicSource.loop = false;
        musicSource.volume = musicVolume;
    }

    private void Start()
    {
        if (backgroundMusicArray != null && backgroundMusicArray.Length > 0)
        {
            PlayNextBackgroundMusic();
        }
    }

    private void Update()
    {
        if (musicSource != null &&
            !musicSource.isPlaying &&
            backgroundMusicArray != null &&
            backgroundMusicArray.Length > 0)
        {
            PlayNextBackgroundMusic();
        }
    }

    private void PlayNextBackgroundMusic()
    {
        if (backgroundMusicArray == null || backgroundMusicArray.Length == 0)
            return;

        int newIndex = Random.Range(0, backgroundMusicArray.Length);

        while (backgroundMusicArray.Length > 1 && newIndex == currentMusicIndex)
        {
            newIndex = Random.Range(0, backgroundMusicArray.Length);
        }

        currentMusicIndex = newIndex;
        musicSource.clip = backgroundMusicArray[newIndex];
        musicSource.Play();
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip, sfxVolume);
        }
    }

    public void PlayIngredientOnPlateSFX()
    {
        PlaySFX(ingredientOnPlateSFX);
    }

    public void PlayIngredientInTableSFX()
    {
        PlaySFX(ingredientInTableSFX);
    }

    public void PlayGetIngredientFromBoxSFX()
    {
        PlaySFX(getIngredientFromBoxSFX);
    }

    public void PlayServeFoodSFX()
    {
        PlaySFX(serveFoodSFX);
    }

    public void PlayThrowSFX()
    {
        PlaySFX(throwSFX);
    }

    public void PlayDoneInteractingSFX()
    {
        PlaySFX(doneInteractingSFX);
    }

    public void PlayGetPlateAndCupSFX()
    {
        PlaySFX(getPlateAndCupSFX);
    }

    public void PlayCounterTopInteractSFX()
    {
        PlaySFX(counterTopInteractSFX);
    }

    public void PlayStartCookingSFX()
    {
        PlaySFX(startCookingSFX);
    }

    public void PlayFinishedCookingSFX()
    {
        PlaySFX(finishedCookingSFX);
    }

    public void PlayStartFryingSFX()
    {
        PlaySFX(startFryingSFX);
    }

    public void PlayFinishedFryingSFX()
    {
        PlaySFX(finishedFryingSFX);
    }

    public void PlayStartDrinkCoffeeSFX()
    {
        PlaySFX(startDrinkCoffeeSFX);
    }

    public void PlayFinishedDrinkCoffeeSFX()
    {
        PlaySFX(finishedDrinkCoffeeSFX);
    }

    public void PlayStartChiliSFX()
    {
        PlaySFX(startChiliSFX);
    }

    public void PlayFinishedChiliSFX()
    {
        PlaySFX(finishedChiliSFX);
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);

        if (musicSource != null)
            musicSource.volume = musicVolume;
    }

    public float GetSFXVolume()
    {
        return sfxVolume;
    }

    public float GetMusicVolume()
    {
        return musicVolume;
    }
}