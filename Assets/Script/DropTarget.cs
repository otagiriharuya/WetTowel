using UnityEngine;
using DG.Tweening;

// ドロップ先エリアの種類
public enum TargetType
{
    WarmerSlot, // 保温庫枠
    CoolerSlot, // 保冷庫枠
    Customer, // 客
    TrashCan // ゴミ箱
}

public class DropTarget : MonoBehaviour
{
    [SerializeField] private TargetType targetType = TargetType.WarmerSlot;
    [SerializeField] private float temperatureChangeRate = 10f; // 1秒あたりの温度変化量（＋または−）
    [SerializeField] private float snapDuration = 0.15f; // スナップにかかる時間

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private WetTowel _mountedWetTowel; // 現在枠にセットされているおしぼり

    public bool IsOccupied => _mountedWetTowel != null; // 枠が埋まっているか

    private void Update()
    {
        // 機械におしぼりがセットされている場合、温度を加算/減算
        if (_mountedWetTowel != null)
        {
            if (targetType == TargetType.WarmerSlot)
            {
                _mountedWetTowel.ChangeTemperature(temperatureChangeRate * Time.deltaTime);
            }
            else if (targetType == TargetType.CoolerSlot)
            {
                _mountedWetTowel.ChangeTemperature(-temperatureChangeRate * Time.deltaTime);
            }
        }
    }

    // おしぼりがドロップされた時の受け入れ判定
    public bool TryAcceptWetTowel(WetTowel wetTowel)
    {
        // 既に他の品がある場合は受け入れ不可（ゴミ箱以外）
        if (IsOccupied && targetType != TargetType.TrashCan)
        {
            if (showDebugLog)
                Debug.Log($"<color=gray>[受け入れ不可]</color> {gameObject.name} はすでに埋まっています。");
            return false;
        }

        switch (targetType)
        {
            case TargetType.WarmerSlot:
            case TargetType.CoolerSlot:
                SetWetTowelToSlot(wetTowel);
                return true;

            case TargetType.TrashCan:
                if (showDebugLog)
                    Debug.Log($"<color=red>【破棄】</color> {wetTowel.gameObject.name} をゴミ箱へ捨てました。");
                // 捨てられた時の演出（縮小して消滅）
                wetTowel.transform.DOKill();
                wetTowel.transform.DOScale(Vector3.zero, 0.2f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => Destroy(wetTowel.gameObject));
                return true;

            case TargetType.Customer:
                if (showDebugLog)
                    Debug.Log($"<color=green>【提供】</color> {wetTowel.gameObject.name} (温度:{wetTowel.CurrentTemperature:F1}) を客へ渡しました。");

                Customer customer = GetComponent<Customer>();
                if (customer != null)
                {
                    customer.ServeWetTowel(wetTowel); // 客側で評価・スコア計算・消滅演出を実行
                }
                else
                {
                    wetTowel.transform.DOKill();
                    wetTowel.transform.DOScale(Vector3.one * 1.3f, 0.15f)
                        .OnComplete(() => Destroy(wetTowel.gameObject));
                }
                return true;

            default:
                return false;
        }
    }

    // おしぼりを枠の中心へセット
    private void SetWetTowelToSlot(WetTowel wetTowel)
    {
        _mountedWetTowel = wetTowel;

        wetTowel.transform.DOKill();
        transform.DOKill();
        wetTowel.transform.DOMove(transform.position, snapDuration).SetEase(Ease.OutQuad); // 枠の中心へ滑らかにスナップ移動

        // 「カチッ」とはまったような打撃感演出（パンチスケール）
        transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0), 0.15f, 10, 1f);

        if (showDebugLog)
            Debug.Log($"<color=green>【セット完了】</color> {wetTowel.gameObject.name} を {gameObject.name} にセットしました。");
    }

    // おしぼりが枠から取り出された時のクリア処理
    public void ReleaseWetTowel()
    {
        if (_mountedWetTowel != null)
        {
            _mountedWetTowel = null;
            if (showDebugLog)
                Debug.Log($"<color=gray>[枠解放]</color> {gameObject.name} の枠が空きました。");
        }
    }
}