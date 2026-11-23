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
