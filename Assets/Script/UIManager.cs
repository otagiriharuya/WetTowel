using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text timeText; // 制限時間テキスト
    [SerializeField] private Text scoreText; // スコアテキスト
    [SerializeField] private Text comboText; // コンボテキスト

    [SerializeField] private GameObject timeUpPanel; // TimeUp画面全体の親オブジェクト
    [SerializeField] private Text finalScoreText; // 最終スコアテキスト
    [SerializeField] private Text highScoreText; // ハイスコアテキスト
    [SerializeField] private Button restartButton; // リスタートボタン

    [SerializeField] private Color normalTimeColor = Color.white; // 通常時のタイマー文字色
    [SerializeField] private Color warningTimeColor = Color.red; // 残り時間わずか（10秒以下）の文字色
    [SerializeField] private float warningThreshold = 10f; // 警告演出を始める秒数

    [SerializeField] private bool showDebugLog = true;

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
        // OnEnable時点でGameManagerが未準備だった場合のフォールバック登録
        SubscribeEvents();

        // 初期UI状態のセットアップ
        if (timeUpPanel != null)
        {
            timeUpPanel.SetActive(false);
        }

        if (comboText != null)
        {
            comboText.gameObject.SetActive(false); // コンボ非表示からスタート
        }

        // リスタートボタンのクリックイベント設定
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
    }

    private void OnDisable()
    {
        // メモリリーク防止のためイベント解除
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged -= UpdateTimeUI;
            GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
            GameManager.Instance.OnComboChanged -= UpdateComboUI;
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
        }
    }

    // イベントの二重登録を防ぎつつ購読
    private void SubscribeEvents()
    {
        if (GameManager.Instance == null)
            return;

        GameManager.Instance.OnTimeChanged -= UpdateTimeUI;
        GameManager.Instance.OnScoreChanged -= UpdateScoreUI;
        GameManager.Instance.OnComboChanged -= UpdateComboUI;
        GameManager.Instance.OnStateChanged -= HandleStateChanged;

        GameManager.Instance.OnTimeChanged += UpdateTimeUI;
        GameManager.Instance.OnScoreChanged += UpdateScoreUI;
        GameManager.Instance.OnComboChanged += UpdateComboUI;
        GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    // 制限時間の表示更新
    private void UpdateTimeUI(float currentTime)
    {
        int displayTime = Mathf.CeilToInt(currentTime); // 小数点繰り上げ表示

        // 秒数が変わったタイミングのみテキストを更新
        if (displayTime != _lastDisplayedTime)
        {
            _lastDisplayedTime = displayTime;

            if (timeText != null)
            {
                timeText.text = $"TIME: {displayTime}";

                // 残り時間わずかで赤字化＆拡大アピール（DOTween）
                if (currentTime <= warningThreshold && currentTime > 0)
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
        if (state == GameState.GameOver)
        {
            ShowGameOverPanel();
        }
    }

    // プレイ中UIの一括表示 / 非表示切り替え
    private void SetInGameUIActive(bool isActive)
    {
        if (timeText != null)
            timeText.gameObject.SetActive(isActive);
        if (scoreText != null)
            scoreText.gameObject.SetActive(isActive);
        // comboTextはコンボ数に応じた表示制御があるため非表示時のみ直接切る
        if (!isActive && comboText != null)
            comboText.gameObject.SetActive(false);
    }

    // GameOverパネル表示演出
    private void ShowGameOverPanel()
    {
        if (timeUpPanel == null)
            return;

        SetInGameUIActive(false);
        timeUpPanel.SetActive(true);

        int finalScore = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;
        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        if (finalScoreText != null)
            finalScoreText.text = $"FINAL SCORE\n{finalScore:N0}";
        if (highScoreText != null)
            highScoreText.text = $"HIGH SCORE\n{highScore:N0}";

        // ふんわり浮き出るフェードイン＆拡大表示（DOTween）
        CanvasGroup canvasGroup = timeUpPanel.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.4f).SetUpdate(true);
        }

        timeUpPanel.transform.localScale = Vector3.one * 0.8f;
        timeUpPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

        if (showDebugLog)
            Debug.Log($"<color=red>【UI表示】</color> GameOverパネルを表示しました。（最終スコア: {finalScore}）");
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

    private void OnDestroy()
    {
        if (timeText != null)
            timeText.transform.DOKill();
        if (scoreText != null)
            scoreText.transform.DOKill();
        if (comboText != null)
            comboText.transform.DOKill();
        if (timeUpPanel != null)
            timeUpPanel.transform.DOKill();
    }
}