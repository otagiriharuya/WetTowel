using UnityEngine;
using UnityEngine.SceneManagement;

public class InitApp : MonoBehaviour
{
    [SerializeField] private int targetFrameRate = 60; // フレームレート設定

    // Windows(横画面)設定
    [SerializeField] private int windowsWidth = 1920;
    [SerializeField] private int windowsHeight = 1080;
    [SerializeField] private bool isWinFullScreen = false;
    [SerializeField] private string windowsFirstScene = "Title_Windows"; // 移行先のタイトルシーン

    // Mobile(縦画面)設定
    [SerializeField] private int androidWidth = 1080;
    [SerializeField] private int androidHeight = 1920;
    [SerializeField] private string androidFirstScene = "Title_Mobile"; // 移行先のタイトルシーン

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private void Awake()
    {
        // 複数生成防止
        Application.targetFrameRate = targetFrameRate;
        ApplyPlatformSettings(); // 解像度・画面向きの設定
    }

    private void Start()
    {
        LoadFirstScene(); // 最初のシーンへ遷移
    }

    // プラットフォームに応じた解像度・画面向きの設定
    private void ApplyPlatformSettings()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        Screen.SetResolution(windowsWidth, windowsHeight, isWinFullScreen);
        if (showDebugLog)
            Debug.Log($"<color=cyan>【初期化】</color> Windows向け解像度を適用 ({windowsWidth}x{windowsHeight})");

#elif UNITY_ANDROID || UNITY_IOS
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.SetResolution(androidWidth, androidHeight, true);
        if (showDebugLog)
            Debug.Log($"<color=cyan>【初期化】</color> モバイル向け解像度を適用 ({androidWidth}x{androidHeight})");
#else
        Screen.SetResolution(windowsWidth, windowsHeight, isWinFullScreen);
#endif
    }

    // プラットフォームに応じた最初のシーンへ遷移
    private void LoadFirstScene()
    {
        string targetScene = "";

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        targetScene = windowsFirstScene;
#elif UNITY_ANDROID || UNITY_IOS
        targetScene = androidFirstScene;
#else
        targetScene = windowsFirstScene;
#endif

        if (!string.IsNullOrEmpty(targetScene))
        {
            if (showDebugLog)
                Debug.Log($"<color=green>【初期化】</color> プラットフォームに合致した最初のシーン '{targetScene}' をロードします。");
            SceneManager.LoadScene(targetScene);
        }
        else
        {
            Debug.LogError("<color=red>【エラー】</color> 初期ロード先のシーン名が設定されていません！");
        }
    }
}