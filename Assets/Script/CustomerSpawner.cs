using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject customerPrefab;  // 客のプレハブ
    [SerializeField] private Transform spawnPoint; // 客が生成される初期位置
    [SerializeField] private Transform[] counterSeats; // 客が着席するカウンター位置

    [SerializeField] private float minSpawnInterval = 3f; // 次の客が来る最小時間（秒）
    [SerializeField] private float maxSpawnInterval = 6f; // 次の客が来る最大時間（秒）

    [SerializeField] private float minTargetTemp = 20f; // 最低要求温度
    [SerializeField] private float maxTargetTemp = 85f; // 最高要求温度
    [SerializeField] private float minPatienceTime = 10f; // 最短我慢時間
    [SerializeField] private float maxPatienceTime = 20f; // 最長我慢時間

    [SerializeField] private float moveDuration = 1.2f; // 着席位置までの移動時間（秒）

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    // 各席に現在並んでいる客を管理
    private Customer[] _seatOccupants;

    private void Start()
    {
        if (counterSeats != null && counterSeats.Length > 0)
        {
            _seatOccupants = new Customer[counterSeats.Length];
        }

        StartCoroutine(SpawnLoop()); // スポーンループを開始
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            // モードや難易度に応じた動的なスポーン間隔の調整
            float currentMinInterval = minSpawnInterval;
            float currentMaxInterval = maxSpawnInterval;

            if (GameManager.Instance != null && GameManager.Instance.CurrentMode == "ENDLESS")
            {
                float progress = GameManager.Instance.GetEndlessDifficultyProgress(); // 0.0 (Easy) -> 1.0 (Hard)
                currentMinInterval = Mathf.Lerp(minSpawnInterval, minSpawnInterval * 0.5f, progress);
                currentMaxInterval = Mathf.Lerp(maxSpawnInterval, maxSpawnInterval * 0.6f, progress);
            }

            float waitTime = Random.Range(currentMinInterval, currentMaxInterval);
            yield return new WaitForSeconds(waitTime);

            // プレイ中以外は生成を中断
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            {
                continue;
            }

            // 空いている席を探す
            int emptySeatIndex = GetRandomEmptySeatIndex();
            if (emptySeatIndex != -1)
            {
                SpawnCustomer(emptySeatIndex);
            }
            else if (showDebugLog)
            {
                Debug.Log("<color=gray>[スポナー]</color> 席が満席のため客の生成をスキップしました。");
            }
        }
    }

    // ランダムに空き席のインデックスを取得
    private int GetRandomEmptySeatIndex()
    {
        List<int> emptyIndices = new List<int>();

        for (int i = 0; i < counterSeats.Length; i++)
        {
            // 参照がNull、またはDestroy済みの場合は空き席と判定
            if (_seatOccupants[i] == null)
            {
                emptyIndices.Add(i);
            }
        }

        if (emptyIndices.Count == 0)
            return -1;

        return emptyIndices[Random.Range(0, emptyIndices.Count)];
    }

    // 客の生成と移動
    private void SpawnCustomer(int seatIndex)
    {
        if (customerPrefab == null || spawnPoint == null)
            return;

        Vector3 spawnPos = spawnPoint.position;
        Transform targetSeat = counterSeats[seatIndex];

        // 客インスタンスを生成
        GameObject customerObj = Instantiate(customerPrefab, spawnPos, Quaternion.identity);
        Customer customer = customerObj.GetComponent<Customer>();

        if (customer != null)
        {
            _seatOccupants[seatIndex] = customer;

            // パラメーターのランダム設定
            float randomTemp = Random.Range(minTargetTemp, maxTargetTemp);
            float patience = Random.Range(minPatienceTime, maxPatienceTime);

            // エンドレスモードの場合は進捗に応じて我慢時間を短縮（EASY -> HARD）
            if (GameManager.Instance != null && GameManager.Instance.CurrentMode == "ENDLESS")
            {
                float progress = GameManager.Instance.GetEndlessDifficultyProgress();
                patience = Mathf.Lerp(maxPatienceTime, minPatienceTime * 0.7f, progress);
            }

            customer.InitializeCustomer(randomTemp, patience);

            customer.SetMovingState(true); // 移動開始：移動中フラグをTrueにしてゲージ減少をストップ

            // DOTweenでカウンター位置へ移動
            customer.transform.DOMove(targetSeat.position, moveDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    // 着席完了：移動中フラグをFalseにしてゲージ減少＆提供受付をスタート
                    if (customer != null)
                    {
                        customer.SetMovingState(false);
                    }
                });

            if (showDebugLog)
                Debug.Log($"<color=cyan>【客の来店】</color> 席[{seatIndex}] に移動開始（移動時間: {moveDuration}秒）");
        }
    }
}