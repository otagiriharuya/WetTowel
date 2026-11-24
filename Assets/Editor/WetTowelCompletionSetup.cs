using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// WetTowel完成作業のためのセットアップ用エディタツール。
/// メニュー: WetTowel/Setup Completion
/// - 新規スプライト3点のインポート設定（Sprite化）
/// - 欠けていたスプライト参照の張り直し（Customer / WetTowel / Game画面の枠類）
/// - タイトル/セレクト画面の「遊び方」「ルール」パネルをページ式にする
/// - ゲーム画面に初回チュートリアル＋「？」ボタン＋操作ヒントバーを追加
/// - ComboTextのtypo修正
/// 既存のゲーム仕様・操作・レベル構成は変えない。
/// </summary>
public static class WetTowelCompletionSetup
{
    private const string WhiteSquarePath = "Assets/Image/white_square.png";
    private const string WetTowelPath = "Assets/Image/wet_towel.png";
    private const string CustomerPath = "Assets/Image/customer.png";
    private const string CustomerPrefabPath = "Assets/Prefab/Customer.prefab";
    private const string WetTowelPrefabPath = "Assets/Prefab/WetTowel.prefab";

    private static readonly string[] TitleScenes =
        { "Assets/Scenes/Windows/Title_Windows.unity", "Assets/Scenes/Mobile/Title_Mobile.unity" };
    private static readonly string[] SelectScenes =
        { "Assets/Scenes/Windows/Select_Windows.unity", "Assets/Scenes/Mobile/Select_Mobile.unity" };
    private static readonly string[] GameScenes =
        { "Assets/Scenes/Windows/Game_Windows.unity", "Assets/Scenes/Mobile/Game_Mobile.unity" };

    // 汎用白四角スプライトを使うGame画面のオブジェクト名
    private static readonly HashSet<string> SlotObjectNames = new HashSet<string>
        { "WarmerSlot", "WarmerSlot (1)", "CoolerSlot", "CoolerSlot (1)", "TrashCan", "WetTowelSpawner" };

    [MenuItem("WetTowel/Setup Completion")]
    public static void SetupCompletion()
    {
        Debug.Log("[WetTowel] Setup Completion を開始します。");
        ImportSpritesAsSprite();
        AssetDatabase.Refresh();

        Sprite whiteSquare = AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSquarePath);
        Sprite wetTowel = AssetDatabase.LoadAssetAtPath<Sprite>(WetTowelPath);
        Sprite customer = AssetDatabase.LoadAssetAtPath<Sprite>(CustomerPath);
        Debug.Log($"[WetTowel] スプライト読込: white={whiteSquare != null} towel={wetTowel != null} customer={customer != null}");
        if (whiteSquare == null || wetTowel == null || customer == null)
        {
            Debug.LogError("[WetTowel] スプライトの読み込みに失敗しました。中止します。");
            return;
        }

