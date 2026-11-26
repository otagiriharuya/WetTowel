using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 第4弾修正（2026-11-23、見出し中央・本文は中央ブロック内左揃え）。
public static class Round4Fixes
{
    // ---- 第4弾修正（2026-11-23、見出し中央・本文は中央ブロック内左揃え） ----
    // ユーザー指摘：Round3の中央寄せだと文が読みづらい。見出しが中央に
    // 来ているのはよいが、本文は左揃えにしてほしい。ただし枠の端ではなく
    // 中央に配置し、その中で改行しても列がそろうようにすること。
    // Windows版シーンのみ。モバイル版シーンは触らない。文言は変えない。
    // - 遊び方・ルール：ページの1行目を見出し（中央寄せのHeadingText）、
    //   残りを本文（bodyText）にする。bodyTextは中央アンカー＋
    //   ContentSizeFitter（横幅＝最長行）で中央に置いたブロックの中で
    //   左揃え（MiddleLeft）にする。
    // - リザルトのDetailStatsText：MiddleLeft＋ContentSizeFitterで、
    //   中央に置いたブロックの中で各行の左端とコロン位置をそろえる。
    //   （Round3のMiddleCenterでは各行が独立に中央寄せされ、コロンがずれていた）
    // - FinalScoreText / HighScoreText（見出し＋値）とチュートリアル
    //   StepText（1行ずつ）は変更しない。
    [MenuItem("WetTowel/Apply Round4 Fixes")]
    public static void ApplyRound4Fixes()
    {
        Debug.Log("[WetTowel] Round4 fixes を開始します。");

        // 遊び方（タイトル画面）とルール（セレクト画面）
        ApplyRound4HowToPanel("Assets/Scenes/Windows/Title_Windows.unity", "TutorialPanel");
        ApplyRound4HowToPanel("Assets/Scenes/Windows/Select_Windows.unity", "RulePanel");

        // リザルト画面（ゲーム画面）
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);
        GameObject detailStats = SetupHelpers.FindInScene(gameScene, "DetailStatsText");
        if (detailStats != null)
        {
            Text detailText = detailStats.GetComponent<Text>();
            if (detailText != null)
            {
                detailText.alignment = TextAnchor.MiddleLeft;
                Debug.Log("[WetTowel] DetailStatsText を左揃え（MiddleLeft）にしました。");
            }
            AddHorizontalFitter(detailStats);
            // 中心は(-544,0)にする。最長行「COMBO   : 1 コンボ」は
            // 約752pxあるため、Round3の中心(-625,0)（画面335）のままでは
            // 左端が-41になり、行頭の文字が画面外にはみ出して欠ける
            // （run7初回で確認）。中心を-544（画面416）にずらすと
            // 左端は約40pxになり、右端は約792pxになる。
            // TOTAL SCORE（x 693〜、y 308〜486）・HIGH SCORE（x 732〜、
            // y 508〜686）の文字とは行の高さが違うため重ならない。
            // （PERFECT行はy 332〜398でTOTAL SCOREの左端693より手前で
            // 終わり、COMBO行はy 676〜741でHIGH SCOREの文字より下にある）
            RectTransform detailRt = detailStats.GetComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.5f, 0.5f);
            detailRt.anchorMax = new Vector2(0.5f, 0.5f);
            detailRt.anchoredPosition = new Vector2(-544f, 0f);
            detailRt.sizeDelta = new Vector2(650f, 500f);
            Debug.Log("[WetTowel] DetailStatsText を中央ブロック内左揃えにしました（中心 -544,0 / 650x500）。");
        }
        else Debug.LogError("[WetTowel] DetailStatsText が見つかりません。");
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Round4 fixes が完了しました。");
    }

    // 遊び方・ルールパネル1件分のRound4修正
    private static void ApplyRound4HowToPanel(string scenePath, string panelName)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
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
        if (bodyText == null)
        {
            Debug.LogError($"[WetTowel] {panelName} の bodyText が見つかりません。");
            return;
        }

        // 見出し用Textを作成（bodyText と同じ親＝白パネルの子）。
        // 再実行時は既存のものを再利用する。
        Transform parent = bodyText.transform.parent;
        Transform headingTr = parent != null ? parent.Find("HeadingText") : null;
        Text headingText;
        if (headingTr != null)
        {
            headingText = headingTr.GetComponent<Text>();
            Debug.Log($"[WetTowel] {scenePath}: 既存の HeadingText を再利用します。");
        }
        else
        {
            GameObject headingGo = new GameObject("HeadingText", typeof(RectTransform), typeof(Text));
            headingGo.transform.SetParent(parent, false);
            headingText = headingGo.GetComponent<Text>();
            Debug.Log($"[WetTowel] {scenePath}: HeadingText を作成しました。");
        }
        headingText.font = bodyText.font;
        headingText.fontSize = bodyText.fontSize;
        headingText.fontStyle = bodyText.fontStyle;
        headingText.color = bodyText.color;
        headingText.alignment = TextAnchor.MiddleCenter;
        headingText.text = string.Empty;
        SetupHelpers.SetAnchored(headingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 135f), new Vector2(1400f, 90f));

        // 本文は中央に置いたブロックの中で左揃えにする。
        // 中央アンカー＋横幅を最長行に合わせるContentSizeFitterで、
        // ブロック自体は白パネルの中央に置かれたままになる。
        RectTransform bodyRt = bodyText.rectTransform;
        bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRt.anchoredPosition = new Vector2(0f, -45f);
        bodyRt.sizeDelta = new Vector2(1400f, 300f);
        bodyText.alignment = TextAnchor.MiddleLeft;
        AddHorizontalFitter(bodyText.gameObject);

        // HowToPlayPanel の headingText フィールドに割り当てる
        so.FindProperty("headingText").objectReferenceValue = headingText;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[WetTowel] {scenePath}: {panelName} を見出し中央・本文は中央ブロック内左揃えにしました。");
    }

    // 横幅だけを内容（最長行）に合わせる ContentSizeFitter を付ける。
    // 縦は Unconstrained のまま（高さは設定値を維持する）。
    private static void AddHorizontalFitter(GameObject go)
    {
        ContentSizeFitter fitter = go.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = go.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
    }
}
