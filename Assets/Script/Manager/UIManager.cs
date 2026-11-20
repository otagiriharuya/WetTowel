using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Text timeText; // 制限時間テキスト
    [SerializeField] private Text scoreText; // スコアテキスト
    [SerializeField] private Text comboText; // コンボテキスト

    [SerializeField] private GameObject centerPopupPanel; // ポップアップ全体のルートパネル
    [SerializeField] private Text popupEvalText; // 評価テキスト
    [SerializeField] private Text popupBonusText; // ボーナス率テキスト
    [SerializeField] private Text popupScoreText; // 獲得スコアテキスト

    [SerializeField] private GameObject resultPanel; // リザルト全体パネル
    [SerializeField] private Text resultTitleText; // リザルトタイトル
    [SerializeField] private Text detailStatsText; // 詳細集計用テキスト（パーフェクト数、ミス数、コンボ数など）
    [SerializeField] private Text finalScoreText; // 最終スコアテキスト
    [SerializeField] private Text highScoreText; // ハイスコアテキスト
    [SerializeField] private Transform starContainer;  // 星（★）を並べる親トランスフォーム
    [SerializeField] private GameObject starIconPrefab;// 星のUI
    [SerializeField] private Text totalStarsCountText; // 星獲得総数テキスト
    [SerializeField] private Button restartButton; // リスタートボタン
    [SerializeField] private Button titleButton; // タイトルへ戻るボタン

    [SerializeField] private Color normalTimeColor = Color.white; // 通常時のタイマー文字色
    [SerializeField] private Color warningTimeColor = Color.red; // 残り時間わずか（10秒以下）の文字色
    [SerializeField] private float warningThreshold = 10f; // 警告演出を始める秒数

    [SerializeField] private SceneNavigator sceneNavigator;

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private int _lastDisplayedTime = -1; // 不要なテキスト更新を減らすための保持変数
    private Tween _popupTween; // ポップアップアニメーションの重複制御用

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
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

        // 非表示スタート
        if (comboText != null)
            comboText.gameObject.SetActive(false);
        if (centerPopupPanel != null)
            centerPopupPanel.SetActive(false);

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

    // おしぼり提供時に画面中央へド派手にポップアップを表示する
    public void ShowEvaluationPopup(Vector3 worldPosition, CustomerEvaluation eval, int bonusPercent, int finalScore)
    {
        if (centerPopupPanel == null)
            return;

        // テキストの流し込み
        if (popupEvalText != null)
        {
            switch (eval)
            {
                case CustomerEvaluation.Perfect:
                    popupEvalText.text = "PERFECT!!";
                    popupEvalText.color = Color.yellow;
                    break;
                case CustomerEvaluation.Great:
                    popupEvalText.text = "GREAT!";
                    popupEvalText.color = Color.green;
                    break;
                case CustomerEvaluation.Good:
                    popupEvalText.text = "GOOD";
                    popupEvalText.color = Color.cyan;
                    break;
                case CustomerEvaluation.Miss:
                    popupEvalText.text = "MISS...";
                    popupEvalText.color = Color.red;
                    break;
            }
        }

        if (popupBonusText != null)
        {
            popupBonusText.text = $"BONUS: {bonusPercent}%";
        }

        if (popupScoreText != null)
        {
            popupScoreText.text = finalScore >= 0 ? $"+{finalScore:N0} pt" : $"{finalScore:N0} pt";
            popupScoreText.color = finalScore >= 0 ? Color.white : Color.red;
        }

        // 既存のポップアップ演出アニメーションがあればキルして初期化
        _popupTween?.Kill();
        centerPopupPanel.SetActive(true);
        centerPopupPanel.transform.localScale = Vector3.zero;

        // ド派手なポップアップ＆フェードアウト演出（DOTween）
        Sequence popSeq = DOTween.Sequence();
        popSeq.Append(centerPopupPanel.transform.DOScale(Vector3.one * 1.2f, 0.15f).SetEase(Ease.OutBack))
              .Append(centerPopupPanel.transform.DOScale(Vector3.one, 0.1f))
              .AppendInterval(0.6f) // 表示を維持する時間
              .Append(centerPopupPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
              .OnComplete(() => centerPopupPanel.SetActive(false));

        _popupTween = popSeq;

        if (showDebugLog)
            Debug.Log($"<color=magenta>【画面中央演出】</color> 判定: {eval} | ボーナス: {bonusPercent}% | 獲得スコア: {finalScore}pt");
    }

    // ゲーム状態変更（GameOver画面表示）のポップアップ制御
    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.GameOver || state == GameState.GameClear)
        {
            ShowResultPanel(state == GameState.GameClear);
        }
    }

    // モード別専用リザルト画面の組み立てと表示
    private void ShowResultPanel(bool isClear)
    {
        if (resultPanel == null || GameManager.Instance == null)
            return;

        resultPanel.SetActive(true);

        string mode = GameManager.Instance.CurrentMode;
        string diff = GameManager.Instance.CurrentDifficulty;
        string saveKey = $"HighScore_{mode}_{diff}";

        // リザルトタイトルの設定
        if (resultTitleText != null)
            resultTitleText.text = $"{mode} RESULT";

        // 星コンテナや合計スターテキストの初期化
        if (starContainer != null)
            starContainer.gameObject.SetActive(false);
        if (totalStarsCountText != null)
            totalStarsCountText.gameObject.SetActive(false);

        // モードごとの表示分岐
        if (mode == "TIME ATTACK")
        {
            float finalTime = GameManager.Instance.ElapsedTimeForTimeAttack;
            float bestTime = PlayerPrefs.GetFloat(saveKey, 9999f);

            if (finalScoreText != null)
                finalScoreText.text = $"CLEAR TIME\n{finalTime:F2} 秒";
            if (highScoreText != null)
                highScoreText.text = bestTime <= 9999f ? $"BEST TIME\n{bestTime:F2} 秒" : "BEST TIME\n--.-- 秒";
        }
        else if (mode == "STANDARD")
        {
            int pCount = GameManager.Instance.PerfectCount;
            int gCount = GameManager.Instance.GreatCount;
            int gdCount = GameManager.Instance.GoodCount;
            int mCount = GameManager.Instance.StandardMissCount;
            int maxCombo = GameManager.Instance.MaxComboCount;
            int score = GameManager.Instance.CurrentScore;

            // 詳細な接客結果テキスト
            if (detailStatsText != null)
            {
                detailStatsText.text =
                    $"PERFECT : {pCount} 人\n" +
                    $"GREAT   : {gCount} 人\n" +
                    $"GOOD    : {gdCount} 人\n" +
                    $"MISS    : {mCount} 人\n" +
                    $"COMBO   : {maxCombo} コンボ";
            }

            if (finalScoreText != null)
                finalScoreText.text = $"TOTAL SCORE\n{score:N0} pt";

            int highScore = PlayerPrefs.GetInt(saveKey, 0);
            if (highScoreText != null)
                highScoreText.text = $"HIGH SCORE\n{highScore:N0} pt";

            // スタンダードモード特有の星評価演出を表示
            if (starContainer != null)
                starContainer.gameObject.SetActive(true);
            if (totalStarsCountText != null)
                totalStarsCountText.gameObject.SetActive(true);

            GenerateRatingStars(pCount, gCount, maxCombo, mCount);
        }
        else // SCORE ATTACK / ENDLESS など
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

    // 星の個数算出と「×個数」の表示、グリッドレイアウトへの動的生成
    private void GenerateRatingStars(int perfects, int greats, int maxCombo, int misses)
    {
        if (starContainer == null || starIconPrefab == null)
            return;

        // 既存の星をクリア
        foreach (Transform child in starContainer)
        {
            Destroy(child.gameObject);
        }

        // パーフェクト数、グッド数、最大コンボ数に応じて星が増殖！ミスで少し減点
        int totalStars = Mathf.Max(1, (perfects * 2) + greats + (maxCombo / 2) - (misses * 2));

        // 星の上や近くに「⭐ × 〇〇」として合計個数を表示
        if (totalStarsCountText != null)
        {
            totalStarsCountText.text = $"× {totalStars}";

            // カウントアップするようなポップ演出
            totalStarsCountText.transform.localScale = Vector3.zero;
            totalStarsCountText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        // 1列10個ずつ綺麗に並べる
        for (int i = 0; i < totalStars; i++)
        {
            GameObject starObj = Instantiate(starIconPrefab, starContainer);
            starObj.SetActive(true);

            // ポコポコと時間差で飛び出してくる気持ちいいアニメーション
            starObj.transform.localScale = Vector3.zero;
            starObj.transform.DOScale(Vector3.one, 0.25f)
                   .SetDelay(0.1f + (i * 0.03f))
                   .SetEase(Ease.OutBack)
                   .SetUpdate(true);
        }
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