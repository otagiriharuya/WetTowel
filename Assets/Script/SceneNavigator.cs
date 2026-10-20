using UnityEngine;

public class SceneNavigator : MonoBehaviour
{
    // Windows版のシーン先名
    [SerializeField] private string winTitleScene = "Title_Windows";
    [SerializeField] private string winSelectScene = "Select_Windows";
    [SerializeField] private string winGameScene = "Game_Windows";

    // モバイル版のシーン先名
    [SerializeField] private string mobileTitleScene = "Title_Mobile";
    [SerializeField] private string mobileSelectScene = "Select_Mobile";
    [SerializeField] private string mobileGameScene = "Game_Mobile";

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    // タイトルシーンへ移動
    public void LoadTitleScene()
    {
        LoadScene(IsMobilePlatform() ? mobileTitleScene : winTitleScene);
    }

    // セレクトシーンへ移動
    public void LoadSelectScene()
    {
        LoadScene(IsMobilePlatform() ? mobileSelectScene : winSelectScene);
    }

    // ゲームシーンへ移動
    public void LoadGameScene()
    {
        LoadScene(IsMobilePlatform() ? mobileGameScene : winGameScene);
    }

    // 任意のシーンへロード
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            if (showDebugLog)
                Debug.LogError("<color=red>【シーン遷移エラー】</color> 遷移先のシーン名が空です。");
            return;
        }

        if (showDebugLog)
            Debug.Log($"<color=green>【シーン遷移】</color> ロード画面を挟んで '{sceneName}' へ遷移します。");

        // ロード画面シーンを経由して非同期ロードを開始
        LoadingScreenManager.LoadScene(sceneName);
    }

    // モバイルプラットフォーム判定用ヘルパー
    private bool IsMobilePlatform()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return false;
#endif
    }
}