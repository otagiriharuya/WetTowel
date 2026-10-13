using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string windowsSceneName = "MainGame_Windows";   // PC（Windows）用シーン名
    [SerializeField] private string androidSceneName = "MainGame_Mobile"; // Android用シーン名

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private void Awake()
    {
        Application.targetFrameRate = 60; // フレームの設定
    }
    private void Start()
    {
        LoadSceneBasedOnPlatform(); // プラットフォームに応じたシーンをロード
    }

    // プラットフォームに応じてロードするシーンを分ける
    private void LoadSceneBasedOnPlatform()
    {
        string targetSceneName = "";

        // 実行中のプラットフォームを判定
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        targetSceneName = windowsSceneName;
        if (showDebugLog)
            Debug.Log("<color=cyan>【プラットフォーム判定】</color> Windows環境を検知しました。");

#elif UNITY_ANDROID
        targetSceneName = androidSceneName;
        if (showDebugLog)
            Debug.Log("<color=cyan>【プラットフォーム判定】</color> Android環境を検知しました。");

#else
        // その他の環境（iOSやMacなど）のフォールバック
        targetSceneName = windowsSceneName;
        if (showDebugLog)
            Debug.LogWarning("<color=yellow>【プラットフォーム判定】</color> 未知の環境のため、デフォルトシーンをロードします。");
#endif

        // シーンが存在するか確認してロード
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            if (showDebugLog)
                Debug.Log($"<color=green>【シーンロード】</color> '{targetSceneName}' へ遷移します。");
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            if (showDebugLog)
                Debug.LogError("<color=red>【エラー】</color> ロード先のシーン名が設定されていません！");
        }
    }
}