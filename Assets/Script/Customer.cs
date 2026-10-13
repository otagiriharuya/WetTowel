using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// 提供時の評価定義
public enum CustomerEvaluation
{
    Perfect, // 誤差 ±5以内
    Great, // 誤差 ±15以内
    Good, // 誤差 ±30以内
    Miss // 誤差 大 または 失敗作（焦げ・カチコチ）
}

public class Customer : MonoBehaviour
{
    [SerializeField] private float targetTemperature = 75f; // 要求温度（0〜100）
    [SerializeField] private float maxPatienceTime = 15f; // 我慢できる最大時間（秒）

    [SerializeField] private float perfectThreshold = 5f; // ±5以内
    [SerializeField] private float greatThreshold = 15f; // ±15以内
    [SerializeField] private float goodThreshold = 30f; // ±30以内

    [SerializeField] private Image patienceBarImage; // ゲージ表示用のUI
    [SerializeField] private Text tempText; // 要求温度を表示するテキスト（例: "75℃" や "極熱"）
    [SerializeField] private Image tempIconSprite; // 要求温度の吹き出しやアイコンのスプライト

    [SerializeField] private Color coldTempColor = Color.cyan; // 極冷（0℃付近）
    [SerializeField] private Color normalTempColor = Color.white; // 常温（50℃付近）
    [SerializeField] private Color hotTempColor = Color.red; // 極熱（100℃付近

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private float _currentPatienceTime; // 現在の残り我慢時間
    private bool _isServed = false; // すでに提供済みか
    private bool _isMoving = false; // 移動中フラグ

    public float TargetTemperature => targetTemperature;
    public float PatienceRatio => Mathf.Clamp01(_currentPatienceTime / maxPatienceTime); // 我慢ゲージ割合 (0.0〜1.0)
    public bool IsServed => _isServed;
    public bool IsMoving => _isMoving;

    private void Awake()
    {
        _currentPatienceTime = maxPatienceTime;
        UpdatePatienceUI();
    }

    private void Update()
    {
        // 提出済み、移動中は我慢ゲージを減らさない
        if (_isServed || _isMoving)
            return;

        // プレイ中以外は我慢ゲージを減らさない
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Playing)
            return;

        // 時間経過で我慢ゲージを減算
        _currentPatienceTime -= Time.deltaTime;
        UpdatePatienceUI();

