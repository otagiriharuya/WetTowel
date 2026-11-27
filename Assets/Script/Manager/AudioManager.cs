using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// BGM・効果音の再生をまとめて管理する。Title_Windows に配置し、
/// DontDestroyOnLoad でシーンをまたいで BGM を流し続ける。
/// Game_Windows にも同じものを置いてあり、重複した場合は後から来た方を破棄する。
/// 音声ファイルは Assets/Resources/Audio/ から読み込む（Inspector の割当は不要）。
///
/// BGM の使い分け（Round11）:
///   タイトル: bgm_title / セレクト: bgm_select
///   リザルト: bgm_result（全モード・難易度共通）
///   ゲーム中: モード×難易度別
///     STANDARD EASY/NORMAL/HARD、TIME ATTACK HARD、SCORE ATTACK HARD、
///     ENDLESS EASY/NORMAL/HARD（TIME/SCORE ATTACK は仕様上 HARD 固定）
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private float bgmVolume = 0.35f;
    [SerializeField] private float seVolume = 0.8f;

    private AudioSource _bgmSource;
    private AudioSource _seSource;

    private readonly Dictionary<string, AudioClip> _bgmClips =
        new Dictionary<string, AudioClip>();

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

    /// <summary>最後に再生した効果音の名前（テスト用）。</summary>
    public string LastPlayedSe { get; private set; } = "";

    /// <summary>効果音ごとの再生回数（テスト用）。上書きに強く、判定の直後に読んでも正しい。</summary>
    private readonly System.Collections.Generic.Dictionary<string, int> _seCounts =
        new System.Collections.Generic.Dictionary<string, int>();

    public int SePlayedCount(string seName)
    {
        return _seCounts.TryGetValue(seName, out var n) ? n : 0;
    }

    /// <summary>BGM が再生中かどうか（テスト用）。</summary>
    public bool IsBgmPlaying => _bgmSource != null && _bgmSource.isPlaying;

    /// <summary>現在流れている BGM の名前（テスト用）。</summary>
    public string CurrentBgmName { get; private set; } = "";

    /// <summary>BGM が1曲でも読み込めているかどうか（テスト用）。</summary>
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

        _bgmSource = gameObject.AddComponent<AudioSource>();
        _bgmSource.loop = true;
        _bgmSource.playOnAwake = false;
        _bgmSource.volume = bgmVolume;

        _seSource = gameObject.AddComponent<AudioSource>();
        _seSource.loop = false;
        _seSource.playOnAwake = false;
        _seSource.volume = seVolume;

        string[] bgmNames = {
            "bgm_title", "bgm_select", "bgm_result",
            "bgm_game_standard_easy", "bgm_game_standard_normal", "bgm_game_standard_hard",
            "bgm_game_timeattack_hard", "bgm_game_scoreattack_hard",
            "bgm_game_endless_easy", "bgm_game_endless_normal", "bgm_game_endless_hard",
        };
        foreach (var n in bgmNames)
            _bgmClips[n] = Resources.Load<AudioClip>("Audio/" + n);

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
        PlayTitleBgm();
    }

    /// <summary>名前で BGM を切り替える。同じ曲が再生中なら切り替えない。</summary>
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

    /// <summary>モード・難易度に合わせたゲーム BGM を流す。</summary>
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

    /// <summary>互換用：タイトル BGM を流す。</summary>
    public void PlayBgm() { PlayTitleBgm(); }

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

    private void PlaySe(AudioClip clip, string seName)
    {
        if (clip == null || _seSource == null)
            return;
        LastPlayedSe = seName;
        _seCounts[seName] = SePlayedCount(seName) + 1;
        _seSource.PlayOneShot(clip);
    }
}
