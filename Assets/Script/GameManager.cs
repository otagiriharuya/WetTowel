using UnityEngine;
using System;
using DG.Tweening;

// ゲームの状態定義
public enum GameState
{
    Ready, // 開始前
    Playing, // プレイ中
    GameOver // ゲームオーバー
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float initialTime = 60f; // 初期制限時間（秒）
    [SerializeField] private float maxTime = 99f; // 制限時間の最大値（上限を設定してオーバーフロー防止）

    [SerializeField] private int warmerUnlockScore = 3000; // 保温庫2個目の解放スコア
    [SerializeField] private int coolerUnlockScore = 7000; // 保冷庫2個目の解放スコア

    [SerializeField] private bool showDebugLog = true;

    // ゲーム状態変数
    private float _currentTime;
    private int _currentScore;
    private int _comboCount;
    private GameState _currentState = GameState.Ready;

    // UIや他システムへ更新を伝えるイベントアクション
    public event Action<float> OnTimeChanged; // 時間変化時 (残り時間)
    public event Action<int> OnScoreChanged; // スコア変化時 (現在スコア)
    public event Action<int> OnComboChanged; // コンボ変化時 (現在コンボ数)
    public event Action<GameState> OnStateChanged; // ゲーム状態変更時

    // プロパティ
    public float CurrentTime => _currentTime;
    public int CurrentScore => _currentScore;
    public int ComboCount => _comboCount;
    public GameState CurrentState => _currentState;

    private void Awake()
    {
        // シングルトンのセットアップ
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        StartGame(); // テスト用：シーン開始と同時に自動でゲームスタート
    }

    private void Update()
    {
        if (_currentState != GameState.Playing)
            return;

        // カウントダウン処理
        _currentTime -= Time.deltaTime;
        OnTimeChanged?.Invoke(_currentTime);

        // タイムアップ（ゲームオーバー判定）
        if (_currentTime <= 0f)
        {
            _currentTime = 0f;
            OnTimeChanged?.Invoke(_currentTime);
            EndGame();
        }
    }

    // ゲーム開始処理
    public void StartGame()
    {
        Time.timeScale = 1.0f; // 時間の再開
        _currentTime = initialTime;
        _currentScore = 0;
        _comboCount = 0;
        _currentState = GameState.Playing;

        OnTimeChanged?.Invoke(_currentTime);
        OnScoreChanged?.Invoke(_currentScore);
        OnComboChanged?.Invoke(_comboCount);
        OnStateChanged?.Invoke(_currentState);

        if (showDebugLog)
            Debug.Log("<color=green>【ゲーム開始】</color> タイマーカウントダウンを開始します。");
    }

    // スコア加算 ＆ コンボ・時間回復処理
    public void AddScore(int amount, CustomerEvaluation eval)
    {
        if (_currentState != GameState.Playing)
            return;

        // コンボ計算
        if (eval == CustomerEvaluation.Perfect || eval == CustomerEvaluation.Great)
        {
            _comboCount++;
        }
        else if (eval == CustomerEvaluation.Miss)
        {
            _comboCount = 0; // Missでコンボリセット
        }
        OnComboChanged?.Invoke(_comboCount);

        // スコア加算（マイナスにも対応）
        _currentScore = Mathf.Max(0, _currentScore + amount);
        OnScoreChanged?.Invoke(_currentScore);

        // 判定に応じた制限時間の加減算処理
        if (eval == CustomerEvaluation.Perfect)
        {
            AddTime(1.0f); // PERFECT時 +1.0秒回復
        }
        else if (eval == CustomerEvaluation.Miss)
        {
            AddTime(-2.0f); // MISS時 -2.0秒ペナルティ
        }

        if (showDebugLog)
            Debug.Log($"<color=cyan>【スコア更新】</color> +{amount} pt (合計: {_currentScore} pt) | コンボ: {_comboCount} | 評価: {eval}");

        // スコアによる段階解放チェック
        CheckUnlockThresholds();
    }

    // 時間加減算処理
    public void AddTime(float seconds)
    {
        if (_currentState != GameState.Playing)
            return;

        _currentTime = Mathf.Clamp(_currentTime + seconds, 0f, maxTime);
        OnTimeChanged?.Invoke(_currentTime);

        if (showDebugLog && seconds != 0)
        {
            string color = seconds > 0 ? "green" : "red";
            Debug.Log($"<color={color}>【時間変化】</color> {seconds:+#.0;-#.0;0} 秒 | 残り: {_currentTime:F1} 秒");
        }
    }

    // 怒り退場ペナルティ処理（Customer.csから直接呼ばれる）
    public void ApplyAngerPenalty()
    {
        if (_currentState != GameState.Playing)
            return;

        _comboCount = 0; // コンボリセット
        OnComboChanged?.Invoke(_comboCount);

        _currentScore = Mathf.Max(0, _currentScore - 500); // -500pt
        OnScoreChanged?.Invoke(_currentScore);

        AddTime(-5.0f); // -5.0秒
    }

    // スコアに応じた要素解放のチェック（機械枠の増設など）
    private void CheckUnlockThresholds()
    {
        
    }

    // タイムアップ処理
    private void EndGame()
    {
        _currentState = GameState.GameOver;
        OnStateChanged?.Invoke(_currentState);
        Time.timeScale = 0.0f; // ザワールド

        if (showDebugLog)
            Debug.LogWarning($"<color=red>【GAME OVER】</color> タイムアップ！ 最終スコア: {_currentScore} pt");

        // ハイスコア保存
        int highscore = PlayerPrefs.GetInt("HighScore", 0);
        if (_currentScore > highscore)
        {
            PlayerPrefs.SetInt("HighScore", _currentScore);
            PlayerPrefs.Save();
            if (showDebugLog)
                Debug.Log($"<color=yellow>★ハイスコア更新！★</color> 旧: {highscore} pt ➔ 新: {_currentScore} pt");
        }
    }
}