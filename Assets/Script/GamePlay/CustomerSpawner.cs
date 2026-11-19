using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class CustomerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject customerPrefab;  // 客のプレハブ
    [SerializeField] private Transform spawnPoint; // 客が生成される初期位置
    [SerializeField] private Transform[] counterSeats; // 客が着席するカウンター位置

    [SerializeField] private float baseMinSpawnInterval = 3f; // 次の客が来る最小時間（秒）
    [SerializeField] private float baseMaxSpawnInterval = 6f; // 次の客が来る最大時間（秒）
    [SerializeField] private float baseMoveDuration = 1.2f; // 移動時間（秒）

    [SerializeField] private float minTargetTemp = 20f; // 最低要求温度
    [SerializeField] private float maxTargetTemp = 85f; // 最高要求温度
    [SerializeField] private float minPatienceTime = 10f; // 最短我慢時間
    [SerializeField] private float maxPatienceTime = 20f; // 最長我慢時間

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private Customer[] _seatOccupants; // 各席に現在並んでいる客を管理
    private bool _isFirstSpawn = true; // 最初の生成かどうかを管理するフラグ

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
            float waitTime;

            // 最初の生成時は待ち時間をほぼゼロ（0.2秒〜0.5秒程度）にしてすぐに客を呼び出す
            if (_isFirstSpawn)
            {
                waitTime = Random.Range(0.2f, 0.5f);
                _isFirstSpawn = false;
            }
            else
            {
                // 難易度やモードに応じた動的なスポーン間隔の調整
                float intervalMultiplier = GetDifficultyIntervalMultiplier();
                float currentMinInterval = baseMinSpawnInterval * intervalMultiplier;
                float currentMaxInterval = baseMaxSpawnInterval * intervalMultiplier;

                // エンドレスモードの追加補正
                if (GameManager.Instance != null && GameManager.Instance.CurrentMode == "ENDLESS")
                {
                    float progress = GameManager.Instance.GetEndlessDifficultyProgress(); // 0.0 -> 1.0
                    currentMinInterval = Mathf.Lerp(currentMinInterval, currentMinInterval * 0.5f, progress);
                    currentMaxInterval = Mathf.Lerp(currentMaxInterval, currentMaxInterval * 0.6f, progress);
                }

                waitTime = Random.Range(currentMinInterval, currentMaxInterval);
            }

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

    // 難易度に応じた生成間隔の倍率を取得
    private float GetDifficultyIntervalMultiplier()
    {
        if (GameManager.Instance == null)
            return 1.0f;

        string diff = GameManager.Instance.CurrentDifficulty;
        if (diff == "EASY")
            return 1.3f; // 生成間隔を少し広げる（ゆっくり）
        if (diff == "HARD")
            return 0.7f; // 生成間隔を短くする（ハイペース）
        return 1.0f; // NORMAL
    }

    // 難易度に応じた移動時間の倍率を取得（HARDほど移動速度が速い＝時間が短い）
    private float GetDifficultyMoveDuration()
    {
        if (GameManager.Instance == null)
            return baseMoveDuration;

        string diff = GameManager.Instance.CurrentDifficulty;
        if (diff == "EASY")
            return baseMoveDuration * 1.2f;
        if (diff == "HARD")
            return baseMoveDuration * 0.75f;
        return baseMoveDuration;
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

            float moveDuration = GetDifficultyMoveDuration(); // 難易度に応じた移動速度（duration）を適用

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