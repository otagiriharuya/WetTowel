using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class PlatformLayoutAdjuster : MonoBehaviour
{
    // Windows版の設定
    [SerializeField] private Vector2 winReferenceResolution = new Vector2(1920, 1080);
    [SerializeField] private float winMatchValue = 0.5f;

    // モバイル版の設定
    [SerializeField] private Vector2 mobileReferenceResolution = new Vector2(1080, 1920);
    [SerializeField] private float mobileMatchValue = 1.0f;

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private CanvasScaler _canvasScaler;

    private void Awake()
    {
        _canvasScaler = GetComponent<CanvasScaler>();
        ApplyLayout(); // レイアウト設定
    }

    // プラットフォームに応じたレイアウトの設定
    private void ApplyLayout()
    {
        if (_canvasScaler == null)
            return;

        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        _canvasScaler.referenceResolution = winReferenceResolution;
        _canvasScaler.matchWidthOrHeight = winMatchValue;

        if (showDebugLog)
            Debug.Log($"<color=cyan>【レイアウト調整】</color> Windows向け（横画面）解像度基準を適用しました。({winReferenceResolution.x}x{winReferenceResolution.y})");

#elif UNITY_ANDROID || UNITY_IOS
        _canvasScaler.referenceResolution = mobileReferenceResolution;
        _canvasScaler.matchWidthOrHeight = mobileMatchValue;

        if (showDebugLog)
            Debug.Log($"<color=cyan>【レイアウト調整】</color> Mobile向け（縦画面）解像度基準を適用しました。({mobileReferenceResolution.x}x{mobileReferenceResolution.y})");
#else
        // デフォルト（エディター等のフォールバック）
        _canvasScaler.referenceResolution = winReferenceResolution;
        _canvasScaler.matchWidthOrHeight = winMatchValue;
#endif
    }
}