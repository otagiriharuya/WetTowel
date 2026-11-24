using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム開始時のチュートリアル（初回のみ表示、スキップ可）。
/// 既存のゲーム進行には干渉しないよう、表示中は時間を止めるだけにする。
/// 2回目以降は表示せず、画面右上の「？」ボタンからいつでも再表示できる。
/// </summary>
public class GameTutorial: MonoBehaviour
{
public const string TutorialDoneKey = "WetTowel_TutorialDone";

[SerializeField] private GameObject panel; // チュートリアル表示用パネル全体
[SerializeField] private Text stepText; // 手順文を表示するText
[SerializeField] private Button nextButton; // 「次へ」ボタン
[SerializeField] private Button skipButton; // 「スキップ」ボタン
[SerializeField] private Text stepLabel; // 「1 / 5」などの進行表示
[SerializeField] private Button helpButton; // ゲーム画面の「？」ボタン（再表示用）

// おしぼり提供の一連の流れ（既存ルールの説明のみ）
// Round7：カウンターの下にスロットが並ぶ新しい配置に合わせた短い事実文
private static readonly string[] Steps =
{
"おしぼりを取る\nカウンターの下、左端のタオルの山からドラッグする。",
"温度を整える\n湯気の保温庫で温める。雪の保冷庫で冷やす。\n100℃で焦げる。0℃で固まる。",
"要求温度を見る\n客の頭上の吹き出しの温度に合わせる。\n近いほど評価が高い。",
"客に渡す\n客へドラッグする。\nPERFECTだと残り時間が+2秒される。",
"ゲージに気をつける\n客の頭の横のゲージが0になると客は帰る。\n焦げ・カチコチのおしぼりはゴミ箱へ。"
};

private int _currentStep;
private bool _isShowing;

/// <summary>チュートリアル表示中かどうか。オーバーレイ自体は常時アクティブのため、activeSelfでは判定しない。</summary>
public bool IsShowing => _isShowing;

private void Start()
{
if (nextButton!= null)
nextButton.onClick.AddListener(OnNextButtonClicked);
if (skipButton!= null)
skipButton.onClick.AddListener(FinishTutorial);
if (helpButton!= null)
helpButton.onClick.AddListener(ShowTutorial);

// 注意: panel がこのGameObject自身の場合、SetActive(false)で
// 自身が非アクティブになり、直後のコルーチンが停止して初回表示が出ない。
// そのため表示/非表示は SetPanelVisible() に集約する。
SetPanelVisible(false);

// 初回プレイ時のみチュートリアルを表示する
if (PlayerPrefs.GetInt(TutorialDoneKey, 0) == 0)
StartCoroutine(ShowAfterFirstFrame());
}

private IEnumerator ShowAfterFirstFrame()
{
// GameManager.Start() などの初期化が終わってから表示する
yield return null;
ShowTutorial();
}

/// <summary>チュートリアルを表示する（「？」ボタンからも呼ぶ）。</summary>
public void ShowTutorial()
{
// プレイ中でなければ開かない（結果画面などで時間を戻してしまわないため）
if (GameManager.Instance!= null && GameManager.Instance.CurrentState!= GameState.Playing)
return;

_isShowing = true;
_currentStep = 0;
ShowStep(_currentStep);
Time.timeScale = 0f;
}

private void ShowStep(int stepIndex)
{
_currentStep = Mathf.Clamp(stepIndex, 0, Steps.Length - 1);

SetPanelVisible(true);

if (stepText!= null)
stepText.text = Steps[_currentStep];

if (stepLabel!= null)
stepLabel.text = $"{_currentStep + 1} / {Steps.Length}";

if (nextButton!= null)
{
Text label = nextButton.GetComponentInChildren<Text>();
if (label!= null)
label.text = _currentStep >= Steps.Length - 1? "はじめる": "次へ";
}
}

private void OnNextButtonClicked()
{
if (_currentStep >= Steps.Length - 1)
{
FinishTutorial();
return;
}
ShowStep(_currentStep + 1);
}

/// <summary>チュートリアルを閉じてプレイを再開する。</summary>
public void FinishTutorial()
{
_isShowing = false;

SetPanelVisible(false);

// プレイ中なら時間を再開する（結果画面で止まっている場合は触らない）
if (GameManager.Instance == null || GameManager.Instance.CurrentState == GameState.Playing)
Time.timeScale = 1f;

PlayerPrefs.SetInt(TutorialDoneKey, 1);
PlayerPrefs.Save();
}

/// <summary>
/// パネルの表示/非表示。panel がこのGameObject自身の場合は
/// GameObjectを非アクティブにしない（コルーチン停止・FindObjectOfType失敗の原因になるため）。
/// 代わりに暗転Imageと子オブジェクト（TutorialBox）だけを切り替える。
/// </summary>
private void SetPanelVisible(bool visible)
{
if (panel == null)
return;
if (panel != gameObject)
{
panel.SetActive(visible);
return;
}
Image dim = GetComponent<Image>();
if (dim != null)
dim.enabled = visible;
foreach (Transform child in panel.transform)
child.gameObject.SetActive(visible);
}
}
