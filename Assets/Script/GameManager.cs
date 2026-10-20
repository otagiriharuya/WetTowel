using UnityEngine;
using UnityEngine.InputSystem;
using System;

// ゲームの状態定義
public enum GameState
{
    Ready, // 開始前
    Playing, // プレイ中
    GameOver, // ゲームオーバー
    GameClear // クリア
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private float initialTime = 60f; // 初期制限時間（秒）
    [SerializeField] private float maxTime = 99f; // 制限時間の最大値（上限を設定してオーバーフロー防止）
    [SerializeField] private int targetScoreForTimeAttack = 5000; // タイムアタックの目標スコア

    [SerializeField] private int warmerUnlockScore = 3000; // 保温庫2個目の解放スコア
    [SerializeField] private int coolerUnlockScore = 7000; // 保冷庫2個目の解放スコア

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    // 現在の選択設定
    private string _currentMode = "STANDARD";
    private string _currentDifficulty = "NORMAL";

    // ゲーム状態変数
    private float _currentTime;
    private int _currentScore;
    private int _comboCount;
    private int _missCount; // エンドレス用ミス回数
    private float _elapsedTimeForTimeAttack; // タイムアタック用経過時間
    private float _playTimer; // プレイ経過時間（エンドレスの難易度上昇用など）
    private GameState _currentState = GameState.Ready;

    // UIや他システムへ更新を伝えるイベントアクション
    public event Action<float> OnTimeChanged; // 時間変化時 (残り時間)
    public event Action<int> OnScoreChanged; // スコア変化時 (現在スコア)
    public event Action<int> OnComboChanged; // コンボ変化時 (現在コンボ数)
    public event Action<int> OnMissChanged; // エンドレス用ミス通知
    public event Action<GameState> OnStateChanged; // ゲーム状態変更時

    // プロパティ
    public float CurrentTime => _currentTime;
    public int CurrentScore => _currentScore;
    public int ComboCount => _comboCount;
    public int MissCount => _missCount;
    public string CurrentMode => _currentMode;
    public string CurrentDifficulty => _currentDifficulty;
    public GameState CurrentState => _currentState;
    public float ElapsedTimeForTimeAttack => _elapsedTimeForTimeAttack;

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

        // セレクト画面で保存された設定を読み込み
        _currentMode = PlayerPrefs.GetString("SelectedMode", "STANDARD");
        _currentDifficulty = PlayerPrefs.GetString("SelectedDifficulty", "NORMAL");

