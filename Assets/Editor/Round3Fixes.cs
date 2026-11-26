using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 第3弾修正（2026-11-20、星・タイトル・中央寄せ）。
public static class Round3Fixes
{
    // ---- 第3弾修正（2026-11-20、星・タイトル・中央寄せ） ----
    // Windows版シーンのみ。モバイル版シーンは触らない。
    // - タイトルを「おしぼり道」にする（Title_Windows の TitleLogo 子Text）
    // - リザルトの星（StarContainer / TotalStarsCountText）を右へずらし、
    //   TOTAL SCORE との重なりをなくす（星の左端 450、TOTAL SCORE の右端 410 と40px空ける）
    // - 端にあって読みづらかった文を中央寄せにする：
    //   遊び方・ルールの本文、チュートリアルの StepText、
    //   リザルトの DetailStatsText / FinalScoreText / HighScoreText
    [MenuItem("WetTowel/Apply Round3 Fixes")]
    public static void ApplyRound3Fixes()
    {
        Debug.Log("[WetTowel] Round3 fixes を開始します。");

        // タイトルロゴと遊び方本文（タイトル画面）
        Scene titleScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Title_Windows.unity", OpenSceneMode.Single);
        GameObject titleLogo = SetupHelpers.FindInScene(titleScene, "TitleLogo");
        if (titleLogo != null)
        {
            Text logoText = titleLogo.GetComponentInChildren<Text>(true);
            if (logoText != null)
            {
                logoText.text = "おしぼり道";
                Debug.Log("[WetTowel] タイトルロゴを「おしぼり道」にしました。");
            }
            else Debug.LogError("[WetTowel] TitleLogo の子Textが見つかりません。");
        }
        else Debug.LogError("[WetTowel] TitleLogo が見つかりません。");
        CenterHowToBodyText(titleScene, "TutorialPanel");
        EditorSceneManager.MarkSceneDirty(titleScene);
        EditorSceneManager.SaveScene(titleScene);

        // ルール本文（セレクト画面）
        Scene selectScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Select_Windows.unity", OpenSceneMode.Single);
        CenterHowToBodyText(selectScene, "RulePanel");
        EditorSceneManager.MarkSceneDirty(selectScene);
        EditorSceneManager.SaveScene(selectScene);

        // リザルト画面とチュートリアル（ゲーム画面）
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);
        GameObject starContainer = SetupHelpers.FindInScene(gameScene, "StarContainer");
        if (starContainer != null)
        {
            RectTransform rt = starContainer.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(700f, 70f);
            rt.sizeDelta = new Vector2(500f, 350f);
            Debug.Log("[WetTowel] StarContainer を (700,70) / 500x350 にしました。");
        }
        else Debug.LogError("[WetTowel] StarContainer が見つかりません。");
        GameObject starsCount = SetupHelpers.FindInScene(gameScene, "TotalStarsCountText");
        if (starsCount != null)
            starsCount.GetComponent<RectTransform>().anchoredPosition = new Vector2(700f, 150f);
        else Debug.LogError("[WetTowel] TotalStarsCountText が見つかりません。");

        // 詳細ステータスは画面左端（x=5）に張り付いていたので、中央寄せにする。
        // 幅は元の650のままにする。600に縮めると最長行「PERFECT : 1 人」
        // （約591px）が折り返して「人」だけ2行目に落ち、6行になって
        // COMBO行が下にはみ出して見えなくなる（run6初回で確認）。
        // 650幅・中央寄せなら最長行の左右に約30pxずつ余白ができ、
        // 文字は画面左端から約40pxの位置から始まる。
        GameObject detailStats = SetupHelpers.FindInScene(gameScene, "DetailStatsText");
        if (detailStats != null)
        {
            Text detailText = detailStats.GetComponent<Text>();
            if (detailText != null) detailText.alignment = TextAnchor.MiddleCenter;
            RectTransform detailRt = detailStats.GetComponent<RectTransform>();
            detailRt.anchoredPosition = new Vector2(-625f, 0f);
            detailRt.sizeDelta = new Vector2(650f, 500f);
            Debug.Log("[WetTowel] DetailStatsText を中央寄せにしました。");
        }
        else Debug.LogError("[WetTowel] DetailStatsText が見つかりません。");
        CenterTextAlignment(gameScene, "FinalScoreText");
        CenterTextAlignment(gameScene, "HighScoreText");

        GameObject stepText = SetupHelpers.FindInScene(gameScene, "StepText");
        if (stepText != null)
        {
            Text st = stepText.GetComponent<Text>();
            if (st != null) st.alignment = TextAnchor.MiddleCenter;
            Debug.Log("[WetTowel] チュートリアル StepText を中央寄せにしました。");
        }
        else Debug.LogError("[WetTowel] StepText が見つかりません。");
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Round3 fixes が完了しました。");
    }

    // 遊び方・ルールパネルの本文を中央寄せにする（白パネルの中央に文が来る）。
    private static void CenterHowToBodyText(Scene scene, string panelName)
    {
        GameObject panel = SetupHelpers.FindInScene(scene, panelName);
        if (panel == null)
        {
            Debug.LogError($"[WetTowel] {panelName} が見つかりません。");
            return;
        }
        HowToPlayPanel howTo = panel.GetComponent<HowToPlayPanel>();
        if (howTo == null)
        {
            Debug.LogError($"[WetTowel] {panelName} に HowToPlayPanel がありません。");
            return;
        }
        SerializedObject so = new SerializedObject(howTo);
        Text bodyText = so.FindProperty("bodyText").objectReferenceValue as Text;
        if (bodyText != null)
        {
            bodyText.alignment = TextAnchor.MiddleCenter;
            Debug.Log($"[WetTowel] {panelName} の本文を中央寄せにしました。");
        }
        else Debug.LogError($"[WetTowel] {panelName} の bodyText が見つかりません。");
    }

    // 指定したTextの配置だけを中央寄せにする。
    private static void CenterTextAlignment(Scene scene, string objectName)
    {
        GameObject go = SetupHelpers.FindInScene(scene, objectName);
        if (go == null)
        {
            Debug.LogError($"[WetTowel] {objectName} が見つかりません。");
            return;
        }
        Text t = go.GetComponent<Text>();
        if (t != null)
        {
            t.alignment = TextAnchor.MiddleCenter;
            Debug.Log($"[WetTowel] {objectName} を中央寄せにしました。");
        }
    }
}
