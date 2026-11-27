using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「遊び方」/「ルール」パネルのページ送り表示。
/// タイトル画面のTutorialPanel（Flow: ゲームの流れ）と
/// セレクト画面のRulePanel（Rules: 各モードの説明）で使う。
/// 文言は事実のみ、短く書く。仕様は変えない。
/// </summary>
public class HowToPlayPanel : MonoBehaviour
{
    public enum PanelKind { Flow, Rules }

    [SerializeField] private PanelKind kind = PanelKind.Flow;
    [SerializeField] private Text bodyText; // 説明文を表示するText（既存のプレースホルダーText）
    [SerializeField] private Text headingText; // 見出しを表示するText（各ページの1行目。中央寄せ）
    [SerializeField] private Button prevButton; // 「前へ」ボタン
    [SerializeField] private Button nextButton; // 「次へ」ボタン
    [SerializeField] private Text pageLabel; // 「1 / 3」などのページ表示

    // ゲームの流れ（タイトル画面、3ページ）
    private static readonly string[] FlowPages =
    {
        "1. ストックからおしぼりを取る\n左下のストックをドラッグする。",
        "2. 温度を整える\n保温庫で温める。保冷庫で冷やす。\n100℃で焦げる。0℃で固まる。",
        "3. 客に渡す\n吹き出しの温度に合わせる。\n近いほど評価が高い。\n我慢ゲージが0になると客は帰る。"
    };

    // 各モードの説明（セレクト画面、4ページ、各モード1ページ）
    private static readonly string[] RulesPages =
    {
        "STANDARD\n制限時間60秒。難易度で変わる。\nPERFECTで+2秒、ミスで-2秒。\n時間切れで終了。",
        "TIME ATTACK\n5000点でクリア。\n経過時間を計る。\n難易度はHARD固定。",
        "SCORE ATTACK\n制限時間内にできるだけ多く稼ぐ。\nPERFECTで+2秒、ミスで-2秒。\n難易度はHARD固定。",
        "ENDLESS\n時間制限なし。\nミス3回で終了。\n時間がたつほど難しくなる。"
    };

    private string[] Pages => kind == PanelKind.Rules ? RulesPages : FlowPages;

    private int _currentPage;

    private void OnEnable()
    {
        ShowPage(0);
    }

    private void Start()
    {
        if (prevButton != null)
            prevButton.onClick.AddListener(() => { AudioManager.Instance?.PlayClick(); ShowPage(_currentPage - 1); });
        if (nextButton != null)
            nextButton.onClick.AddListener(() => { AudioManager.Instance?.PlayClick(); ShowPage(_currentPage + 1); });
        ShowPage(_currentPage);
    }

    public void ShowPage(int pageIndex)
    {
        _currentPage = Mathf.Clamp(pageIndex, 0, Pages.Length - 1);

        // ページの1行目を見出し、残りを本文に分ける。
        // 見出しは中央寄せ、本文は中央に置いたブロックの中で左揃えにする。
        // headingText が未設定の場合は従来通り全文を bodyText に入れる。
        string page = Pages[_currentPage];
        if (headingText != null)
        {
            int split = page.IndexOf('\n');
            if (split >= 0)
            {
                headingText.text = page.Substring(0, split);
                if (bodyText != null)
                    bodyText.text = page.Substring(split + 1);
            }
            else
            {
                headingText.text = page;
                if (bodyText != null)
                    bodyText.text = string.Empty;
            }
        }
        else if (bodyText != null)
        {
            bodyText.text = page;
        }

        if (pageLabel != null)
            pageLabel.text = $"{_currentPage + 1} / {Pages.Length}";

        if (prevButton != null)
            prevButton.interactable = _currentPage > 0;
        if (nextButton != null)
            nextButton.interactable = _currentPage < Pages.Length - 1;
    }
}
