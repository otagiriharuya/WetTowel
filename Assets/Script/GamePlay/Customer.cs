using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

// おしぼり提供時の評価定義
public enum CustomerEvaluation
{
    // 誤差は難易度により推移
    Perfect, // 誤差 極小
    Great, // 誤差 小
    Good, // 誤差 中
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

    [SerializeField] private SpriteRenderer bodyRenderer; // 客の絵（MainUnit）
    [SerializeField] private Sprite normalSprite; // 通常の表情
    [SerializeField] private Sprite happySprite; // 笑顔
    [SerializeField] private Sprite angrySprite; // 怒り顔
    [SerializeField] private Sprite sparkleSprite; // PERFECT時のキラキラ

    [SerializeField] private Color coldTempColor = Color.cyan; // 極冷（0℃付近）
    [SerializeField] private Color normalTempColor = Color.white; // 常温（50℃付近）
    [SerializeField] private Color hotTempColor = Color.red; // 極熱（100℃付近）

    [SerializeField] private bool showDebugLog = true; // デバッグログの表示切り替え

    private float _currentPatienceTime; // 現在の残り我慢時間
    private bool _isServed = false; // すでに提供済みか
    private bool _isMoving = false; // 移動中フラグ

    // 難易度によって調整された動的判定閾値
    private float _appliedPerfectThreshold;
    private float _appliedGreatThreshold;
    private float _appliedGoodThreshold;
    private float _difficultyScoreMultiplier = 1.0f; // 難易度倍率

    public float TargetTemperature => targetTemperature;
    public float PatienceRatio => Mathf.Clamp01(_currentPatienceTime / maxPatienceTime); // 我慢ゲージ割合 (0.0〜1.0)
    public bool IsServed => _isServed;
    public bool IsMoving => _isMoving;

