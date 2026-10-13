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

    public void PlayButtonClickSFX()
    {
        PlaySFX(buttonClickSFX);
    }

    public void PlayOneShot(AudioClip clip) => PlaySFX(clip);

    public void PlayCountdownBeep() => PlaySFX(countdownBeepSFX);
    public void PlayGoHorn() => PlaySFX(goHornSFX);
    public void PlayTipSFX() => PlaySFX(tipSFX);
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

    public void PlayIngredientOnPlateSFX() => PlaySFX(ingredientOnPlateSFX);
    public void PlayIngredientInTableSFX() => PlaySFX(ingredientInTableSFX);
    public void PlayGetIngredientFromBoxSFX() => PlaySFX(getIngredientFromBoxSFX);
    public void PlayServeFoodSFX() => PlaySFX(serveFoodSFX);
    public void PlayThrowSFX() => PlaySFX(throwSFX);
    public void PlayDoneInteractingSFX() => PlaySFX(doneInteractingSFX);
    public void PlayGetPlateAndCupSFX() => PlaySFX(getPlateAndCupSFX);
    public void PlayCounterTopInteractSFX() => PlaySFX(counterTopInteractSFX);

    public void PlayStartCookingSFX() => PlaySFX(startCookingSFX);
    public void PlayFinishedCookingSFX() => PlaySFX(finishedCookingSFX);
    public void PlayStartFryingSFX() => PlaySFX(startFryingSFX);
    public void PlayFinishedFryingSFX() => PlaySFX(finishedFryingSFX);
    public void PlayStartDrinkCoffeeSFX() => PlaySFX(startDrinkCoffeeSFX);
    public void PlayFinishedDrinkCoffeeSFX() => PlaySFX(finishedDrinkCoffeeSFX);
    public void PlayStartChiliSFX() => PlaySFX(startChiliSFX);
    public void PlayFinishedChiliSFX() => PlaySFX(finishedChiliSFX);

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