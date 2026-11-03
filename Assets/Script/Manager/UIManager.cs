using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text timeText; // 制限時間テキスト
    [SerializeField] private Text scoreText; // スコアテキスト
    [SerializeField] private Text comboText; // コンボテキスト

    [SerializeField] private GameObject resultPanel; // リザルト全体パネル
    [SerializeField] private Text finalScoreText; // 最終スコアテキスト
    [SerializeField] private Text highScoreText; // ハイスコアテキスト
    [SerializeField] private Button restartButton; // リスタートボタン
    [SerializeField] private Button titleButton; // タイトルへ戻るボタン

    [SerializeField] private Color normalTimeColor = Color.white; // 通常時のタイマー文字色
    [SerializeField] private Color warningTimeColor = Color.red; // 残り時間わずか（10秒以下）の文字色
    [SerializeField] private float warningThreshold = 10f; // 警告演出を始める秒数

    [SerializeField] private SceneNavigator sceneNavigator;

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private int _lastDisplayedTime = -1; // 不要なテキスト更新を減らすための保持変数

    private void OnEnable()
    {
        // GameManagerのイベントに登録
        if (GameManager.Instance != null)
        {
            SubscribeEvents();
        }
    }

    private void Start()
    {
        SubscribeEvents(); // OnEnable時点でGameManagerが未準備だった場合のフォールバック登録

        // 初期UI状態のセットアップ
        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (comboText != null)
            comboText.gameObject.SetActive(false); // コンボ非表示からスタート

        // ボタンのクリックイベント設定
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        if (titleButton != null)
            titleButton.onClick.AddListener(OnTitleButtonClicked);

        // 初期状態でミス表示・時間表示を即座にUIへ適用
        if (GameManager.Instance != null)
            UpdateMissUI(GameManager.Instance.MissCount);
    }

    private void OnDisable()
    {
        // メモリリーク防止のためイベント解除
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged -= UpdateTimeUI;
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnComboChanged -= UpdateComboUI;
            GameManager.Instance.OnMissChanged -= UpdateMissUI;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    // イベントの登録
    private void SubscribeEvents()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnTimeChanged -= UpdateTimeUI;
        GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
        GameManager.Instance.OnComboChanged -= UpdateComboUI;
        GameManager.Instance.OnMissChanged -= UpdateMissUI;
        GameManager.Instance.OnStateChanged -= HandleStateChanged;

        GameManager.Instance.OnTimeChanged += UpdateTimeUI;
        GameManager.Instance.OnScoreChanged += UpdateScoreUI;
        GameManager.Instance.OnComboChanged += UpdateComboUI;
        GameManager.Instance.OnMissChanged += UpdateMissUI;
        GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    // 制限時間の表示更新
    private void UpdateTimeUI(float timeValue)
    {
        if(GameManager.Instance == null || timeText == null)
            return;

        string currentMode = GameManager.Instance.CurrentMode; // 現在のモードをセット

        // タイムアタック（リアルタイム経過秒数を表示）
        if (currentMode == "TIME ATTACK")
        {
            timeText.text = $"TIME: {timeValue:F1}s";
            timeText.color = normalTimeColor;
            return;
        }

        // エンドレス（ミス表示はUpdateMissUI側で専任制御するためスルー）
        if (currentMode == "ENDLESS")
        {
            return;
        }

        // スタンダード / スコアアタック（残り時間をカウントダウン）
        int displayTime = Mathf.CeilToInt(timeValue);

        if (displayTime != _lastDisplayedTime)
        {
            _lastDisplayedTime = displayTime;
            timeText.text = $"TIME: {displayTime}";

            // 残り時間わずかで赤字化＆拡大アピール（DOTween）
            if (timeValue <= warningThreshold && timeValue > 0)
            {
                timeText.color = warningTimeColor;
                timeText.transform.DOKill();
                timeText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 1f);
            }
            else
            {
                timeText.color = normalTimeColor;
            }
        }
    }

    // エンドレスモード時のミス数の更新
    private void UpdateMissUI(int missCount)
    {
        if (GameManager.Instance == null || timeText == null)
            return;

        if (GameManager.Instance.CurrentMode == "ENDLESS")
        {
            timeText.text = $"MISS: {missCount} / 3";
            timeText.color = missCount >= 2 ? warningTimeColor : normalTimeColor;

            if (missCount > 0)
            {
                timeText.transform.DOKill();
                timeText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 5, 1f);
            }
        }
    }

    // スコアの表示更新
    private void UpdateScoreUI(int currentScore)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE: {currentScore:N0}"; // 3桁カンマ区切り表示

            // スコア加算時のパンチ演出
            scoreText.transform.DOKill();
            scoreText.transform.localScale = Vector3.one;
            scoreText.transform.DOPunchScale(Vector3.one * 0.15f, 0.15f, 10, 1f);
        }
    }

    // コンボ数の表示更新
    private void UpdateComboUI(int comboCount)
    {
        if (comboText == null)
            return;

        if (comboCount >= 2)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"{comboCount} COMBO!";

            // コンボ更新時のポップアップポップ演出
            comboText.transform.DOKill();
            comboText.transform.localScale = Vector3.one;
            comboText.transform.DOPunchScale(Vector3.one * 0.3f, 0.2f, 10, 1f);
        }
        else
        {
            comboText.gameObject.SetActive(false); // 0〜1コンボ時は非表示
        }
    }

    // ゲーム状態変更（GameOver画面表示）のポップアップ制御
    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.GameOver || state == GameState.GameClear)
        {
            ShowResultPanel(state == GameState.GameClear);
        }
    }

    // リザルト画面の表示
    private void ShowResultPanel(bool isClear)
    {
        if (resultPanel == null || GameManager.Instance == null)
            return;

        resultPanel.SetActive(true);

        string mode = GameManager.Instance.CurrentMode;
        string diff = GameManager.Instance.CurrentDifficulty;
        string saveKey = $"HighScore_{mode}_{diff}";

        if (mode == "TIME ATTACK")
        {
            float finalTime = GameManager.Instance.ElapsedTimeForTimeAttack;
            float bestTime = PlayerPrefs.GetFloat(saveKey, 9999f);

            if (finalScoreText != null)
                finalScoreText.text = $"CLEAR TIME\n{finalTime:F2} 秒";
            if (highScoreText != null)
                highScoreText.text = bestTime <= 9999f ? $"BEST TIME\n{bestTime:F2} 秒" : "BEST TIME\n--.-- 秒";
        }
        else
        {
            int finalScore = GameManager.Instance.CurrentScore;
            int highScore = PlayerPrefs.GetInt(saveKey, 0);

            if (finalScoreText != null)
                finalScoreText.text = $"FINAL SCORE\n{finalScore:N0} pt";
            if (highScoreText != null)
                highScoreText.text = $"HIGH SCORE\n{highScore:N0} pt";
        }

        CanvasGroup cg = resultPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.DOFade(1f, 0.4f).SetUpdate(true);
        }

        resultPanel.transform.localScale = Vector3.one * 0.8f;
        resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    // リスタートボタンのアクション
    private void OnRestartButtonClicked()
    {       
        DOTween.KillAll(); // アニメーション等のキル

        Time.timeScale = 1.0f; // ポーズ解除（時間の流れを戻す）

        if (showDebugLog)
            Debug.Log("<color=yellow>【リトライ】</color> シーンを再読み込みします。");

        // シーンを最初から再読み込み
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    // タイトルボタンのアクション
    private void OnTitleButtonClicked()
    {
        DOTween.KillAll();
        Time.timeScale = 1.0f;
        if (sceneNavigator != null)
        {
            sceneNavigator.LoadTitleScene();
        }
        else
        {
            SceneManager.LoadScene("Title_Windows");
        }
    }

    private void OnDestroy()
    {
        if (timeText != null)
            timeText.transform.DOKill();
        if (scoreText != null)
            scoreText.transform.DOKill();
        if (comboText != null)
            comboText.transform.DOKill();
        if (resultPanel != null)
            resultPanel.transform.DOKill();
    }
}