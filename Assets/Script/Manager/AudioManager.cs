using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private float bgmVolume = 0.35f; // BGMの音量
    [SerializeField] private float seVolume = 0.8f; // 効果音の音量

    private AudioSource _bgmSource;
    private AudioSource _seSource;

    private readonly Dictionary<string, AudioClip> _bgmClips = new Dictionary<string, AudioClip>();

    private AudioClip _clickClip;
    private AudioClip _pickupClip;
    private AudioClip _perfectClip;
    private AudioClip _greatClip;
    private AudioClip _goodClip;
    private AudioClip _missClip;
    private AudioClip _unlockClip;
    private AudioClip _startClip;
    private AudioClip _gameOverClip;
    private AudioClip _clearClip;

    // 最後に再生した効果音の名前
    public string LastPlayedSe { get; private set; } = "";

    // 効果音ごとの再生回数
    private readonly System.Collections.Generic.Dictionary<string, int> _seCounts = new System.Collections.Generic.Dictionary<string, int>();

    // 指定した効果音が何回再生されたかを返す
    public int SePlayedCount(string seName)
    {
        return _seCounts.TryGetValue(seName, out var n) ? n : 0;
    }

    // BGM が再生中かどうか
    public bool IsBgmPlaying => _bgmSource != null && _bgmSource.isPlaying;

    // 現在流れているBGMの名前
    public string CurrentBgmName { get; private set; } = "";

    // BGMが読み込めているかどうか
    public bool IsBgmClipLoaded => _bgmClips.Count > 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // BGM用途効果音用のAudioSourceを作成
        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = bgmVolume;

        _seSource = gameObject.AddComponent<AudioSource>();
        _seSource.loop = false;
        _seSource.playOnAwake = false;
        _seSource.volume = seVolume;

        // BGMのAudioClipをResourcesから読み込む
        string[] bgmNames = {
            "bgm_title", "bgm_select", "bgm_result",
            "bgm_game_standard_easy", "bgm_game_standard_normal", "bgm_game_standard_hard",
            "bgm_game_timeattack_hard", "bgm_game_scoreattack_hard",
            "bgm_game_endless_easy", "bgm_game_endless_normal", "bgm_game_endless_hard",
        };
        foreach (var n in bgmNames)
            _bgmClips[n] = Resources.Load<AudioClip>("Audio/" + n);

        // 効果音のAudioClipをResourcesから読み込む
        _clickClip = Resources.Load<AudioClip>("Audio/se_click");
        _pickupClip = Resources.Load<AudioClip>("Audio/se_pickup");
        _perfectClip = Resources.Load<AudioClip>("Audio/se_perfect");
        _greatClip = Resources.Load<AudioClip>("Audio/se_great");
        _goodClip = Resources.Load<AudioClip>("Audio/se_good");
        _missClip = Resources.Load<AudioClip>("Audio/se_miss");
        _unlockClip = Resources.Load<AudioClip>("Audio/se_unlock");
        _startClip = Resources.Load<AudioClip>("Audio/se_start");
        _gameOverClip = Resources.Load<AudioClip>("Audio/se_gameover");
        _clearClip = Resources.Load<AudioClip>("Audio/se_clear");
    }

    private void Start()
    {
        PlayTitleBgm(); // タイトルBGMの再生
    }

    // 名前でBGMを切り替える
    public void PlayBgmByName(string bgmName)
    {
        if (_bgmSource == null)
            return;
        if (!_bgmClips.TryGetValue(bgmName, out var clip) || clip == null)
            return;
        if (CurrentBgmName == bgmName && _bgmSource.isPlaying)
            return;
        CurrentBgmName = bgmName;
        _bgmSource.clip = clip;
        _bgmSource.Play();
    }

    public void PlayTitleBgm() { PlayBgmByName("bgm_title"); }
    public void PlaySelectBgm() { PlayBgmByName("bgm_select"); }
    public void PlayResultBgm() { PlayBgmByName("bgm_result"); }

    // モード・難易度に合わせたゲームBGMを流す
    public void PlayGameBgm(string mode, string difficulty)
    {
        string diff = (difficulty ?? "NORMAL").ToLowerInvariant();
        if (diff != "easy" && diff != "normal" && diff != "hard")
            diff = "normal";
        string name;
        if (mode == "TIME ATTACK")
            name = "bgm_game_timeattack_hard";
        else if (mode == "SCORE ATTACK")
            name = "bgm_game_scoreattack_hard";
        else if (mode == "ENDLESS")
            name = "bgm_game_endless_" + diff;
        else
            name = "bgm_game_standard_" + diff;
        PlayBgmByName(name);
    }

    // タイトルBGMを流す
    public void PlayBgm() { PlayTitleBgm(); }

    // 効果音を再生するメソッド
    public void PlayClick() { PlaySe(_clickClip, "click"); }
    public void PlayPickup() { PlaySe(_pickupClip, "pickup"); }
    public void PlayPerfect() { PlaySe(_perfectClip, "perfect"); }
    public void PlayGreat() { PlaySe(_greatClip, "great"); }
    public void PlayGood() { PlaySe(_goodClip, "good"); }
    public void PlayMiss() { PlaySe(_missClip, "miss"); }
    public void PlayUnlock() { PlaySe(_unlockClip, "unlock"); }
    public void PlayStart() { PlaySe(_startClip, "start"); }
    public void PlayGameOver() { PlaySe(_gameOverClip, "gameover"); }
    public void PlayClear() { PlaySe(_clearClip, "clear"); }

    // 効果音を再生する
    private void PlaySe(AudioClip clip, string seName)
    {
        if (clip == null || _seSource == null)
            return;
        LastPlayedSe = seName;
        _seCounts[seName] = SePlayedCount(seName) + 1;
        _seSource.PlayOneShot(clip);
    }
}