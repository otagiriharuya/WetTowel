using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「遊び方」/「ルール」パネルのページ送り表示。
/// タイトル画面のTutorialPanelとセレクト画面のRulePanelの両方で使う。
/// 内容は既存のゲームルールの説明のみで、仕様は変えない。
/// </summary>
public class HowToPlayPanel: MonoBehaviour
{
[SerializeField] private Text bodyText; // 説明文を表示するText（既存のプレースホルダーText）
[SerializeField] private Button prevButton; // 「前へ」ボタン
[SerializeField] private Button nextButton; // 「次へ」ボタン
[SerializeField] private Text pageLabel; // 「1 / 4」などのページ表示

// 遊び方の各ページ（既存ルールの説明のみ）
private static readonly string[] Pages =
{
"\nお客さんに、ちょうどいい温度の\nおしぼりを届けよう！\n\n要求温度に近いほど高評価。\nPERFECTならスコアも時間も\nボーナスがもらえるよ！",
"おしぼりを取る\n左下のストックをドラッグ！\nおしぼりが出てくるよ。\n\nドラッグしたまま動かして、\n好きな場所へ運ぼう。",
"温度を調整する\n赤い保温庫に置くと温まる、\n青い保冷庫に置くと冷えるよ。\n\n100℃で焦げちゃう、\n0℃でカチコチになるので注意！\n失敗作はゴミ箱へ捨てよう。",
"お客さんへ提供\nお客さんの吹き出しの温度を\nチェック！\n\nおしぼりを客までドラッグして\n渡そう。温度が近いほど高評価！\n\n我慢ゲージが0になると\n客は怒って帰っちゃうよ。"
};

private int _currentPage;

private void OnEnable()
{
ShowPage(0);
}

private void Start()
{
if (prevButton!= null)
prevButton.onClick.AddListener(() => ShowPage(_currentPage - 1));
if (nextButton!= null)
nextButton.onClick.AddListener(() => ShowPage(_currentPage + 1));
ShowPage(_currentPage);
}

public void ShowPage(int pageIndex)
{
_currentPage = Mathf.Clamp(pageIndex, 0, Pages.Length - 1);

if (bodyText!= null)
bodyText.text = Pages[_currentPage];

if (pageLabel!= null)
pageLabel.text = $"{_currentPage + 1} / {Pages.Length}";

if (prevButton!= null)
prevButton.interactable = _currentPage > 0;
if (nextButton!= null)
nextButton.interactable = _currentPage < Pages.Length - 1;
}
}
