using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SelectManager : MonoBehaviour
{
    [SerializeField] private Text difficultyText;
    [SerializeField] private Button diffPrevButton; // 左の三角形ボタン
    [SerializeField] private Button diffNextButton; // 右の三角形ボタン

    [SerializeField] private Text modeText;
    [SerializeField] private Button modePrevButton; // 左の三角形ボタン
    [SerializeField] private Button modeNextButton; // 右の三角形ボタン

    [SerializeField] private Button startButton; // 本編ゲーム開始
    [SerializeField] private Button miniGameButton; // ミニゲーム開始（未実装用）
    [SerializeField] private Button backButton; // タイトルへ戻る

    [SerializeField] private Button ruleButton; // ルール（仕様）確認ボタン
    [SerializeField] private GameObject rulePanel; // ルール説明パネル
    [SerializeField] private Button closeRuleButton; // ルールパネル閉じるボタン

    [SerializeField] private SceneNavigator sceneNavigator;

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    // 選択肢の定義
    private string[] _difficulties = { "EASY", "NORMAL", "HARD" };
    private int _currentDiffIndex = 1; // デフォルトはNORMAL

    private string[] _modes = { "STANDARD", "TIME ATTACK", "SCORE ATTACK", "ENDLESS" };
    private int _currentModeIndex = 0; // デフォルトはSTANDARD

    private void Start()
    {
        // ボタンイベントの登録
        // 難易度切替
        if (diffPrevButton != null)
            diffPrevButton.onClick.AddListener(() => ChangeDifficulty(-1));
        if (diffNextButton != null)
            diffNextButton.onClick.AddListener(() => ChangeDifficulty(1));

        // モード切替
        if (modePrevButton != null)
            modePrevButton.onClick.AddListener(() => ChangeMode(-1));
        if (modeNextButton != null)
            modeNextButton.onClick.AddListener(() => ChangeMode(1));

        // ゲーム開始
        if (startButton != null)
            startButton.onClick.AddListener(OnStartButtonClicked);
        // ミニゲーム
        if (miniGameButton != null)
            miniGameButton.onClick.AddListener(OnMiniGameButtonClicked);
        // タイトルへ戻る
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

        // ルール確認
        if (ruleButton != null)
            ruleButton.onClick.AddListener(ToggleRulePanel);
        if (closeRuleButton != null)
            closeRuleButton.onClick.AddListener(ToggleRulePanel);

        // 初期UIの反映
        UpdateUI();

        if (showDebugLog)
            Debug.Log("<color=cyan>【セレクト画面】</color> 初期化が完了しました。");
    }

    // 難易度の切り替え
    private void ChangeDifficulty(int direction)
    {
        _currentDiffIndex += direction;
        if (_currentDiffIndex < 0)
            _currentDiffIndex = _difficulties.Length - 1;
        else if (_currentDiffIndex >= _difficulties.Length)
            _currentDiffIndex = 0;

        UpdateUI();

        // 変更時の演出
        if (difficultyText != null)
        {
            difficultyText.transform.DOKill();
            difficultyText.transform.localScale = Vector3.one;
            difficultyText.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 5, 1f);
        }

        if (showDebugLog)
            Debug.Log($"<color=yellow>【難易度変更】</color> {_difficulties[_currentDiffIndex]}");
    }

    // モードの切り替え
    private void ChangeMode(int direction)
    {
        _currentModeIndex += direction;
        if (_currentModeIndex < 0)
            _currentModeIndex = _modes.Length - 1;
        else if (_currentModeIndex >= _modes.Length)
            _currentModeIndex = 0;

        UpdateUI();

        if (modeText != null)
        {
            modeText.transform.DOKill();
            modeText.transform.localScale = Vector3.one;
            modeText.transform.DOPunchScale(Vector3.one * 0.2f, 0.15f, 5, 1f);
        }

        // TIME ATTACK と SCORE ATTACK は難易度HARD固定になる旨の自動調整・表示制御
        string selectedMode = _modes[_currentModeIndex];
        if (selectedMode == "TIME ATTACK" || selectedMode == "SCORE ATTACK")
        {
            _currentDiffIndex = 2; // HARD固定
            UpdateUI();
        }

        if (showDebugLog)
            Debug.Log($"<color=yellow>【モード変更】</color> {_modes[_currentModeIndex]}");
    }

    // UIテキストの更新
    private void UpdateUI()
    {
        string selectedMode = _modes[_currentModeIndex];

        if (difficultyText != null)
        {
            // タイム・スコアアタックはハード固定表示
            if (selectedMode == "TIME ATTACK" || selectedMode == "SCORE ATTACK")
            {
                difficultyText.text = "HARD (FIXED)";
            }
            else if (selectedMode == "ENDLESS")
            {
                difficultyText.text = "EASY ➔ HARD";
            }
            else
            {
                difficultyText.text = _difficulties[_currentDiffIndex];
            }
        }

        if (modeText != null)
            modeText.text = _modes[_currentModeIndex];
    }

    // ルール確認パネルの開閉
    private void ToggleRulePanel()
    {
        if (rulePanel == null)
            return;

        bool isActive = !rulePanel.activeSelf;
        rulePanel.SetActive(isActive);

        if (isActive)
        {
            CanvasGroup cg = rulePanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.DOFade(1f, 0.25f).SetUpdate(true);
            }
            rulePanel.transform.localScale = Vector3.one * 0.9f;
            rulePanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    // ゲーム開始ボタン
    private void OnStartButtonClicked()
    {
        string finalDiff = _difficulties[_currentDiffIndex];
        string finalMode = _modes[_currentModeIndex];

        // 仕様上の強制固定を適用
        if (finalMode == "TIME ATTACK" || finalMode == "SCORE ATTACK")
        {
            finalDiff = "HARD";
        }

        if (showDebugLog)
            Debug.Log($"<color=green>【ゲーム開始】</color> モード: {finalMode} / 難易度: {finalDiff} で開始します。");

        // 正しい難易度(finalDiff)とモード(finalMode)を保存
        PlayerPrefs.SetString("SelectedDifficulty", finalDiff);
        PlayerPrefs.SetString("SelectedMode", finalMode);
        PlayerPrefs.Save();

        if (startButton != null)
        {
            startButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f);
        }

        if (sceneNavigator != null)
        {
            sceneNavigator.LoadGameScene();
        }
    }

    // ミニゲームボタン（未実装用）
    private void OnMiniGameButtonClicked()
    {
        if (showDebugLog)
            Debug.Log("<color=orange>【ミニゲーム】</color> ミニゲームは現在未実装です。今後のアップデートをお楽しみに！");

        if (miniGameButton != null)
        {
            miniGameButton.transform.DOKill();
            miniGameButton.transform.DOShakePosition(0.2f, new Vector3(10f, 0, 0), 20, 90, false, true);
        }
    }

    // タイトルへ戻るボタン
    private void OnBackButtonClicked()
    {
        if (sceneNavigator != null)
        {
            sceneNavigator.LoadTitleScene();
        }
    }

    private void OnDestroy()
    {
        if (difficultyText != null)
            difficultyText.transform.DOKill();
        if (modeText != null)
            modeText.transform.DOKill();
    }
}