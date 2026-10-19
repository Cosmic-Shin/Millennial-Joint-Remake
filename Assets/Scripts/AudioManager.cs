using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Menu Music")]
    [SerializeField] private AudioClip menuBGM;

    [Header("Gameplay Music")]
    [SerializeField] private AudioClip[] gameplayBGMArray;
    private int currentMusicIndex = -1;
    private bool isGameplayMusicMode = false;

    [Header("Game End Music")]
    [SerializeField] private AudioClip gameEndBGM;

    [Header("UI Button SFX")]
    [SerializeField] private AudioClip buttonClickSFX;
    [SerializeField] private Button[] buttonsWithSFX;

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

    [Header("Game Event Sounds")]
    [SerializeField] private AudioClip countdownBeepSFX;
    [SerializeField] private AudioClip goHornSFX;
    [SerializeField] private AudioClip tipSFX;
    [SerializeField] private AudioClip penaltySFX;
    [SerializeField] private AudioClip freezeSFX;
    [SerializeField] private AudioClip comboUpSFX;
    [SerializeField] private AudioClip comboLostSFX;
    [SerializeField] private AudioClip chalkHoverSFX;
    [SerializeField] private AudioClip burnSizzleSFX;

    [Header("Volume")]
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.7f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.buttonsWithSFX = buttonsWithSFX;
            Instance.RegisterButtonSFX();
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", sfxVolume);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", musicVolume);

        SetupSources();
    }

    private void Start()
    {
        RegisterButtonSFX();
        PlayMenuBGM();
    }

    private void Update()
    {
        if (isGameplayMusicMode &&
            musicSource != null &&
            !musicSource.isPlaying &&
            gameplayBGMArray != null &&
            gameplayBGMArray.Length > 0)
        {
            PlayNextGameplayBGM();
        }
    }

    private void SetupSources()
    {
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX_Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
        }

        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("Music_Source");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
        }

        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
    }

    private void RegisterButtonSFX()
    {
        if (buttonsWithSFX == null)
            return;

        foreach (Button button in buttonsWithSFX)
        {
            if (button == null)
                continue;

            button.onClick.RemoveListener(PlayButtonClickSFX);
            button.onClick.AddListener(PlayButtonClickSFX);
        }
    }

    public void PlayMenuBGM()
    {
        isGameplayMusicMode = false;

        if (menuBGM == null || musicSource == null)
            return;

        if (musicSource.clip == menuBGM && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.loop = true;
        musicSource.clip = menuBGM;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayGameplayBGM()
    {
        isGameplayMusicMode = true;
        currentMusicIndex = -1;
        PlayNextGameplayBGM();
    }

    private void PlayNextGameplayBGM()
    {
        if (gameplayBGMArray == null || gameplayBGMArray.Length == 0 || musicSource == null)
            return;

        int newIndex = Random.Range(0, gameplayBGMArray.Length);

        while (gameplayBGMArray.Length > 1 && newIndex == currentMusicIndex)
            newIndex = Random.Range(0, gameplayBGMArray.Length);

        currentMusicIndex = newIndex;

        musicSource.Stop();
        musicSource.loop = false;
        musicSource.clip = gameplayBGMArray[currentMusicIndex];
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    public void PlayGameEndBGM()
    {
        isGameplayMusicMode = false;

        if (gameEndBGM == null || musicSource == null)
            return;

        musicSource.Stop();
        musicSource.loop = true;
        musicSource.clip = gameEndBGM;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, sfxVolume);
    }

    [Header("Pitch Variation")]
    [Range(0f, 0.3f)] [SerializeField] private float pitchVariation = 0.08f;

    private AudioSource[] variedSources;
    private int nextVariedSource;

    private void PlayVariedSFX(AudioClip clip, float basePitch = 1f, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null)
            return;

        if (variedSources == null)
        {
            variedSources = new AudioSource[6];
            for (int i = 0; i < variedSources.Length; i++)
            {
                AudioSource source = new GameObject("Varied_SFX_Source").AddComponent<AudioSource>();
                source.transform.SetParent(transform);
                source.playOnAwake = false;
                source.outputAudioMixerGroup = sfxSource.outputAudioMixerGroup;
                variedSources[i] = source;
            }
        }

        AudioSource next = variedSources[nextVariedSource];
        nextVariedSource = (nextVariedSource + 1) % variedSources.Length;

        next.volume = sfxSource.volume;
        next.pitch = basePitch * Random.Range(1f - pitchVariation, 1f + pitchVariation);
        next.PlayOneShot(clip, sfxVolume * volumeScale);
    }

    public void PlayButtonClickSFX()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlayOneShot(AudioClip clip) => PlaySFX(clip);

    public void PlayCountdownBeep() => PlaySFX(countdownBeepSFX);
    public void PlayGoHorn() => PlaySFX(goHornSFX);
    public void PlayTipSFX() => PlayVariedSFX(tipSFX);
    public void PlayPenaltySFX() => PlaySFX(penaltySFX);
    public void PlayFreezeSFX() => PlaySFX(freezeSFX);
    public void PlayComboLostSFX() => PlaySFX(comboLostSFX);
    public void PlayChalkHoverSFX() => PlaySFX(chalkHoverSFX);

    public AudioClip BurnSizzleClip => burnSizzleSFX;

    public void PlayComboUpSFX(float multiplier)
    {
        if (comboUpSFX == null)
            return;

        if (pitchedSource == null)
        {
            pitchedSource = new GameObject("Pitched_SFX_Source").AddComponent<AudioSource>();
            pitchedSource.transform.SetParent(transform);
            pitchedSource.playOnAwake = false;
        }

        pitchedSource.pitch = Mathf.Lerp(1f, 1.5f, Mathf.Clamp01((multiplier - 1f) / 1f));
        pitchedSource.PlayOneShot(comboUpSFX, sfxVolume);
    }

    private AudioSource pitchedSource;

    public void PlayIngredientOnPlateSFX() => PlayVariedSFX(ingredientOnPlateSFX);
    public void PlayIngredientInTableSFX() => PlayVariedSFX(ingredientInTableSFX);
    public void PlayGetIngredientFromBoxSFX() => PlayVariedSFX(getIngredientFromBoxSFX);
    public void PlayServeFoodSFX() => PlayVariedSFX(serveFoodSFX);
    public void PlayThrowSFX() => PlayVariedSFX(throwSFX);
    public void PlayDoneInteractingSFX() => PlayVariedSFX(doneInteractingSFX);
    public void PlayGetPlateAndCupSFX() => PlayVariedSFX(getPlateAndCupSFX);
    public void PlayCounterTopInteractSFX() => PlayVariedSFX(counterTopInteractSFX);
    public void PlayLandSFX(float strength) => PlayVariedSFX(counterTopInteractSFX, 0.7f, Mathf.Clamp01(strength) * 0.7f);
    public void PlayNopeSFX() => PlayVariedSFX(penaltySFX, 1.35f, 0.45f);
    public void PlayCoinTick() => PlayVariedSFX(tipSFX, 1.5f, 0.2f);

    public void PlayStartCookingSFX() => PlayVariedSFX(startCookingSFX);
    public void PlayFinishedCookingSFX() => PlayVariedSFX(finishedCookingSFX);
    public void PlayStartFryingSFX() => PlayVariedSFX(startFryingSFX);
    public void PlayFinishedFryingSFX() => PlayVariedSFX(finishedFryingSFX);
    public void PlayStartDrinkCoffeeSFX() => PlayVariedSFX(startDrinkCoffeeSFX);
    public void PlayFinishedDrinkCoffeeSFX() => PlayVariedSFX(finishedDrinkCoffeeSFX);
    public void PlayStartChiliSFX() => PlayVariedSFX(startChiliSFX);
    public void PlayFinishedChiliSFX() => PlayVariedSFX(finishedChiliSFX);

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);

        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);

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