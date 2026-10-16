using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TitleManager : MonoBehaviour
{
    [SerializeField] private Button startButton; // ゲームスタートボタン
    [SerializeField] private Button tutorialButton; // チュートリアルボタン
    [SerializeField] private Button exitButton; // ゲーム終了ボタン
    [SerializeField] private GameObject tutorialPanel; // チュートリアル表示用パネル
    [SerializeField] private Button closeTutorialButton; // チュートリアル閉じるボタン
    [SerializeField] private Transform titleLogoTransform; // タイトルロゴのトランスフォーム（演出用）

    [SerializeField] private SceneNavigator sceneNavigator; // シーン遷移コントローラー

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private void Start()
    {
        // チュートリアルパネルは非表示
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        // ボタンイベントの登録
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OnTutorialButtonClicked);
        }
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        if (closeTutorialButton != null)
        {
            closeTutorialButton.onClick.AddListener(OnCloseTutorialButtonClicked);
        }

        // タイトルロゴの登場演出（DOTween）
        if (titleLogoTransform != null)
        {
            titleLogoTransform.localScale = Vector3.zero;
            titleLogoTransform.DOScale(Vector3.one, 0.6f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (showDebugLog)
            Debug.Log("<color=cyan>【タイトル画面】</color> 初期化が完了しました。");
    }

    private void OnStartButtonClicked()
    {
        if (showDebugLog)
            Debug.Log("<color=yellow>【タイトル】</color> スタートボタンが押されました。セレクトシーンへ遷移します。");

        // ボタンクリック時のフィードバック演出を挟んでから遷移
        if (startButton != null)
        {
            startButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f);
        }

        if (sceneNavigator != null)
        {
            // セレクトシーンへ遷移（セレクトを挟まない場合は直接 LoadGameScene() でも可）
            sceneNavigator.LoadSelectScene();
        }
    }

    private void OnTutorialButtonClicked()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            CanvasGroup cg = tutorialPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.DOFade(1f, 0.3f);
            }
            if (showDebugLog)
                Debug.Log("<color=cyan>【チュートリアル】</color> パネルを開きました。");
        }
    }

    private void OnExitButtonClicked()
    {
        if (exitButton != null)
        {
            exitButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 10, 1f);
        }

        if (showDebugLog)
            Debug.Log("<color=red>【ゲーム終了】</color> アプリケーションを終了します。");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Unityエディター上での再生停止
#else
        Application.Quit(); // 実機（ビルド後）でのアプリ終了
#endif
    }

    private void OnCloseTutorialButtonClicked()
    {
        if (tutorialPanel != null)
        {
            CanvasGroup cg = tutorialPanel.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.DOFade(0f, 0.2f).OnComplete(() => tutorialPanel.SetActive(false));
            }
            else
            {
                tutorialPanel.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (titleLogoTransform != null)
        {
            titleLogoTransform.DOKill();
        }
    }
}