using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class WetTowelStockSpawner : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private GameObject wetTowelPrefab; // 生成するおしぼりのプレハブ
    [SerializeField] private Transform spawnParent; // 生成先
    [SerializeField] private float spawnZOffset = -0.1f; // ドラッグ中に前面へ表示するためのZ軸オフセット

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private Camera _mainCamera;
    private GameObject _currentDraggedObject;
    private DraggableItem _currentDraggableItem;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    // ストック部分でドラッグが開始された瞬間
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (wetTowelPrefab == null)
        {
            if (showDebugLog)
                Debug.LogError($"[{gameObject.name}] WetTowel Prefab が設定されていません。");
            return;
        }

        // タッチ座標を取得し、Z軸を手前にずらして生成
        Vector3 spawnPos = GetMouseWorldPosition(eventData.position);
        spawnPos.z = transform.position.z + spawnZOffset;

        _currentDraggedObject = Instantiate(wetTowelPrefab, spawnPos, Quaternion.identity, spawnParent);
        _currentDraggableItem = _currentDraggedObject.GetComponent<DraggableItem>();

        if (_currentDraggableItem != null)
        {
            // 出現演出
            _currentDraggedObject.transform.localScale = Vector3.zero;
            _currentDraggedObject.transform.DOScale(0.5f, 0.2f).SetEase(Ease.OutBack);

            _currentDraggableItem.SetSpawnedFromStock(true, transform.position); // ストック生成フラグ・戻り座標を設定

            _currentDraggableItem.OnBeginDrag(eventData); // おしぼり側のドラッグ開始処理

            if (showDebugLog)
                Debug.Log($"<color=cyan>【ストックドラッグ開始】</color> おしぼりを生成し、ドラッグを開始しました。");
        }
    }

    // ストックからのドラッグ中
    public void OnDrag(PointerEventData eventData)
    {
        if (_currentDraggableItem != null)
            _currentDraggableItem.OnDrag(eventData);
    }

    // ストックからのドラッグ終了
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_currentDraggableItem != null)
        {
            _currentDraggableItem.OnEndDrag(eventData);

            // 参照をクリア（以降の操作はおしぼり自身が受付）
            _currentDraggedObject = null;
            _currentDraggableItem = null;
        }
    }

    // 画面のスクリーン座標（ピクセル）をゲーム空間のワールド座標へ変換する処理
    private Vector3 GetMouseWorldPosition(Vector2 screenPosition)
    {
        if (_mainCamera == null)
            return transform.position;

        Vector3 point = new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(_mainCamera.transform.position.z));
        return _mainCamera.ScreenToWorldPoint(point);
    }
}