    private void Awake()
    {
        _currentPatienceTime = maxPatienceTime;
        ApplyDifficultySettings(); // 難易度に応じた判定閾値の調整
        UpdatePatienceUI();
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bodyRenderer != null && normalSprite != null)
            bodyRenderer.sprite = normalSprite;
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
            OnPatienceTimeout();
    }

    // 難易度設定に応じた判定閾値を動的に変更
    private void ApplyDifficultySettings()
    {
        string diff = GameManager.Instance != null ? GameManager.Instance.CurrentDifficulty : "NORMAL";

        switch (diff)
        {
            case "EASY":
                _appliedPerfectThreshold = perfectThreshold * 1.6f; // 判定がゆるい (例: ±8)
                _appliedGreatThreshold = greatThreshold * 1.5f; // (例: ±22.5)
                _appliedGoodThreshold = goodThreshold * 1.3f; // (例: ±39)
                _difficultyScoreMultiplier = 0.8f; // スコア加算控えめ
                break;
            case "HARD":
                _appliedPerfectThreshold = perfectThreshold * 0.6f; // 判定が厳しい (例: ±3)
                _appliedGreatThreshold = greatThreshold * 0.7f; // (例: ±10.5)
                _appliedGoodThreshold = goodThreshold * 0.8f; // (例: ±24)
                _difficultyScoreMultiplier = 1.5f; // スコア加算大幅
                break;
            default: // NORMAL
                _appliedPerfectThreshold = perfectThreshold;
                _appliedGreatThreshold = greatThreshold;
                _appliedGoodThreshold = goodThreshold;
                _difficultyScoreMultiplier = 1.0f;
                break;
        }
    }

    // 移動状態の設定
    public void SetMovingState(bool isMoving)
    {
        _isMoving = isMoving;
    }

    // 我慢ゲージUIの量および色を更新
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

    // 客の要求パラメータ（要求温度・我慢時間）を初期化
    public void InitializeCustomer(float targetTemp, float patienceTime)
    {
        targetTemperature = Mathf.Clamp(targetTemp, 0f, 100f);
        maxPatienceTime = patienceTime;
        _currentPatienceTime = maxPatienceTime;
        _isServed = false;

        ApplyDifficultySettings(); // 難易度再確認・反映
        UpdatePatienceUI();
        UpdateTargetTempVisual(); // 要求温度の見た目を更新

        if (showDebugLog)
            Debug.Log($"<color=cyan>【客登場】</color> 要求温度: {targetTemperature:F1} ℃ | 我慢時間: {maxPatienceTime} 秒");
    }

    // 要求温度のテキストおよびアイコンカラーを更新
    private void UpdateTargetTempVisual()
    {
        // テキスト（数値）の表示更新
        if (tempText != null)
            tempText.text = $"{Mathf.RoundToInt(targetTemperature)}℃";

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
            tempIconSprite.color = calculatedColor;
        if (tempText != null)
            tempText.color = Color.black;
    }

    // 提供されたおしぼりを受け取り、評価判定・スコア計算
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
                            Destroy(wetTowel.gameObject); // ここでおしぼりを削除
                    });
        }

        // 失敗作チェック（焦げ・カチコチ）
        if (wetTowel.CurrentState != WetTowelState.Normal)
        {
            SetExpression(angrySprite);
            ProcessEvaluation(CustomerEvaluation.Miss, 0, 0, -200);
            PlayExitAnimation(false);
            return;
        }

        // 温度の誤差計算
        float tempDiff = Mathf.Abs(targetTemperature - wetTowel.CurrentTemperature);
        CustomerEvaluation eval;
        int baseScore = 0;

        if (tempDiff <= _appliedPerfectThreshold)
        {
            eval = CustomerEvaluation.Perfect;
            baseScore = 1000;
        }
        else if (tempDiff <= _appliedGreatThreshold)
        {
            eval = CustomerEvaluation.Great;
            baseScore = 600;
        }
        else if (tempDiff <= _appliedGoodThreshold)
        {
            eval = CustomerEvaluation.Good;
            baseScore = 300;
        }
        else
        {
            eval = CustomerEvaluation.Miss;
            baseScore = 0;
        }

        // 我慢ゲージ残量によるスコア倍率計算
        float timeMultiplier = 1.0f;
        if (PatienceRatio >= 0.7f)
        {
            timeMultiplier = 2.0f; // 素早い提供
        }
        else if (PatienceRatio < 0.3f)
        {
            timeMultiplier = 0.8f; // ギリギリ提供補正
        }

        // コンボ倍率の計算
        // 現在のコンボ数に応じて倍率を加算
        float comboMultiplier = 1.0f;
        if (GameManager.Instance != null && GameManager.Instance.ComboCount > 0)
        {
            float bonusPerCombo = 0.2f; // 1コンボにつき +20% 加算
            comboMultiplier = Mathf.Min(20.0f, 1.0f + (GameManager.Instance.ComboCount * bonusPerCombo));
        }

        // 総合ボーナス率
        float totalMultiplier = timeMultiplier * _difficultyScoreMultiplier * comboMultiplier;
        int totalBonusPercent = Mathf.RoundToInt(totalMultiplier * 100f);

        int finalScore = Mathf.RoundToInt(baseScore * totalMultiplier);

        // 結果のログ出力＆処理
        SetExpression(eval == CustomerEvaluation.Miss ? angrySprite : happySprite);
        ProcessEvaluation(eval, baseScore, totalBonusPercent, finalScore);
        PlayExitAnimation(eval != CustomerEvaluation.Miss);
    }

    // 評価結果をログ出力し、GameManagerへ通知
    private void ProcessEvaluation(CustomerEvaluation eval, int baseScore, int bonusPercent, int finalScore)
    {
        if (eval == CustomerEvaluation.Perfect)
            SpawnSparkles();
        if (showDebugLog)
        {
            string color = eval == CustomerEvaluation.Perfect ? "yellow" : (eval == CustomerEvaluation.Miss ? "red" : "green");
            Debug.Log($"<color={color}>【提供評価】</color> 判定: {eval} | 基礎点: {baseScore} pt | ボーナス率: {bonusPercent}% | 最終スコア: {finalScore} pt");
        }

        // 画面上のUIへ結果を通知してプレイヤーに見せる
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowEvaluationPopup(transform.position, eval, bonusPercent, finalScore);
        }

        // GameManagerへスコアと判定を通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(finalScore, eval);
        }
    }

    // 表情を切り替える（見た目だけ。仕様は変えない）
    private void SetExpression(Sprite sprite)
    {
        if (bodyRenderer != null && sprite != null)
            bodyRenderer.sprite = sprite;
    }

    // PERFECT時のキラキラ演出（見た目だけ）
    private void SpawnSparkles()
    {
        if (sparkleSprite == null) return;
        for (int i = 0; i < 8; i++)
        {
            GameObject sparkle = new GameObject("Sparkle", typeof(SpriteRenderer));
            sparkle.transform.position = transform.position + new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.5f, 1.0f), 0f);
            SpriteRenderer sr = sparkle.GetComponent<SpriteRenderer>();
            sr.sprite = sparkleSprite;
            sr.sortingOrder = 10;
            sparkle.transform.localScale = Vector3.zero;
            Vector3 target = sparkle.transform.position + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.5f, 1.2f), 0f);
            Sequence seq = DOTween.Sequence();
            seq.Append(sparkle.transform.DOScale(Vector3.one * Random.Range(0.25f, 0.45f), 0.15f).SetEase(Ease.OutBack))
               .Join(sparkle.transform.DOMove(target, 0.5f).SetEase(Ease.OutQuad))
               .Append(sparkle.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack))
               .OnComplete(() => Destroy(sparkle));
        }
    }

    // 我慢限界による退場処理
    private void OnPatienceTimeout()
    {
        if (_isServed)
            return;

        _isServed = true;

        if (showDebugLog)
            Debug.LogWarning($"<color=red>【怒り退場】</color> 客の我慢限界！ スコア: -500 pt");

        // GameManagerへペナルティ通知
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ApplyAngerPenalty();
        }

        SetExpression(angrySprite);
        PlayExitAnimation(false);
    }

    // 満足・怒りに応じた退場アニメーションを再生
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