        // タイムアタック・スコアアタックはハード固定仕様の二重担保
        if (_currentMode == "TIME ATTACK" || _currentMode == "SCORE ATTACK")
        {
            _currentDifficulty = "HARD";
        }
    }

    private void Start()
    {
        StartGame(); // シーン開始と同時に自動でゲームスタート
    }

    private void Update()
    {
        if (_currentState != GameState.Playing)
            return;

        _playTimer += Time.deltaTime;

        // モード別の時間・条件進行処理
        if (_currentMode == "TIME ATTACK")
        {
            // タイムアタックは経過時間を計測し、UIを更新
            _elapsedTimeForTimeAttack += Time.deltaTime;
            OnTimeChanged?.Invoke(_elapsedTimeForTimeAttack);
        }
        else if (_currentMode == "SCORE ATTACK" || _currentMode == "STANDARD")
        {
            _currentTime -= Time.deltaTime;
            OnTimeChanged?.Invoke(_currentTime);

            if (_currentTime <= 0f)
            {
                _currentTime = 0f;
                OnTimeChanged?.Invoke(_currentTime);
                EndGame(false);
            }
        }

        // Escキーでゲームを終わらせる
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ExitGame();
        }
    }

    // エンドレス用：経過時間（秒）に応じて 0.0（EASY相当）〜 1.0（HARD相当）の係数を返す
    public float GetEndlessDifficultyProgress()
    {
        if (_currentMode != "ENDLESS")
            return 0f;
        // 120秒（2分）かけてマックス（HARD相当）へ到達する
        return Mathf.Clamp01(_playTimer / 120f);
    }

    // ゲームの終了処理
    public void ExitGame()
    {
        if (showDebugLog)
            Debug.Log("<color=red>【ゲーム終了】</color>");

#if UNITY_EDITOR
        // Unityエディター実行中：再生モードを停止
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // ビルド後（exe等）：アプリケーションを終了
        Application.Quit();
#endif
    }

    // ゲーム開始処理
    public void StartGame()
    {
        Time.timeScale = 1.0f; // 時間の再開
        _currentScore = 0;
        _comboCount = 0;
        _missCount = 0;
        _elapsedTimeForTimeAttack = 0f;
        _playTimer = 0f;
        _currentState = GameState.Playing;

        // モードに応じた初期時間のセット
        if (_currentMode == "TIME ATTACK")
        {
            _currentTime = 0f; // タイムアタックでは残り時間表示ではなく経過時間を主に見せる
        }
        else if (_currentMode == "ENDLESS")
        {
            _currentTime = 999f; // エンドレスは時間制限なし
        }
        else
        {
            // スタンダード・スコアアタックの初期時間
            if (_currentDifficulty == "EASY")
                _currentTime = initialTime + 20f;
            else if (_currentDifficulty == "HARD")
                _currentTime = initialTime - 15f;
            else
                _currentTime = initialTime;
        }

        OnTimeChanged?.Invoke(_currentMode == "TIME ATTACK" ? _elapsedTimeForTimeAttack : _currentTime);
        OnScoreChanged?.Invoke(_currentScore);
        OnComboChanged?.Invoke(_comboCount);
        OnMissChanged?.Invoke(_missCount);
        OnStateChanged?.Invoke(_currentState);

        if (showDebugLog)
            Debug.Log($"<color=green>【ゲーム開始】</color> モード: {_currentMode} / 難易度: {_currentDifficulty}");
    }

    // スコア加算 ＆ コンボ・時間回復処理
    public void AddScore(int amount, CustomerEvaluation eval)
    {
        if (_currentState != GameState.Playing)
            return;

        // コンボ・ミス計算
        if (eval == CustomerEvaluation.Perfect || eval == CustomerEvaluation.Great)
        {
            _comboCount++;
        }
        else if (eval == CustomerEvaluation.Miss)
        {
            _comboCount = 0;
            _missCount++;
            OnMissChanged?.Invoke(_missCount);

            // エンドレスモードでミス3回に達した場合の終了判定
            if (_currentMode == "ENDLESS" && _missCount >= 3)
            {
                EndGame(false);
                return;
            }
        }
        OnComboChanged?.Invoke(_comboCount);

        // スコア加算
        _currentScore = Mathf.Max(0, _currentScore + amount);
        OnScoreChanged?.Invoke(_currentScore);

        // スタンダード等の時間加減算
        if (_currentMode != "ENDLESS" && _currentMode != "TIME ATTACK")
        {
            if (eval == CustomerEvaluation.Perfect)
            {
                AddTime(2.0f);
            }
            else if (eval == CustomerEvaluation.Miss)
            {
                AddTime(-2.0f);
            }
        }

        // タイムアタックのクリア条件チェック（目標スコア到達）
        if (_currentMode == "TIME ATTACK" && _currentScore >= targetScoreForTimeAttack)
        {
            EndGame(true); // クリア！
            return;
        }

        if (showDebugLog)
            Debug.Log($"<color=cyan>【スコア更新】</color> +{amount} pt (合計: {_currentScore}) | ミス回数: {_missCount}");
    }

    // 時間加減算処理
    public void AddTime(float seconds)
    {
        if (_currentState != GameState.Playing || _currentMode == "ENDLESS" || _currentMode == "TIME ATTACK")
            return;

        _currentTime = Mathf.Clamp(_currentTime + seconds, 0f, maxTime);
        OnTimeChanged?.Invoke(_currentTime);
    }

    // 怒り退場ペナルティ処理
    public void ApplyAngerPenalty()
    {
        if (_currentState != GameState.Playing)
            return;

        _comboCount = 0; // コンボリセット
        OnComboChanged?.Invoke(_comboCount);

        _currentScore = Mathf.Max(0, _currentScore - 500); // -500pt
        OnScoreChanged?.Invoke(_currentScore);

        _missCount++;
        OnMissChanged?.Invoke(_missCount);

        if (_currentMode == "ENDLESS" && _missCount >= 3)
        {
            EndGame(false);
            return;
        }

        if (_currentMode != "ENDLESS" && _currentMode != "TIME ATTACK")
        {
            AddTime(-5.0f);
        }
    }

    // 終了処理（クリア または ゲームオーバー）
    private void EndGame(bool isClear)
    {
        _currentState = isClear ? GameState.GameClear : GameState.GameOver;
        OnStateChanged?.Invoke(_currentState);
        Time.timeScale = 0.0f;

        // モード別のハイスコアキー生成
        string saveKey = $"HighScore_{_currentMode}_{_currentDifficulty}";

        if (_currentMode == "TIME ATTACK")
        {
            // タイムアタックの場合は「かかった時間（短いほど良い）」を保存（初回または自己ベスト更新時）
            float bestTime = PlayerPrefs.GetFloat(saveKey, 9999f);
            if (isClear && _elapsedTimeForTimeAttack < bestTime)
            {
                PlayerPrefs.SetFloat(saveKey, _elapsedTimeForTimeAttack);
                PlayerPrefs.Save();
            }
        }
        else
        {
            // 通常のスコア保存
            int highScore = PlayerPrefs.GetInt(saveKey, 0);
            if (_currentScore > highScore)
            {
                PlayerPrefs.SetInt(saveKey, _currentScore);
                PlayerPrefs.Save();
            }
        }

        if (showDebugLog)
            Debug.LogWarning($"<color=red>【ゲーム終了】</color> 状態: {_currentState} | スコア: {_currentScore}");
    }

    // スコアに応じた要素解放のチェック（機械枠の増設など）
    private void CheckUnlockThresholds()
    {

    }
}