        FixPrefabs(wetTowel, customer);
        foreach (string scene in TitleScenes) SetupHowToPanel(scene, "TutorialPanel", HowToPlayPanel.PanelKind.Flow);
        foreach (string scene in SelectScenes) SetupHowToPanel(scene, "RulePanel", HowToPlayPanel.PanelKind.Rules);
        foreach (string scene in GameScenes) SetupGameScene(scene, whiteSquare);

        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Setup Completion が完了しました。");
    }

    [MenuItem("WetTowel/Verify References")]
    public static void VerifyReferences()
    {
        int problems = 0;
        foreach (string scene in TitleScenes.Concat(SelectScenes).Concat(GameScenes))
        {
            Scene s = EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
            foreach (SpriteRenderer sr in Object.FindObjectsOfType<SpriteRenderer>(true))
            {
                if (sr.sprite == null)
                {
                    Debug.LogWarning($"[Verify] {scene}: SpriteRendererのスプライト未設定: {sr.gameObject.name}");
                    problems++;
                }
            }
            foreach (HowToPlayPanel p in Object.FindObjectsOfType<HowToPlayPanel>(true))
            {
                if (p == null) continue;
            }
            foreach (GameTutorial t in Object.FindObjectsOfType<GameTutorial>(true))
            {
                if (t == null) continue;
            }
            Debug.Log($"[Verify] {scene} を確認しました。");
        }
        Debug.Log(problems == 0
            ? "[Verify] 問題なし: 全シーンのSpriteRendererにスプライトが設定されています。"
            : $"[Verify] 問題 {problems} 件が残っています。");
    }

    // ---- スプライトのインポート設定 ----

    private static void ImportSpritesAsSprite()
    {
        foreach (string path in new[] { WhiteSquarePath, WetTowelPath, CustomerPath })
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[WetTowel] TextureImporterが取得できません: {path}");
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.SaveAndReimport();
            Debug.Log($"[WetTowel] Sprite化: {path}");
        }
    }

    // ---- プレハブのスプライト張り直し ----

    private static void FixPrefabs(Sprite wetTowel, Sprite customer)
    {
        // Customer.prefab: MainUnit の SpriteRenderer
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(CustomerPrefabPath);
        SpriteRenderer customerSr = customerRoot.transform.Find("MainUnit")?.GetComponent<SpriteRenderer>();
        if (customerSr != null)
        {
            customerSr.sprite = customer;
            Debug.Log("[WetTowel] Customer.prefab MainUnit にスプライトを設定しました。");
        }
        else
        {
            Debug.LogError("[WetTowel] Customer.prefab に MainUnit が見つかりません。");
        }
        PrefabUtility.SaveAsPrefabAsset(customerRoot, CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);

        // WetTowel.prefab: 2つのSpriteRenderer（影層・本体層）両方に同じスプライト
        GameObject towelRoot = PrefabUtility.LoadPrefabContents(WetTowelPrefabPath);
        SpriteRenderer[] towelSrs = towelRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in towelSrs)
            sr.sprite = wetTowel;
        Debug.Log($"[WetTowel] WetTowel.prefab のSpriteRenderer {towelSrs.Length} 件にスプライトを設定しました。");
        PrefabUtility.SaveAsPrefabAsset(towelRoot, WetTowelPrefabPath);
        PrefabUtility.UnloadPrefabContents(towelRoot);
    }

    // ---- 「遊び方」「ルール」パネル ----

    private static void SetupHowToPanel(string scenePath, string panelName, HowToPlayPanel.PanelKind kind)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        // Steam向けはWindows版のみ。モバイル版シーンは触らない。
        if (scenePath.Contains("/Mobile/"))
            return;
        // GameObject.Find は非アクティブなオブジェクトを見つけられない。
        // TutorialPanel / RulePanel は m_IsActive: 0 で保存されているため、
        // シーンのルートから再帰的に探す。
        GameObject panel = FindInScene(scene, panelName);
        if (panel == null)
        {
            Debug.LogError($"[WetTowel] {scenePath}: {panelName} が見つかりません。");
            return;
        }

        // パネルはCanvasの最前面に置く（タイトルのロゴやボタンが
        // 暗転の上に透けて見えるのを防ぐ）。再実行時も毎回適用する。
        panel.transform.SetAsLastSibling();

        // 白パネル（子Image、1500x800）を縦に広げる。1500x800のままだと
        // 背後のタイトルロゴの上部と「辞める」ボタンの下部がパネルの
        // 上下からはみ出して白く透けて見える。1500x1000にすると
        // ロゴ上端（画面y≈90）とボタン下端（画面y≈1015）が隠れる。
        // 再実行時も含めて毎回適用する。
        Transform imgTr = panel.transform.Find("Image");
        RectTransform imgRt = imgTr != null ? imgTr.GetComponent<RectTransform>() : null;
        if (imgRt != null && imgRt.sizeDelta.y < 1000f)
        {
            imgRt.sizeDelta = new Vector2(1500f, 1000f);
            Debug.Log($"[WetTowel] {scenePath}: {panelName} の白パネルを1500x1000に広げました。");
        }

        // 既にセットアップ済みなら二重追加しない（再実行時の冪等性）。
        // ただし上の白パネル拡大などの修正は保存する（保存しないと
        // インメモリだけの変更になってビルドに反映されない）。
        // ボタンの配置修正と表示内容の種類（Flow / Rules）の割り当ても
        // 早期returnの前に毎回適用する。既存パネルはこの時点で
        // HowToPlayPanelを持っているため、ここで直さないと反映されない。
        if (panel.GetComponent<HowToPlayPanel>() != null)
        {
            FixHowToPanelButtons(panel, imgTr);
            HowToPlayPanel existing = panel.GetComponent<HowToPlayPanel>();
            SerializedObject existingSo = new SerializedObject(existing);
            existingSo.FindProperty("kind").enumValueIndex = (int)kind;
            existingSo.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[WetTowel] {scenePath}: {panelName} は既にセットアップ済みのためスキップします。（ボタン配置と表示内容 {kind} を適用）");
            return;
        }

        // 既存のプレースホルダーTextを探す
        Text bodyText = null;
        foreach (Text t in panel.GetComponentsInChildren<Text>(true))
        {
            if (t.text.Contains("一枚画像"))
                bodyText = t;
        }
        if (bodyText == null)
        {
            Debug.LogError($"[WetTowel] {scenePath}: プレースホルダーTextが見つかりません。");
            return;
        }

        // 説明文エリアを下部のボタン分だけ空ける
        RectTransform bodyRt = bodyText.rectTransform;
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(24f, 96f);
        bodyRt.offsetMax = new Vector2(-24f, -16f);
        bodyText.alignment = TextAnchor.UpperLeft;

        Font font = bodyText.font;

        // ページ送りボタンとページ表示は白パネル（Image）の子にする。
        // 以前は全画面のTutorialPanel直下に置いていたため、画面の隅に
        // 離れて置かれ、白パネルの外にはみ出していた。
        // 白パネル内なら下部の左右・中央に固定サイズで収まる。
        Transform buttonParent = imgTr != null ? imgTr : panel.transform;
        Button prev = CreateTextButton(buttonParent, "PrevPageButton", "◀ 前へ", font, new Color(0.85f, 0.85f, 0.85f));
        Button next = CreateTextButton(buttonParent, "NextPageButton", "次へ ▶", font, new Color(0.85f, 0.85f, 0.85f));
        Text pageLabel = CreateLabel(buttonParent, "PageLabel", kind == HowToPlayPanel.PanelKind.Rules ? "1 / 4" : "1 / 3", font);

        FixHowToPanelButtons(panel, imgTr);

        HowToPlayPanel howTo = panel.GetComponent<HowToPlayPanel>();
        if (howTo == null)
            howTo = panel.AddComponent<HowToPlayPanel>();

        // SerializedFieldへ割り当て
        SerializedObject so = new SerializedObject(howTo);
        so.FindProperty("kind").enumValueIndex = (int)kind;
        so.FindProperty("bodyText").objectReferenceValue = bodyText;
        so.FindProperty("prevButton").objectReferenceValue = prev;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.FindProperty("pageLabel").objectReferenceValue = pageLabel;
        so.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[WetTowel] {scenePath}: {panelName} をページ式の遊び方パネルにしました。");
    }

    // ページ送りボタンとページ表示を白パネル内の固定サイズ・固定位置に直す。
    // 原因: 以前はボタンが全画面（1920x1080）のTutorialPanel/RulePanel直下に
    // 置かれていた。RectTransform自体は170x60の固定サイズだったが、
    // 位置が画面の隅（x=96/-96, y=40）で白パネル（1500x1000）の外に
    // はみ出し、画面幅いっぱいに離れて置かれたうえ、UISpriteの角丸が
    // Simple描画で横に引き延ばされて楕円形に見えていた。
    // 修正: 白パネル（Image）の子にし、白パネル下部の左右・中央に
    // anchorMin==anchorMaxの固定アンカーで置く。サイズは文字に合った
    // 120x48（ページ表示は120x40）にする。既存ボタンにも再実行時に適用する。
    private static void FixHowToPanelButtons(GameObject panel, Transform imgTr)
    {
        if (imgTr == null)
        {
            Debug.LogWarning($"[WetTowel] {panel.name}: 白パネル（Image）が見つからないためボタン配置を直せません。");
            return;
        }
        Transform prevTr = panel.transform.Find("PrevPageButton");
        if (prevTr == null) prevTr = imgTr.Find("PrevPageButton");
        Transform nextTr = panel.transform.Find("NextPageButton");
        if (nextTr == null) nextTr = imgTr.Find("NextPageButton");
        Transform labelTr = panel.transform.Find("PageLabel");
        if (labelTr == null) labelTr = imgTr.Find("PageLabel");

        if (prevTr != null)
        {
            prevTr.SetParent(imgTr, false);
            SetAnchored(prevTr.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(90f, 40f), new Vector2(120, 48));
        }
        if (nextTr != null)
        {
            nextTr.SetParent(imgTr, false);
            SetAnchored(nextTr.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-90f, 40f), new Vector2(120, 48));
        }
        if (labelTr != null)
        {
            labelTr.SetParent(imgTr, false);
            SetAnchored(labelTr.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 40f), new Vector2(120, 40));
        }
        // 角丸スプライトをSimple描画のまま横長にすると角まで引き延ばされるため、
        // Sliced描画にして角の形を保つ。
        foreach (Transform btnTr in new[] { prevTr, nextTr })
        {
            if (btnTr == null) continue;
            Image img = btnTr.GetComponent<Image>();
            if (img != null)
                img.type = Image.Type.Sliced;
        }
        Debug.Log($"[WetTowel] {panel.name}: ページ送りボタンを白パネル内に固定サイズ（120x48）で配置しました。");
    }

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
        GameObject titleLogo = FindInScene(titleScene, "TitleLogo");
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
        GameObject starContainer = FindInScene(gameScene, "StarContainer");
        if (starContainer != null)
        {
            RectTransform rt = starContainer.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(700f, 70f);
            rt.sizeDelta = new Vector2(500f, 350f);
            Debug.Log("[WetTowel] StarContainer を (700,70) / 500x350 にしました。");
        }
        else Debug.LogError("[WetTowel] StarContainer が見つかりません。");
        GameObject starsCount = FindInScene(gameScene, "TotalStarsCountText");
        if (starsCount != null)
            starsCount.GetComponent<RectTransform>().anchoredPosition = new Vector2(700f, 150f);
        else Debug.LogError("[WetTowel] TotalStarsCountText が見つかりません。");

        // 詳細ステータスは画面左端（x=5）に張り付いていたので、中央寄せにする。
        // 幅は元の650のままにする。600に縮めると最長行「PERFECT : 1 人」
        // （約591px）が折り返して「人」だけ2行目に落ち、6行になって
        // COMBO行が下にはみ出して見えなくなる（run6初回で確認）。
        // 650幅・中央寄せなら最長行の左右に約30pxずつ余白ができ、
        // 文字は画面左端から約40pxの位置から始まる。
        GameObject detailStats = FindInScene(gameScene, "DetailStatsText");
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

        GameObject stepText = FindInScene(gameScene, "StepText");
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
        GameObject detailStats = FindInScene(gameScene, "DetailStatsText");
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

    // ---- Round5: 見た目改善 ----
    // 配色（和風の店）：藍＋生成り＋暖かい赤。タイトル・セレクト・ゲーム・リザルトで統一する。
    private static readonly Color PaletteIndigo = new Color(0.18f, 0.29f, 0.42f, 1f);
    private static readonly Color PaletteCream = new Color(0.97f, 0.94f, 0.85f, 1f);
    private static readonly Color PaletteLogoCream = new Color(0.98f, 0.95f, 0.88f, 1f);
    private static readonly Color PaletteResultCream = new Color(0.95f, 0.90f, 0.79f, 1f);
    private static readonly Color PaletteWarmRed = new Color(0.70f, 0.25f, 0.16f, 1f);
    private static readonly Color PaletteSlotCream = new Color(0.97f, 0.94f, 0.85f, 1f);

    [MenuItem("WetTowel/Apply Round5 Fixes")]
    public static void ApplyRound5Fixes()
    {
        Debug.Log("[WetTowel] Round5 fixes を開始します。");
        SetupRound5SpriteImports();
        ApplyRound5CustomerPrefab();
        ApplyRound5GameScene();
        ApplyRound5TitleScene();
        ApplyRound5SelectScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Round5 fixes が完了しました。");
    }

    // Round5で使う画像のインポート設定（Sprite化・Pixels Per Unit）
    private static void SetupRound5SpriteImports()
    {
        SetSpriteImport("Assets/Image/customer.png", 200);
        SetSpriteImport("Assets/Image/wet_towel.png", 100);
        SetSpriteImport("Assets/Image/customer_happy.png", 200);
        SetSpriteImport("Assets/Image/customer_angry.png", 200);
        SetSpriteImport("Assets/Image/shop_bg.png", 108);
        SetSpriteImport("Assets/Image/title_bg.png", 100);
        SetSpriteImport("Assets/Image/sparkle.png", 100);
        SetSpriteImport("Assets/Image/steam.png", 100);
        SetSpriteImport("Assets/Image/icon_warmer.png", 100);
        SetSpriteImport("Assets/Image/icon_cooler.png", 100);
        SetSpriteImport("Assets/Image/icon_stock.png", 100);
        SetSpriteImport("Assets/Image/icon_trash.png", 100);
        SetSpriteImport("Assets/Image/label_warmer.png", 100);
        SetSpriteImport("Assets/Image/label_cooler.png", 100);
        SetSpriteImport("Assets/Image/label_stock.png", 100);
        SetSpriteImport("Assets/Image/label_trash.png", 100);
    }

    private static void SetSpriteImport(string path, int pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[WetTowel] 画像が見つかりません: {path}");
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.SaveAndReimport();
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError($"[WetTowel] スプライトが読み込めません: {path}");
        return sprite;
    }

    // 客プレハブに表情スプライトとキラキラを割り当てる
    private static void ApplyRound5CustomerPrefab()
    {
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(CustomerPrefabPath);
        Customer customer = customerRoot.GetComponent<Customer>();
        if (customer == null)
        {
            Debug.LogError("[WetTowel] Customerプレハブに Customer がありません。");
            PrefabUtility.UnloadPrefabContents(customerRoot);
            return;
        }
        SpriteRenderer bodyRenderer = customerRoot.GetComponentInChildren<SpriteRenderer>();
        SerializedObject so = new SerializedObject(customer);
        so.FindProperty("bodyRenderer").objectReferenceValue = bodyRenderer;
        so.FindProperty("normalSprite").objectReferenceValue = LoadSprite("Assets/Image/customer.png");
        so.FindProperty("happySprite").objectReferenceValue = LoadSprite("Assets/Image/customer_happy.png");
        so.FindProperty("angrySprite").objectReferenceValue = LoadSprite("Assets/Image/customer_angry.png");
        so.FindProperty("sparkleSprite").objectReferenceValue = LoadSprite("Assets/Image/sparkle.png");
        so.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(customerRoot, CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);
        Debug.Log("[WetTowel] Customerプレハブに表情・キラキラを割り当てました。");
    }

    private static void ApplyRound5GameScene()
    {
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);

        // 店の背景（最背面）。カメラは orthographic size 5 なので、
        // 画面は 17.78 x 10 ユニット。shop_bg は PPU 108 でちょうどその大きさになる。
        GameObject shopBg = FindInScene(gameScene, "ShopBackground");
        if (shopBg == null)
        {
            shopBg = new GameObject("ShopBackground", typeof(SpriteRenderer));
            Debug.Log("[WetTowel] ShopBackground を作成しました。");
        }
        SpriteRenderer bgSr = shopBg.GetComponent<SpriteRenderer>();
        bgSr.sprite = LoadSprite("Assets/Image/shop_bg.png");
        bgSr.sortingOrder = -10;
        shopBg.transform.position = Vector3.zero;

        // スロット：土台を生成り色にし、アイコンとラベルを付ける
        ApplyRound5Slot(gameScene, "WarmerSlot", "Assets/Image/icon_warmer.png", "Assets/Image/label_warmer.png");
        ApplyRound5Slot(gameScene, "CoolerSlot", "Assets/Image/icon_cooler.png", "Assets/Image/label_cooler.png");
        ApplyRound5Slot(gameScene, "WetTowelSpawner", "Assets/Image/icon_stock.png", "Assets/Image/label_stock.png");
        ApplyRound5Slot(gameScene, "TrashCan", "Assets/Image/icon_trash.png", "Assets/Image/label_trash.png");

        // 保温庫の湯気
        GameObject warmer = FindInScene(gameScene, "WarmerSlot");
        if (warmer != null)
        {
            Transform steamTr = warmer.transform.Find("SteamPuffs");
            GameObject steamGo;
            if (steamTr != null)
                steamGo = steamTr.gameObject;
            else
            {
                steamGo = new GameObject("SteamPuffs", typeof(SteamPuffs));
                steamGo.transform.SetParent(warmer.transform, false);
                steamGo.transform.localPosition = new Vector3(0f, 0.15f, -0.1f);
            }
            Sprite steamSprite = LoadSprite("Assets/Image/steam.png");
            Vector3[] puffPos =
            {
                new Vector3(-0.3f, 0.2f, 0f),
                new Vector3(0.15f, 0.35f, 0f),
                new Vector3(0.35f, 0.1f, 0f)
            };
            for (int i = 0; i < 3; i++)
            {
                Transform puffTr = steamGo.transform.Find("Puff" + i);
                SpriteRenderer puffSr;
                if (puffTr != null)
                    puffSr = puffTr.GetComponent<SpriteRenderer>();
                else
                {
                    GameObject puffGo = new GameObject("Puff" + i, typeof(SpriteRenderer));
                    puffGo.transform.SetParent(steamGo.transform, false);
                    puffSr = puffGo.GetComponent<SpriteRenderer>();
                }
                puffSr.sprite = steamSprite;
                puffSr.transform.localPosition = puffPos[i];
                puffSr.transform.localScale = Vector3.one * 0.4f;
                puffSr.sortingOrder = 2;
                puffSr.color = Color.white;
            }
            DropTarget dropTarget = warmer.GetComponent<DropTarget>();
            if (dropTarget != null)
            {
                SerializedObject so = new SerializedObject(dropTarget);
                so.FindProperty("steamRoot").objectReferenceValue = steamGo;
                so.ApplyModifiedProperties();
            }
            steamGo.SetActive(false); // おしぼりがセットされた時だけ DropTarget が表示する
            Debug.Log("[WetTowel] 保温庫に湯気を追加しました。");
        }

        // HUDのSCORE文字色：黒だと店の背景（梁）で読みにくいので白にする
        foreach (Text text in Object.FindObjectsOfType<Text>(true))
        {
            if (text.gameObject.name != "ScoreText") continue;
            RectTransform rt = text.rectTransform;
            if (rt.anchorMin == new Vector2(0f, 1f))
            {
                text.color = Color.white;
                Debug.Log("[WetTowel] HUDのSCORE文字色を白にしました。");
            }
        }

        // リザルト：緑の背景を生成り色にし、文字色を配色に合わせる
        GameObject background = FindInScene(gameScene, "Background");
        if (background != null)
        {
            Image bgImage = background.GetComponent<Image>();
            if (bgImage != null) bgImage.color = PaletteResultCream;
        }
        GameObject finalScore = FindInScene(gameScene, "FinalScoreText");
        if (finalScore != null)
        {
            Text t = finalScore.GetComponent<Text>();
            if (t != null) t.color = PaletteIndigo;
        }
        GameObject highScore = FindInScene(gameScene, "HighScoreText");
        if (highScore != null)
        {
            Text t = highScore.GetComponent<Text>();
            if (t != null) t.color = PaletteWarmRed;
        }
        GameObject resultPanel = FindInScene(gameScene, "ResultPanell");
        if (resultPanel != null)
        {
            foreach (Button button in resultPanel.GetComponentsInChildren<Button>(true))
                RestyleMenuButton(button, PaletteIndigo, PaletteCream);
        }

        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[WetTowel] ゲーム画面のRound5修正が完了しました。");
    }

    // スロット1件分：土台の色を生成りにし、アイコンとラベルを子として付ける
    private static void ApplyRound5Slot(Scene scene, string slotName, string iconPath, string labelPath)
    {
        GameObject slot = FindInScene(scene, slotName);
        if (slot == null)
        {
            Debug.LogError($"[WetTowel] {slotName} が見つかりません。");
            return;
        }
        SpriteRenderer baseSr = slot.GetComponent<SpriteRenderer>();
        if (baseSr != null)
            baseSr.color = PaletteSlotCream;
        AddSlotChild(slot, "SlotIcon", LoadSprite(iconPath), new Vector3(0f, 0.05f, -0.1f), 0.4f, 1);
        AddSlotChild(slot, "SlotLabel", LoadSprite(labelPath), new Vector3(0f, 0.85f, -0.1f), 0.5f, 1);
        Debug.Log($"[WetTowel] {slotName} にアイコンとラベルを付けました。");
    }

    private static SpriteRenderer AddSlotChild(GameObject slot, string name, Sprite sprite, Vector3 localPos, float scale, int order)
    {
        Transform t = slot.transform.Find(name);
        SpriteRenderer sr;
        if (t != null)
            sr = t.GetComponent<SpriteRenderer>();
        else
        {
            GameObject go = new GameObject(name, typeof(SpriteRenderer));
            go.transform.SetParent(slot.transform, false);
            sr = go.GetComponent<SpriteRenderer>();
        }
        sr.sprite = sprite;
        sr.transform.localPosition = localPos;
        sr.transform.localScale = Vector3.one * scale;
        sr.sortingOrder = order;
        return sr;
    }

    private static void ApplyRound5TitleScene()
    {
        Scene titleScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Title_Windows.unity", OpenSceneMode.Single);
        Canvas canvas = FindMainCanvas();
        if (canvas != null)
            AddFullscreenBackground(canvas, "TitleBackground", LoadSprite("Assets/Image/title_bg.png"));

        GameObject logo = FindInScene(titleScene, "TitleLogo");
        if (logo != null)
        {
            Image logoImage = logo.GetComponent<Image>();
            if (logoImage != null) logoImage.color = PaletteLogoCream;
        }
        foreach (string buttonName in new[] { "StartButton", "TutorialButton", "EndButton" })
        {
            GameObject buttonGo = FindInScene(titleScene, buttonName);
            if (buttonGo == null) continue;
            Button button = buttonGo.GetComponent<Button>();
            if (button != null) RestyleMenuButton(button, PaletteIndigo, PaletteCream);
        }
        EditorSceneManager.MarkSceneDirty(titleScene);
        EditorSceneManager.SaveScene(titleScene);
        Debug.Log("[WetTowel] タイトル画面のRound5修正が完了しました。");
    }

    private static void ApplyRound5SelectScene()
    {
        Scene selectScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Select_Windows.unity", OpenSceneMode.Single);
        Canvas canvas = FindMainCanvas();
        if (canvas != null)
            AddFullscreenBackground(canvas, "SelectBackground", LoadSprite("Assets/Image/title_bg.png"));

        // ルールパネル内の小さいボタン（前へ・次へ・閉じる）は変えない
        foreach (Button button in Object.FindObjectsOfType<Button>(true))
        {
            if (HasAncestor(button.transform, "RulePanel")) continue;
            RestyleMenuButton(button, PaletteIndigo, PaletteCream);
        }
        EditorSceneManager.MarkSceneDirty(selectScene);
        EditorSceneManager.SaveScene(selectScene);
        Debug.Log("[WetTowel] セレクト画面のRound5修正が完了しました。");
    }

    // ---- Round6: 居酒屋らしさ・ボタンの見やすさ・客バリエーション・表情 ----
    // 舞台は居酒屋。タイトルに赤提灯・のれん、ゲーム画面にお品書き・酒瓶・のれん。
    // ボタンは生成り＋藍文字＋濃い縁取りにして背景から見やすくする。
    // 客は3パターン（来店ごとにランダム）、表情は大きく描き直した絵に差し替える。
    private static readonly Color PaletteButtonOutline = new Color(0.10f, 0.16f, 0.25f, 1f);

    [MenuItem("WetTowel/Apply Round6 Fixes")]
    public static void ApplyRound6Fixes()
    {
        Debug.Log("[WetTowel] Round6 fixes を開始します。");
        SetupRound6SpriteImports();
        ApplyRound6CustomerPrefab();
        ApplyRound6GameScene();
        ApplyRound6TitleScene();
        ApplyRound6SelectScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Round6 fixes が完了しました。");
    }

    // ---- Round7: 客は椅子に座る構図・枠の見やすさ・質感 ----
    // カウンターを中央やや下に置き、客はカウンターの奥の椅子に座る。
    // 保温庫・保冷庫・ストック・ゴミ箱はカウンターの下に一列に並べる。
    // スロットに濃い藍の縁取りの枠を付け、ラベルに生成りの台を付ける。
    // 背景は質感のある夕方の居酒屋に描き直し、客の絵も影と質感を足したものに差し替える。
    // 座標は画面 1920x1080（カメラ orthographic size 5 → 108px/ユニット）で測ったもの。
    // 仕様（温度・得点・時間・解放の点数）は変えない。
    // MachineUnlockManager の参照（4000点→CoolerSlot (1)、7000点→WarmerSlot (1)）と
    // (1) スロットの開始時非アクティブは維持する。
    private static readonly Color PaletteFrameNavy = new Color(0.18f, 0.29f, 0.42f, 1f); // 藍 #2E4A6B
    private static readonly Color PaletteLabelBacking = new Color(0.97f, 0.94f, 0.85f, 1f); // 生成り

    // 椅子の中心（shop_bg の実測：椅子x 535, 784, 1037, 1257, 1534px → wx = px/108 - 8.8889）
    private static readonly float[] SeatX = { -3.94f, -1.63f, 0.71f, 2.75f, 5.31f };
    private const float SeatY = -2.28f; // 席の画面y 786。客の腰がカウンターの上端（画面800）に来る
    private const float SlotRowY = -4.12f; // スロットの画面y 985。ヒントバー（画面1032〜）と重ならない

    [MenuItem("WetTowel/Apply Round7 Fixes")]
    public static void ApplyRound7Fixes()
    {
        Debug.Log("[WetTowel] Round7 fixes を開始します。");
        SetupRound7SpriteImports();
        ApplyRound7CustomerPrefab();
        ApplyRound7WetTowelPrefab();
        ApplyRound7GameScene();
        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Round7 fixes が完了しました。");
    }

    private static void SetupRound7SpriteImports()
    {
        SetSpriteImport("Assets/Image/shop_bg.png", 108);
        SetSpriteImport("Assets/Image/counter_front.png", 108);
        SetSpriteImport("Assets/Image/customer.png", 200);
        SetSpriteImport("Assets/Image/customer_happy.png", 200);
        SetSpriteImport("Assets/Image/customer_angry.png", 200);
        SetSpriteImport("Assets/Image/customer_b.png", 200);
        SetSpriteImport("Assets/Image/customer_b_happy.png", 200);
        SetSpriteImport("Assets/Image/customer_b_angry.png", 200);
        SetSpriteImport("Assets/Image/customer_c.png", 200);
        SetSpriteImport("Assets/Image/customer_c_happy.png", 200);
        SetSpriteImport("Assets/Image/customer_c_angry.png", 200);
    }

    // 客プレハブ：我慢ゲージを頭の横へ（カウンターの上に見える位置に）
    private static void ApplyRound7CustomerPrefab()
    {
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(CustomerPrefabPath);
        Transform patienceBar = customerRoot.transform.Find("MainUnit/Canvas/PatienceBar");
        if (patienceBar == null)
        {
            Debug.LogError("[WetTowel] PatienceBar が見つかりません。");
        }
        else
        {
            RectTransform rt = patienceBar as RectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(160f, 60f);
                Debug.Log("[WetTowel] 我慢ゲージを客の頭の横に移しました。");
            }
        }
        PrefabUtility.SaveAsPrefabAsset(customerRoot, CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);
    }

    // おしぼりプレハブ：影をスロットの土台より上に（カウンターの前面より上、タオル本体の下に）
    // 注意：本体はルート（scale 0.5）、影は子（scale 1.2、黒）にある。
    private static void ApplyRound7WetTowelPrefab()
    {
        GameObject towelRoot = PrefabUtility.LoadPrefabContents(WetTowelPrefabPath);
        SpriteRenderer mainSr = towelRoot.GetComponent<SpriteRenderer>();
        if (mainSr != null)
            mainSr.sortingOrder = 10;
        foreach (SpriteRenderer sr in towelRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject == towelRoot) continue;
            sr.sortingOrder = 7;
        }
        PrefabUtility.SaveAsPrefabAsset(towelRoot, WetTowelPrefabPath);
        PrefabUtility.UnloadPrefabContents(towelRoot);
        Debug.Log("[WetTowel] おしぼりの影の sortingOrder を 7、本体を 10 にしました。");
    }

    private static void ApplyRound7GameScene()
    {
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);
        // 背景の差し替え（同じアセットパス・GUID）
        GameObject shopBg = FindInScene(gameScene, "ShopBackground");
        if (shopBg != null)
        {
            SpriteRenderer bgSr = shopBg.GetComponent<SpriteRenderer>();
            if (bgSr != null) bgSr.sprite = LoadSprite("Assets/Image/shop_bg.png");
        }

        // カウンター前面：客の下半身を隠す。shop_bg のカウンター部分（画面y 800〜1080）を切り出した絵。
        // 1920x280 @ PPU108 → 17.78 x 2.59。中心の画面y 940 → wy = 5 - 940/108 = -3.70
        GameObject counterFront = FindInScene(gameScene, "CounterFront");
        if (counterFront == null)
        {
            counterFront = new GameObject("CounterFront", typeof(SpriteRenderer));
            Debug.Log("[WetTowel] CounterFront を作成しました。");
        }
        SpriteRenderer cfSr = counterFront.GetComponent<SpriteRenderer>();
        cfSr.sprite = LoadSprite("Assets/Image/counter_front.png");
        cfSr.sortingOrder = 2;
        counterFront.transform.position = new Vector3(0f, -3.70f, 0f);

        // 席：椅子の中心に合わせる
        CustomerSpawner spawner = Object.FindObjectOfType<CustomerSpawner>();
        if (spawner == null)
        {
            Debug.LogError("[WetTowel] CustomerSpawner が見つかりません。");
        }
        else
        {
            // spawnPoint はスポナー自身の Transform。客は席と同じ高さから歩いてくる
            spawner.transform.position = new Vector3(10f, SeatY, 0f);
            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty seats = so.FindProperty("counterSeats");
            if (seats == null)
            {
                Debug.LogError("[WetTowel] counterSeats が見つかりません。");
            }
            else
            {
                for (int i = 0; i < seats.arraySize && i < SeatX.Length; i++)
                {
                    Transform seat = seats.GetArrayElementAtIndex(i).objectReferenceValue as Transform;
                    if (seat != null)
                        seat.position = new Vector3(SeatX[i], SeatY, 0f);
                }
                Debug.Log("[WetTowel] 席を椅子の位置に移しました。");
            }
        }

        // スロット：カウンターの下に一列に並べる。(1) スロットの開始時非アクティブは触らない
        MoveRound7Slot(gameScene, "WetTowelSpawner", -6.5f);
        MoveRound7Slot(gameScene, "CoolerSlot", -4.3f);
        MoveRound7Slot(gameScene, "CoolerSlot (1)", -2.1f);
        MoveRound7Slot(gameScene, "WarmerSlot (1)", 2.1f);
        MoveRound7Slot(gameScene, "WarmerSlot", 4.3f);
        MoveRound7Slot(gameScene, "TrashCan", 6.5f);

        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[WetTowel] ゲーム画面のRound7修正が完了しました。");
    }

    // スロット1件分：位置・枠・ラベルの台・sortingOrder を整える
    private static void MoveRound7Slot(Scene scene, string slotName, float x)
    {
        GameObject slot = FindInScene(scene, slotName);
        if (slot == null)
        {
            Debug.LogError($"[WetTowel] {slotName} が見つかりません。");
            return;
        }
        slot.transform.position = new Vector3(x, SlotRowY, slot.transform.position.z);

        // 土台は枠の上に
        SpriteRenderer baseSr = slot.GetComponent<SpriteRenderer>();
        if (baseSr != null) baseSr.sortingOrder = 4;

        // 枠：濃い藍の縁取り。土台より一回り大きく、土台の後ろに置く
        Transform frameTr = slot.transform.Find("Frame");
        SpriteRenderer frameSr;
        if (frameTr != null)
            frameSr = frameTr.GetComponent<SpriteRenderer>();
        else
        {
            GameObject frameGo = new GameObject("Frame", typeof(SpriteRenderer));
            frameGo.transform.SetParent(slot.transform, false);
            frameSr = frameGo.GetComponent<SpriteRenderer>();
        }
        frameSr.sprite = LoadSprite(WhiteSquarePath);
        frameSr.color = PaletteFrameNavy;
        frameSr.transform.localPosition = Vector3.zero;
        frameSr.transform.localScale = Vector3.one * 1.15f;
        frameSr.sortingOrder = 3;

        // アイコン・ラベルは枠の上に
        foreach (string childName in new[] { "SlotIcon", "SlotLabel" })
        {
            Transform t = slot.transform.Find(childName);
            if (t == null) continue;
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 5;
        }

        // ラベルに生成りの台を付ける（カウンターの木目でも読めるように）
        Transform labelTr = slot.transform.Find("SlotLabel");
        if (labelTr != null)
        {
            SpriteRenderer labelSr = labelTr.GetComponent<SpriteRenderer>();
            if (labelSr != null && labelSr.sprite != null)
            {
                Transform backTr = labelTr.Find("Backing");
                SpriteRenderer backSr;
                if (backTr != null)
                    backSr = backTr.GetComponent<SpriteRenderer>();
                else
                {
                    GameObject backGo = new GameObject("Backing", typeof(SpriteRenderer));
                    backGo.transform.SetParent(labelTr, false);
                    backSr = backGo.GetComponent<SpriteRenderer>();
                }
                backSr.sprite = LoadSprite(WhiteSquarePath);
                backSr.color = PaletteLabelBacking;
                backSr.transform.localPosition = Vector3.zero;
                float bx = 1.15f * labelSr.sprite.rect.width / 128f;
                float by = 1.30f * labelSr.sprite.rect.height / 128f;
                backSr.transform.localScale = new Vector3(bx, by, 1f);
                backSr.sortingOrder = 4;
            }
        }

        // 湯気は枠・土台の上に。steamRoot の参照も確認する（保温庫のみ）
        Transform steamTr = slot.transform.Find("SteamPuffs");
        if (steamTr != null)
        {
            foreach (SpriteRenderer puffSr in steamTr.GetComponentsInChildren<SpriteRenderer>(true))
                puffSr.sortingOrder = 6;
            DropTarget dropTarget = slot.GetComponent<DropTarget>();
            if (dropTarget != null)
            {
                SerializedObject so = new SerializedObject(dropTarget);
                so.FindProperty("steamRoot").objectReferenceValue = steamTr.gameObject;
                so.ApplyModifiedProperties();
            }
        }
        Debug.Log($"[WetTowel] {slotName} を ({x}, {SlotRowY}) に移し、枠を付けました。");
    }

    private static void SetupRound6SpriteImports()
    {
        SetSpriteImport("Assets/Image/shop_bg.png", 108);
        SetSpriteImport("Assets/Image/title_bg.png", 100);
        SetSpriteImport("Assets/Image/customer.png", 200);
        SetSpriteImport("Assets/Image/customer_happy.png", 200);
        SetSpriteImport("Assets/Image/customer_angry.png", 200);
        SetSpriteImport("Assets/Image/customer_b.png", 200);
        SetSpriteImport("Assets/Image/customer_b_happy.png", 200);
        SetSpriteImport("Assets/Image/customer_b_angry.png", 200);
        SetSpriteImport("Assets/Image/customer_c.png", 200);
        SetSpriteImport("Assets/Image/customer_c_happy.png", 200);
        SetSpriteImport("Assets/Image/customer_c_angry.png", 200);
    }

    // 客プレハブに3パターンの見た目（通常・笑顔・怒り顔）を割り当てる
    private static void ApplyRound6CustomerPrefab()
    {
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(CustomerPrefabPath);
        Customer customer = customerRoot.GetComponent<Customer>();
        if (customer == null)
        {
            Debug.LogError("[WetTowel] Customerプレハブに Customer がありません。");
            PrefabUtility.UnloadPrefabContents(customerRoot);
            return;
        }
        SerializedObject so = new SerializedObject(customer);
        SerializedProperty looks = so.FindProperty("looks");
        if (looks == null)
        {
            Debug.LogError("[WetTowel] Customer.looks フィールドが見つかりません。");
            PrefabUtility.UnloadPrefabContents(customerRoot);
            return;
        }
        looks.arraySize = 3;
        string[] normal = { "Assets/Image/customer.png", "Assets/Image/customer_b.png", "Assets/Image/customer_c.png" };
        string[] happy = { "Assets/Image/customer_happy.png", "Assets/Image/customer_b_happy.png", "Assets/Image/customer_c_happy.png" };
        string[] angry = { "Assets/Image/customer_angry.png", "Assets/Image/customer_b_angry.png", "Assets/Image/customer_c_angry.png" };
        for (int i = 0; i < 3; i++)
        {
            SerializedProperty look = looks.GetArrayElementAtIndex(i);
            look.FindPropertyRelative("normal").objectReferenceValue = LoadSprite(normal[i]);
            look.FindPropertyRelative("happy").objectReferenceValue = LoadSprite(happy[i]);
            look.FindPropertyRelative("angry").objectReferenceValue = LoadSprite(angry[i]);
        }
        // フォールバック（looks未設定時）の表情も新しい絵にしておく
        so.FindProperty("normalSprite").objectReferenceValue = LoadSprite("Assets/Image/customer.png");
        so.FindProperty("happySprite").objectReferenceValue = LoadSprite("Assets/Image/customer_happy.png");
        so.FindProperty("angrySprite").objectReferenceValue = LoadSprite("Assets/Image/customer_angry.png");
        so.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(customerRoot, CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);
        Debug.Log("[WetTowel] Customerプレハブに3パターンの見た目を割り当てました。");
    }

    private static void ApplyRound6GameScene()
    {
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);
        // 店の背景は差し替え済み（同じアセットパス・GUID）。再割り当てして確実にする。
        GameObject shopBg = FindInScene(gameScene, "ShopBackground");
        if (shopBg != null)
        {
            SpriteRenderer bgSr = shopBg.GetComponent<SpriteRenderer>();
            if (bgSr != null) bgSr.sprite = LoadSprite("Assets/Image/shop_bg.png");
        }
        RestyleHelpButton(gameScene);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[WetTowel] ゲーム画面のRound6修正が完了しました。");
    }

    private static void ApplyRound6TitleScene()
    {
        Scene titleScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Title_Windows.unity", OpenSceneMode.Single);
        Canvas canvas = FindMainCanvas();
        if (canvas != null)
            AddFullscreenBackground(canvas, "TitleBackground", LoadSprite("Assets/Image/title_bg.png"));
        foreach (string buttonName in new[] { "StartButton", "TutorialButton", "EndButton" })
        {
            GameObject buttonGo = FindInScene(titleScene, buttonName);
            if (buttonGo == null) continue;
            Button button = buttonGo.GetComponent<Button>();
            if (button != null) RestyleMenuButtonCream(button);
        }
        RestyleHelpButton(titleScene);
        EditorSceneManager.MarkSceneDirty(titleScene);
        EditorSceneManager.SaveScene(titleScene);
        Debug.Log("[WetTowel] タイトル画面のRound6修正が完了しました。");
    }

    private static void ApplyRound6SelectScene()
    {
        Scene selectScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Select_Windows.unity", OpenSceneMode.Single);
        Canvas canvas = FindMainCanvas();
        if (canvas != null)
            AddFullscreenBackground(canvas, "SelectBackground", LoadSprite("Assets/Image/title_bg.png"));
        // 遊び方・ルールパネル内の小さいボタンは白パネルの上で読めるので変えない
        foreach (Button button in Object.FindObjectsOfType<Button>(true))
        {
            if (HasAncestor(button.transform, "RulePanel")) continue;
            if (HasAncestor(button.transform, "TutorialPanel")) continue;
            RestyleMenuButtonCream(button);
        }
        RestyleHelpButton(selectScene);
        EditorSceneManager.MarkSceneDirty(selectScene);
        EditorSceneManager.SaveScene(selectScene);
        Debug.Log("[WetTowel] セレクト画面のRound6修正が完了しました。");
    }

    // メニューボタンを生成り＋藍文字＋濃い縁取りにする。配置・サイズ・文言は変えない。
    private static void RestyleMenuButtonCream(Button button)
    {
        Image img = button.GetComponent<Image>();
        if (img != null) img.color = PaletteCream;
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.selectedColor = Color.white;
        button.colors = cb;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.color = PaletteIndigo;
        // 濃い縁取り（背景が生成りの場所でもボタンが見えるように）
        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
            outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = PaletteButtonOutline;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = false;
    }

    // 「?」ボタン（HelpButton、または「?」の文字を持つボタン）を見やすくする
    private static void RestyleHelpButton(Scene scene)
    {
        GameObject helpGo = FindInScene(scene, "HelpButton");
        Button button = null;
        if (helpGo != null)
            button = helpGo.GetComponent<Button>();
        if (button == null)
        {
            foreach (Button b in Object.FindObjectsOfType<Button>(true))
            {
                Text label = b.GetComponentInChildren<Text>();
                if (label != null && label.text == "?")
                {
                    button = b;
                    break;
                }
            }
        }
        if (button != null)
        {
            RestyleMenuButtonCream(button);
            Debug.Log($"[WetTowel] HelpButton を見やすくしました: {button.gameObject.name}");
        }
        else
        {
            Debug.Log("[WetTowel] HelpButton が見つかりません（このシーンには無い場合もあります）。");
        }
    }

    // キャンバスの最背面にフルスクリーンの背景画像を置く（再実行時は再利用）
    private static void AddFullscreenBackground(Canvas canvas, string name, Sprite sprite)
    {
        Transform bgTr = canvas.transform.Find(name);
        Image bgImage;
        if (bgTr != null)
            bgImage = bgTr.GetComponent<Image>();
        else
        {
            GameObject bgGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvas.transform, false);
            bgGo.transform.SetAsFirstSibling();
            bgImage = bgGo.GetComponent<Image>();
        }
        bgImage.sprite = sprite;
        bgImage.color = Color.white;
        RectTransform rt = bgImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static bool HasAncestor(Transform t, string name)
    {
        Transform p = t.parent;
        while (p != null)
        {
            if (p.gameObject.name == name) return true;
            p = p.parent;
        }
        return false;
    }

    // メニューボタンを配色に合わせる（藍のボタン＋生成りの文字）。配置・仕様は変えない。
    private static void RestyleMenuButton(Button button, Color bg, Color textColor)
    {
        Image img = button.GetComponent<Image>();
        if (img != null) img.color = bg;
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.selectedColor = Color.white;
        button.colors = cb;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.color = textColor;
    }

    // 遊び方・ルールパネル1件分のRound4修正
    private static void ApplyRound4HowToPanel(string scenePath, string panelName)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject panel = FindInScene(scene, panelName);
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
        SetAnchored(headingText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
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

    // 遊び方・ルールパネルの本文を中央寄せにする（白パネルの中央に文が来る）。
    private static void CenterHowToBodyText(Scene scene, string panelName)
    {
        GameObject panel = FindInScene(scene, panelName);
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
        GameObject go = FindInScene(scene, objectName);
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

    // ---- ゲーム画面 ----

    private static void SetupGameScene(string scenePath, Sprite whiteSquare)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        // Steam向けはWindows版のみ。モバイル版シーンは触らない。
        if (scenePath.Contains("/Mobile/"))
            return;

        // 欠けていた枠類スプライトの張り直し
        foreach (SpriteRenderer sr in Object.FindObjectsOfType<SpriteRenderer>(true))
        {
            if (sr.sprite == null && SlotObjectNames.Contains(sr.gameObject.name))
            {
                sr.sprite = whiteSquare;
                Debug.Log($"[WetTowel] {scenePath}: {sr.gameObject.name} に白四角スプライトを設定しました。");
            }
        }

        // ComboTextのtypo修正
        foreach (Text t in Object.FindObjectsOfType<Text>(true))
        {
            if (t.text.Contains("Conbo"))
            {
                t.text = "COMBO";
                Debug.Log($"[WetTowel] {scenePath}: ComboTextのtypoを修正しました。");
            }
        }

        Canvas mainCanvas = FindMainCanvas();
        if (mainCanvas == null)
        {
            Debug.LogError($"[WetTowel] {scenePath}: メインCanvasが見つかりません。");
            return;
        }

        Font font = null;
        Text anyText = Object.FindObjectOfType<Text>();
        if (anyText != null)
            font = anyText.font;

        // 操作ヒントバー（画面下部・常時表示）
        if (mainCanvas.transform.Find("HintBar") == null)
        {
            Text hint = CreateLabel(mainCanvas.transform, "HintBar",
                "ドラッグ: ストック → 保温/保冷で温度調整 → 客へ提供　　Esc: 終了", font);
            hint.fontSize = 26;
            hint.color = Color.white;
            hint.alignment = TextAnchor.MiddleCenter;
            SetAnchored(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(1500, 44));
            Debug.Log($"[WetTowel] {scenePath}: 操作ヒントバーを追加しました。");
        }

        // 「？」ボタン（画面右上・チュートリアル再表示）
        Button helpButton = null;
        if (mainCanvas.transform.Find("HelpButton") == null)
        {
            helpButton = CreateTextButton(mainCanvas.transform, "HelpButton", "？", font, new Color(1f, 0.95f, 0.7f));
            helpButton.GetComponentInChildren<Text>().fontSize = 40;
            SetAnchored(helpButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-70f, -70f), new Vector2(90, 90));
            Debug.Log($"[WetTowel] {scenePath}: 「？」ボタンを追加しました。");
        }
        else
        {
            helpButton = mainCanvas.transform.Find("HelpButton").GetComponent<Button>();
        }

        // チュートリアルオーバーレイ
        if (mainCanvas.transform.Find("TutorialOverlay") == null)
        {
            GameObject overlay = new GameObject("TutorialOverlay", typeof(RectTransform), typeof(Image), typeof(GameTutorial));
            overlay.transform.SetParent(mainCanvas.transform, false);
            RectTransform overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero;
            overlayRt.anchorMax = Vector2.one;
            overlayRt.offsetMin = Vector2.zero;
            overlayRt.offsetMax = Vector2.zero;
            Image dim = overlay.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f);

            // 下部の説明パネル。画面下にはみ出さないよう、ボックス下端が
            // 画面下から40px上になる位置（中心y=170）に置く。
            GameObject box = new GameObject("TutorialBox", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(overlay.transform, false);
            box.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.95f);
            SetAnchored(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 170f), new Vector2(1100, 260));

            // 説明文はボックス内に収める（ボタン分の下部70pxを空ける）。
            // 以前の (0, 70) / (-32, -16) では文がボックスの上に
            // はみ出していた。
            Text stepText = CreateLabel(box.transform, "StepText", "", font);
            stepText.color = Color.black;
            stepText.fontSize = 30;
            stepText.alignment = TextAnchor.UpperLeft;
            SetAnchored(stepText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 27f), new Vector2(-32f, -86f));

            Text stepLabel = CreateLabel(box.transform, "StepLabel", "1 / 5", font);
            stepLabel.color = Color.black;
            stepLabel.fontSize = 24;
            stepLabel.alignment = TextAnchor.MiddleCenter;
            SetAnchored(stepLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 30f), new Vector2(140, 40));

            Button nextBtn = CreateTextButton(box.transform, "NextButton", "次へ", font, new Color(0.6f, 0.9f, 0.6f));
            SetAnchored(nextBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-110f, 32f), new Vector2(170, 56));

            Button skipBtn = CreateTextButton(box.transform, "SkipButton", "スキップ", font, new Color(0.85f, 0.85f, 0.85f));
            SetAnchored(skipBtn.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(110f, 32f), new Vector2(170, 56));

            GameTutorial tutorial = overlay.GetComponent<GameTutorial>();
            SerializedObject so = new SerializedObject(tutorial);
            so.FindProperty("panel").objectReferenceValue = overlay;
            so.FindProperty("stepText").objectReferenceValue = stepText;
            so.FindProperty("nextButton").objectReferenceValue = nextBtn;
            so.FindProperty("skipButton").objectReferenceValue = skipBtn;
            so.FindProperty("stepLabel").objectReferenceValue = stepLabel;
            so.FindProperty("helpButton").objectReferenceValue = helpButton;
            so.ApplyModifiedProperties();

            // 注意: overlay 自体を SetActive(false) で保存すると、シーン読込時に
            // GameTutorial.Start() が一度も呼ばれず、初回チュートリアルも「？」ボタンも
            // 動かない（コルーチンも開始されない）。overlay はアクティブなまま保存し、
            // 見た目だけ（暗転ImageとTutorialBox）を隠しておく。表示/非表示は
            // 実行時に GameTutorial.SetPanelVisible() が切り替える。
            overlay.GetComponent<Image>().enabled = false;
            box.SetActive(false);
            Debug.Log($"[WetTowel] {scenePath}: チュートリアルオーバーレイを追加しました。");
        }

        // オーバーレイはCanvasの最前面に置く（HUDより手前に描くため）。
        // 再実行時も含めて毎回適用する。
        Transform overlayTr = mainCanvas.transform.Find("TutorialOverlay");
        if (overlayTr != null)
            overlayTr.SetAsLastSibling();

        // チュートリアルボックスのレイアウト修正（再実行時も適用）。
        // 既存シーンのボックスは作成時の古い配置（中心y=120、説明文が
        // ボックス上にはみ出し、ボタンが画面下で切れる）のままなので、
        // ここで新しい配置に直す。
        if (overlayTr != null)
        {
            Transform boxTr = overlayTr.Find("TutorialBox");
            if (boxTr != null)
            {
                SetAnchored(boxTr.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 170f), new Vector2(1100, 260));
                Transform stTr = boxTr.Find("StepText");
                if (stTr != null)
                    SetAnchored(stTr.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f),
                        new Vector2(0f, 27f), new Vector2(-32f, -86f));
                Transform nxTr = boxTr.Find("NextButton");
                if (nxTr != null)
                    SetAnchored(nxTr.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-110f, 32f), new Vector2(170, 56));
                Transform skTr = boxTr.Find("SkipButton");
                if (skTr != null)
                    SetAnchored(skTr.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(110f, 32f), new Vector2(170, 56));
                Transform lbTr = boxTr.Find("StepLabel");
                if (lbTr != null)
                    SetAnchored(lbTr.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 30f), new Vector2(140, 40));
                Debug.Log($"[WetTowel] {scenePath}: チュートリアルボックスのレイアウトを修正しました。");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[WetTowel] {scenePath}: ゲーム画面のセットアップが完了しました。");
    }

    // ---- ヘルパー ----

    // 非アクティブなオブジェクトも含めてシーン内を名前で探す
    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindInChildren(root.transform, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static GameObject FindInChildren(Transform parent, string name)
    {
        if (parent.gameObject.name == name)
            return parent.gameObject;
        foreach (Transform child in parent)
        {
            GameObject found = FindInChildren(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private static Canvas FindMainCanvas()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
        {
            if (c.transform.parent == null && c.gameObject.name == "Canvas")
                return c;
        }
        return null;
    }

    private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private static Button CreateTextButton(Transform parent, string name, string label, Font font, Color bgColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.color = bgColor;
        Button button = go.GetComponent<Button>();

        GameObject labelGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(go.transform, false);
        Text text = labelGo.GetComponent<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = 28;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        return button;
    }

    private static Text CreateLabel(Transform parent, string name, string content, Font font)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = 28;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        return text;
    }
}
