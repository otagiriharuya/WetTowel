using UnityEngine;
using UnityEngine.InputSystem;

// おしぼりの状態定義
public enum WetTowelState
{
    Normal, // 通常
    Burnt, // 焦げ（加熱しすぎ）
    Frozen // カチコチ（冷却しすぎ）
}

public class WetTowel : MonoBehaviour
{
    [SerializeField] private float currentTemperature = 50f; // 現在の温度（0〜100）
    [SerializeField] private WetTowelState currentState = WetTowelState.Normal; // 現在の状態

    [SerializeField] private SpriteRenderer spriteRenderer; // スプライト描画コンポーネント
    [SerializeField] private Color coldColor = Color.cyan; // 極冷時のマスク色（0）
    [SerializeField] private Color normalColor = Color.white; // 常温時の基本色（50）
    [SerializeField] private Color hotColor = Color.red; // 極熱時のマスク色（100）
    [SerializeField] private Color burntColor = Color.black; // 焦げ時の色（100超え）
    [SerializeField] private Color frozenColor = Color.blue; // カチコチ時の色（0未満）

    [SerializeField] private bool showDebugLog = true;  // デバッグログの表示切り替え

    // 外部から温度や状態を取得するためのプロパティ
    public float CurrentTemperature => currentTemperature;
    public WetTowelState CurrentState => currentState;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // 開発テスト用：キーボード操作で手動で温度を変えてテスト可能
#if UNITY_EDITOR
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.upArrowKey.isPressed)
                ChangeTemperature(20f * Time.deltaTime);
            if (keyboard.downArrowKey.isPressed)
                ChangeTemperature(-20f * Time.deltaTime);
        }
#endif
    }

    // 温度を加算・減算し、色や状態を自動更新する
    public void ChangeTemperature(float delta)
    {
        if (currentState != WetTowelState.Normal)
            return; // 既に失敗作の場合は温度変化を停止

        currentTemperature = Mathf.Clamp(currentTemperature + delta, 0f, 100f);
        UpdateStateAndVisual();
    }

    // 温度に応じた状態とビジュアル（色）を更新
    private void UpdateStateAndVisual()
    {
        // 限界値による状態遷移チェック
        if (currentTemperature >= 100f)
        {
            currentState = WetTowelState.Burnt;
            if (showDebugLog)
                Debug.LogWarning($"<color=red>【状態変更】</color> {gameObject.name} が焦げました！");
        }
        else if (currentTemperature <= 0f)
        {
            currentState = WetTowelState.Frozen;
            if (showDebugLog)
                Debug.LogWarning($"<color=blue>【状態変更】</color> {gameObject.name} がカチコチになりました！");
        }

        // 見た目（カラー）の更新
        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case WetTowelState.Burnt:
                spriteRenderer.color = burntColor;
                break;
            case WetTowelState.Frozen:
                spriteRenderer.color = frozenColor;
                break;
            case WetTowelState.Normal:
                // 温度 50 を基準に青→白→赤へグラデーション変化
                if (currentTemperature < 50f)
                {
                    // 0 (coldColor) 〜 50 (normalColor/白)
                    float t = currentTemperature / 50f;
                    spriteRenderer.color = Color.Lerp(coldColor, normalColor, t);
                }
                else
                {
                    // 50 (normalColor/白) 〜 100 (hotColor)
                    float t = (currentTemperature - 50f) / 50f;
                    spriteRenderer.color = Color.Lerp(normalColor, hotColor, t);
                }
                break;
        }
    }
}