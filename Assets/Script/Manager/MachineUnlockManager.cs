using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class MachineUnlockManager : MonoBehaviour
{
    [System.Serializable]
    public class UnlockData
    {
        public int thresholdScore; // 解放に必要なスコア
        public GameObject targetMachine; // 解放する設備
        public bool isUnlocked = false; // 解放済みフラグ
    }

    [SerializeField] private List<UnlockData> unlockSettings = new List<UnlockData>(); // 解放されている設備
    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private void Start()
    {
        // 初期化時にすべての対象設備を非アクティブに
        foreach (var setting in unlockSettings)
        {
            if (setting.targetMachine != null)
                setting.targetMachine.SetActive(false);
        }

        // GameManager のスコア変更イベント
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged += CheckUnlock;
        }
    }

    private void OnDestroy()
    {
        // メモリリーク防止のためイベントを解除
        if (GameManager.Instance != null)
            GameManager.Instance.OnScoreChanged -= CheckUnlock;
    }

    // スコア変動時に呼び出される判定処理
    private void CheckUnlock(int currentScore)
    {
        foreach (var setting in unlockSettings)
        {
            // 未解放かつ、現在のスコアが閾値を超えている場合
            if (!setting.isUnlocked && currentScore >= setting.thresholdScore)
                UnlockMachine(setting);
        }
    }

    // 設備の解放と演出処理
    private void UnlockMachine(UnlockData setting)
    {
        setting.isUnlocked = true;

        if (setting.targetMachine != null)
        {
            setting.targetMachine.SetActive(true);
            AudioManager.Instance?.PlayUnlock();

            // 登場演出（DOTweenで小さくポップアップさせる）
            Transform targetTransform = setting.targetMachine.transform;
            targetTransform.localScale = Vector3.zero;
            targetTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);

            if (showDebugLog)
                Debug.Log($"<color=magenta>【設備解放！】</color> スコア {setting.thresholdScore} 達成により '{setting.targetMachine.name}' が解放されました！");
        }
    }
}