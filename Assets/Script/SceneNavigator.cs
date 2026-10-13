using UnityEngine;
using UnityEngine.SceneManagement;

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
        string target = IsMobilePlatform() ? mobileTitleScene : winTitleScene;
        LoadScene(target);
    }

    // セレクトシーンへ移動
    public void LoadSelectScene()
    {
        string target = IsMobilePlatform() ? mobileSelectScene : winSelectScene;
        LoadScene(target);
    }

    // ゲームシーンへ移動
    public void LoadGameScene()
    {
        string target = IsMobilePlatform() ? mobileGameScene : winGameScene;
        LoadScene(target);
    }

    // 任意のシーン名へロード
    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            if (showDebugLog)
                Debug.LogError("<color=red>【シーン遷移エラー】</color> 遷移先のシーン名が空です。");
            return;
        }

        Time.timeScale = 1.0f; // 時間停止を確実に解除

        if (showDebugLog)
            Debug.Log($"<color=green>【シーン遷移】</color> '{sceneName}' へロードを開始します。");

        // 本格的なロード画面を挟む場合はここでローディングシーン経由にする拡張が可能
        SceneManager.LoadScene(sceneName);
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