        // 我慢限界（ゲージ0）になった時の処理
        if (_currentPatienceTime <= 0f)
        {
            OnPatienceTimeout();
        }
    }

    // 移動状態の切り替え
    public void SetMovingState(bool isMoving)
    {
        _isMoving = isMoving;
    }

    // 我慢ゲージUIの表示更新
    private void UpdatePatienceUI()
    {
        if (patienceBarImage != null)
        {
            patienceBarImage.fillAmount = PatienceRatio;

            // ゲージが減るにつれて緑→黄→赤へ色を変化させる
            if (PatienceRatio > 0.5f)
            {
                patienceBarImage.color = Color.Lerp(Color.yellow, Color.green, (PatienceRatio - 0.5f) * 2f);
            }
            else
            {
                patienceBarImage.color = Color.Lerp(Color.red, Color.yellow, PatienceRatio * 2f);
            }
        }
    }

    // 客に初期化パラメータをセットする処理
    public void InitializeCustomer(float targetTemp, float patienceTime)
    {
        targetTemperature = Mathf.Clamp(targetTemp, 0f, 100f);
        maxPatienceTime = patienceTime;
        _currentPatienceTime = maxPatienceTime;
        _isServed = false;

        UpdatePatienceUI();
        UpdateTargetTempVisual(); // 要求温度の見た目を更新

        if (showDebugLog)
            Debug.Log($"<color=cyan>【客登場】</color> 要求温度: {targetTemperature:F1} ℃ | 我慢時間: {maxPatienceTime} 秒");
    }

    // 要求温度のテキスト・アイコン色を自動更新するメソッド
    private void UpdateTargetTempVisual()
    {
        // テキスト（数値）の表示更新
        if (tempText != null)
        {
            tempText.text = $"{Mathf.RoundToInt(targetTemperature)}℃";
        }

        // 温度に応じたグラデーションカラーの計算
        Color calculatedColor;
        if (targetTemperature < 50f)
        {
            float t = targetTemperature / 50f;
            calculatedColor = Color.Lerp(coldTempColor, normalTempColor, t);
        }
        else
        {
            float t = (targetTemperature - 50f) / 50f;
            calculatedColor = Color.Lerp(normalTempColor, hotTempColor, t);
        }

        // アイコンや吹き出しの色を変えて直感的にする
        if (tempIconSprite != null)
        {
            tempIconSprite.color = calculatedColor;
        }
        if (tempText != null)
        {
            tempText.color = Color.black;
        }
    }

    // おしぼりを受け取った時の判定・処理
    public void ServeWetTowel(WetTowel wetTowel)
    {
        // 提出済み、移動中は受取らない
        if (_isServed || _isMoving)
            return;

        _isServed = true;

        // おしぼりの消滅演出
        if (wetTowel != null)
        {
            wetTowel.transform.DOKill();
            // 客の位置へ移動しながら縮小
            Sequence towelSeq = DOTween.Sequence();
            towelSeq.Append(wetTowel.transform.DOMove(transform.position, 0.15f).SetEase(Ease.OutQuad))
                    .Join(wetTowel.transform.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack))
                    .OnComplete(() =>
                    {
                        if (wetTowel != null)
                        {
                            Destroy(wetTowel.gameObject); // ここでおしぼりを削除
                        }
                    });
        }

        // 失敗作チェック（焦げ・カチコチ）
        if (wetTowel.CurrentState != WetTowelState.Normal)
        {
            ProcessEvaluation(CustomerEvaluation.Miss, 0, -200, -2.0f);
            PlayExitAnimation(false);
            return;
        }

        // 温度の誤差計算
        float tempDiff = Mathf.Abs(targetTemperature - wetTowel.CurrentTemperature);
        CustomerEvaluation eval;
        int baseScore = 0;

        if (tempDiff <= perfectThreshold)
        {
            eval = CustomerEvaluation.Perfect;
            baseScore = 1000;
        }
        else if (tempDiff <= greatThreshold)
        {
            eval = CustomerEvaluation.Great;
            baseScore = 600;
        }
        else if (tempDiff <= goodThreshold)
        {
            eval = CustomerEvaluation.Good;
            baseScore = 300;
        }
        else
        {
            eval = CustomerEvaluation.Miss;
            baseScore = 0;
        }

        // 我慢ゲージ残量によるスコア倍率計算（仕様通り）
        float multiplier = 1.0f;
        if (PatienceRatio >= 0.7f)
            multiplier = 1.5f; // 素早い提供ボーナス
        else if (PatienceRatio < 0.3f)
            multiplier = 0.8f; // ギリギリ提供補正

        int finalScore = Mathf.RoundToInt(baseScore * multiplier);
        float timeChange = (eval == CustomerEvaluation.Perfect) ? 1.0f : ((eval == CustomerEvaluation.Miss) ? -2.0f : 0f);

        // 結果のログ出力＆処理
        ProcessEvaluation(eval, baseScore, finalScore, timeChange);
        PlayExitAnimation(eval != CustomerEvaluation.Miss);
    }

    // 評価結果の処理
    private void ProcessEvaluation(CustomerEvaluation eval, int baseScore, int finalScore, float timeChange)
    {
        if (showDebugLog)
        {
            string color = eval == CustomerEvaluation.Perfect ? "yellow" : (eval == CustomerEvaluation.Miss ? "red" : "green");
            Debug.Log($"<color={color}>【提供評価】</color> 判定: {eval} | 基礎点: {baseScore} pt | 倍率補正後: {finalScore} pt | 時間変化: {timeChange:F1} 秒 | 我慢残量: {PatienceRatio * 100:F0}%");
        }

        // GameManagerへスコアと判定を通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(finalScore, eval);
        }
    }

    // タイムアウト怒り退場
    private void OnPatienceTimeout()
    {
        if (_isServed)
            return;

        _isServed = true;

        if (showDebugLog)
            Debug.LogWarning($"<color=red>【怒り退場】</color> 客の我慢限界！ スコア: -500 pt | 時間: -5.0 秒");

        // GameManagerへペナルティ通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyAngerPenalty();
        }

        PlayExitAnimation(false);
    }

    // 退場アニメーション（DOTween）
    private void PlayExitAnimation(bool isHappy)
    {
        transform.DOKill();

        if (isHappy)
        {
            // 満足して退場（少し上にジャンプしてから消滅）
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOJump(transform.position, 0.5f, 1, 0.3f))
               .Append(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
               .OnComplete(() => Destroy(gameObject));
        }
        else
        {
            // 不満・怒り退場（左右に揺れてから消滅）
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOShakePosition(0.3f, new Vector3(0.2f, 0, 0), 20, 90, false, true))
               .Append(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
               .OnComplete(() => Destroy(gameObject));
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}