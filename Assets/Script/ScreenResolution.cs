using UnityEngine;

public class ScreenResolution : MonoBehaviour
{
    [SerializeField] private int windowsWidth = 1920;
    [SerializeField] private int windowsHeight = 1080;
    [SerializeField] private bool isWinFullScreen = false; // Windows環境でフルスクリーンか

    [SerializeField] private int androidWidth = 1080;
    [SerializeField] private int androidHeight = 1920;

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private void Awake()
    {
        //DontDestroyOnLoad(gameObject);// シーンを跨いでも破棄されないようにする場合

        ApplyPlatformResolution(); // 解像度設定
    }

    // プラットフォームに応じた解像度設定
    private void ApplyPlatformResolution()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        // Windows環境：16:9 (例: 1920x1080)
        Screen.SetResolution(windowsWidth, windowsHeight, isWinFullScreen);

        if (showDebugLog)
            Debug.Log($"<color=cyan>【解像度設定】</color> Windows向けに解像度を変更しました ({windowsWidth}x{windowsHeight})");

#elif UNITY_ANDROID
        // Android環境：縦画面 (1080x1920)
        // モバイルの場合は自動回転を縦向きに固定しつつ解像度を指定
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.SetResolution(androidWidth, androidHeight, true);

        if (showDebugLog)
            Debug.Log($"<color=cyan>【解像度設定】</color> Android向け縦画面に解像度を変更しました ({androidWidth}x{androidHeight})");
#else
        // その他（エディターでの確認用など、デフォルトを16:9等にする場合）
        Screen.SetResolution(windowsWidth, windowsHeight, isWinFullScreen);
        if (showDebugLog)
            Debug.Log($"<color=yellow>【解像度設定】</color> その他の環境のためデフォルト解像度を適用しました。");
#endif
    }
}