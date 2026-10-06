using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class DraggableItem : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float returnDuration = 0.25f; // 元の位置またはストックへ戻る時間
    [SerializeField] private float dropDetectRadius = 0.5f; // ドロップ先を検知する半径

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private Vector3 _dragOffset; // タッチ位置とオブジェクト中心の位置ズレ
    private Vector3 _startPosition; // ドラッグ開始時の初期位置
    private Vector3 _stockSpawnPosition; // 生成元のストック位置
    private DropTarget _currentMountedTarget; // 現在このおしぼりがセットされている機械枠
    private Camera _mainCamera;

    private Sequence _activeSequence; // DOTweenの一連のアニメーションを安全に一元管理するシーケンス
    private bool _isSpawnedFromStock = false; // ストックから生成された直後（未設置）の状態か

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null && showDebugLog)
            Debug.LogError($"[{gameObject.name}] Main Camera が見つかりません。Camera の Tag を 'MainCamera' に設定してください。");
    }

    // スポナーからの生成時の初期パラメータ設定
    public void SetSpawnedFromStock(bool isSpawned, Vector3 stockPosition)
    {
        _isSpawnedFromStock = isSpawned;
        _stockSpawnPosition = stockPosition;
    }

    // クリック/タップ時のイベント
    public void OnPointerDown(PointerEventData eventData)
    {
        eventData.Use();
    }

    // ドラッグ開始処理
    public void OnBeginDrag(PointerEventData eventData)
    {
        KillActiveSequence(); // 実行中のアニメーションを停止

        _startPosition = transform.position; // 初期位置を記憶

        // タッチ位置をワールド座標に変換して、クリック位置と中心のズレを計算
        Vector3 mouseWorldPos = GetMouseWorldPosition(eventData.position);
        _dragOffset = transform.position - mouseWorldPos;

        // すでに機械枠にセットされていた場合は、枠の占有を解除（温度変化などを停止）
        if (_currentMountedTarget != null)
        {
            _currentMountedTarget.ReleaseWetTowel();
            _currentMountedTarget = null;
        }

        if (showDebugLog)
            Debug.Log($"<color=cyan>[ドラッグ開始]</color> {gameObject.name} | 開始位置: {_startPosition}");
    }

    // ドラッグ中処理
    public void OnDrag(PointerEventData eventData)
    {
        // 現在のタッチ位置にオフセットを加えてオブジェクトの位置を更新
        Vector3 currentMouseWorldPos = GetMouseWorldPosition(eventData.position);
        transform.position = currentMouseWorldPos + _dragOffset;
    }

    // ドラッグ終了処理
    public void OnEndDrag(PointerEventData eventData)
    {
        if (showDebugLog)
            Debug.Log($"<color=yellow>【ドラッグ終了】</color> {gameObject.name} | 離した位置: {transform.position}");

        // 離した位置の周辺にある DropTarget を検索（自身のColliderは除外）
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, dropDetectRadius);
        DropTarget target = null;

        foreach (var hit in hits)
        {
            if (hit.gameObject != gameObject)
            {
                target = hit.GetComponent<DropTarget>();
                if (target != null)
                    break;
            }
        }

        bool success = false;
        if (target != null)
        {
            WetTowel item = GetComponent<WetTowel>();
            if (item != null)
            {
                success = target.TryAcceptWetTowel(item); // ターゲットへのドロップを試みる
                // ドロップに成功した場合、新しい枠の参照を保持する（ゴミ箱や客以外）
                if (success)
                {
                    _currentMountedTarget = target;
                    _isSpawnedFromStock = false; // ドロップ成功でフラグ解除
                }

            }
        }

        // ドロップ失敗（何もない場所で離した / 枠が埋まっていた）場合の復帰・破棄処理
        if (!success)
        {
            HandleDropFailure();
        }
    }

    // ドロップ失敗時の挙動制御
    private void HandleDropFailure()
    {
        KillActiveSequence();
        _activeSequence = DOTween.Sequence();

        if (_isSpawnedFromStock)
        {
            if (showDebugLog)
                Debug.Log($"<color=red>【生成キャンセル】</color> {gameObject.name} をストック位置へ戻して削除します。");

            // キャンセル時はスポナーの元の中心位置に吸い込まれるように消滅
            _activeSequence.Append(transform.DOMove(_stockSpawnPosition, returnDuration).SetEase(Ease.OutCubic))
                           .Append(transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack))
                           .OnComplete(() => Destroy(gameObject));
        }
        else
        {
            if (showDebugLog)
                Debug.Log($"<color=orange>【位置リセット】</color> {gameObject.name} を元の枠へ戻します。");

            // 位置リセット時は元の位置に戻す
            _activeSequence.Append(transform.DOMove(_startPosition, returnDuration).SetEase(Ease.OutCubic))
                           .OnComplete(() =>
                           {
                               // 元の場所にあったDropTargetを再取得して再セット
                               Collider2D[] startHits = Physics2D.OverlapCircleAll(_startPosition, 0.2f);
                               foreach (var hit in startHits)
                               {
                                   if (hit.gameObject == gameObject)
                                       continue;
                                   DropTarget startTarget = hit.GetComponent<DropTarget>();
                                   if (startTarget != null)
                                   {
                                       WetTowel item = GetComponent<WetTowel>();
                                       if (item != null)
                                       {
                                           startTarget.TryAcceptWetTowel(item);
                                           _currentMountedTarget = startTarget;
                                           break;
                                       }
                                   }
                               }
                           });
        }
    }

    // 画面のスクリーン座標（ピクセル）をゲーム空間のワールド座標へ変換する処理
    private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
    {
        if (_mainCamera == null)
            return transform.position;

        // カメラの奥行き（Z軸）を考慮して2D空間上の位置を計算
        Vector3 point = new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(_mainCamera.transform.position.z));
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(point);
        worldPos.z = transform.position.z; // Z軸（奥行き）を維持
        return worldPos;
    }

    // 実行中のシーケンスを安全にキルする
    private void KillActiveSequence()
    {
        if (_activeSequence != null && _activeSequence.IsActive())
        {
            _activeSequence.Kill();
            _activeSequence = null;
        }
    }

    private void OnDestroy()
    {
        KillActiveSequence(); // メモリリーク防止のため破棄時にアニメーションをキル
    }
}