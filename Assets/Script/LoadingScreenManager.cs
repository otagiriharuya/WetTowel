using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using DG.Tweening;

public class LoadingScreenManager : MonoBehaviour
{
    private static string _nextSceneName; // 遷移する予定のシーン名

    [SerializeField] private static string winLoadingSceneName = "Loading_Windows";
    [SerializeField] private static string mobileLoadingSceneName = "Loading_Mobile";

    [SerializeField] private CanvasGroup canvasGroup; // フェード用のCanvasGroup
    [SerializeField] private Slider progressBar; // 進行度を示すスライダー (0〜1)
    [SerializeField] private Text progressText; // パーセンテージ表示テキスト (例: "50%")

    [SerializeField] private float fadeDuration = 0.3f; // フェードにかかる時間
    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    // 外部（SceneNavigator等）から最初に呼ばれる入口
    public static void LoadScene(string sceneName)
    {
        _nextSceneName = sceneName;
        Time.timeScale = 1.0f; // 時間停止を解除

        // プラットフォームに応じてロードシーンを切り替える
        bool isMobile = IsMobilePlatform();
        string loadingScene = isMobile ? mobileLoadingSceneName : winLoadingSceneName;

        if (Debug.isDebugBuild || Application.isEditor)
        {
            Debug.Log($"<color=cyan>【ロード管理】</color> プラットフォーム判定: {(isMobile ? "Mobile" : "Windows")} ➔ ロードシーン '{loadingScene}' を経由します。");
        }

        SceneManager.LoadScene(loadingScene); // ロード専用シーンへ一旦遷移する
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(_nextSceneName))
        {
            if (showDebugLog)
                Debug.LogWarning("<color=yellow>【ロード】</color> 次のシーン名が指定されていないため、タイトルへ戻ります。");
            _nextSceneName = "Title_Windows"; // フォールバック
        }

        StartCoroutine(LoadSceneRoutine(_nextSceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        // フェードイン
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            yield return canvasGroup.DOFade(1f, fadeDuration).SetUpdate(true).WaitForCompletion();
        }

        // 非同期で目的のシーンをロード
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        float displayProgress = 0f;

        while (!asyncOperation.isDone)
        {
            float targetProgress = Mathf.Clamp01(asyncOperation.progress / 0.9f);
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.unscaledDeltaTime * 2f);

            UpdateProgressUI(displayProgress);

            if (asyncOperation.progress >= 0.9f && displayProgress >= 0.99f)
            {
                displayProgress = 1f;
                UpdateProgressUI(displayProgress);

                yield return new WaitForSecondsRealtime(0.3f);
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void UpdateProgressUI(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = progress;
        }

        if (progressText != null)
        {
            progressText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }
    }

    private static bool IsMobilePlatform()
    {
#if UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return false;
#endif
    }
}