using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 初回セットアップ（WetTowel/Setup Completion）と参照検証。
// スプライトのSprite化・プレハブの張り直し、遊び方・ルールパネルのページ式化、
// ゲーム画面へのチュートリアル・「？」ボタン・操作ヒントバーの追加を行う。
public static class WetTowelSetup
{
    [MenuItem("WetTowel/Setup Completion")]
    public static void SetupCompletion()
    {
        Debug.Log("[WetTowel] Setup Completion を開始します。");
        ImportSpritesAsSprite();
        AssetDatabase.Refresh();

        Sprite whiteSquare = AssetDatabase.LoadAssetAtPath<Sprite>(SetupConstants.WhiteSquarePath);
        Sprite wetTowel = AssetDatabase.LoadAssetAtPath<Sprite>(SetupConstants.WetTowelPath);
        Sprite customer = AssetDatabase.LoadAssetAtPath<Sprite>(SetupConstants.CustomerPath);
        Debug.Log($"[WetTowel] スプライト読込: white={whiteSquare != null} towel={wetTowel != null} customer={customer != null}");
        if (whiteSquare == null || wetTowel == null || customer == null)
        {
            Debug.LogError("[WetTowel] スプライトの読み込みに失敗しました。中止します。");
            return;
        }

        FixPrefabs(wetTowel, customer);
        foreach (string scene in SetupConstants.TitleScenes) SetupHowToPanel(scene, "TutorialPanel", HowToPlayPanel.PanelKind.Flow);
        foreach (string scene in SetupConstants.SelectScenes) SetupHowToPanel(scene, "RulePanel", HowToPlayPanel.PanelKind.Rules);
        foreach (string scene in SetupConstants.GameScenes) SetupGameScene(scene, whiteSquare);

        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Setup Completion が完了しました。");
    }

    [MenuItem("WetTowel/Verify References")]
    public static void VerifyReferences()
    {
        int problems = 0;
        foreach (string scene in SetupConstants.TitleScenes.Concat(SetupConstants.SelectScenes).Concat(SetupConstants.GameScenes))
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
        foreach (string path in new[] { SetupConstants.WhiteSquarePath, SetupConstants.WetTowelPath, SetupConstants.CustomerPath })
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
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(SetupConstants.CustomerPrefabPath);
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
        PrefabUtility.SaveAsPrefabAsset(customerRoot, SetupConstants.CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);

        // WetTowel.prefab: 2つのSpriteRenderer（影層・本体層）両方に同じスプライト
        GameObject towelRoot = PrefabUtility.LoadPrefabContents(SetupConstants.WetTowelPrefabPath);
        SpriteRenderer[] towelSrs = towelRoot.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in towelSrs)
            sr.sprite = wetTowel;
        Debug.Log($"[WetTowel] WetTowel.prefab のSpriteRenderer {towelSrs.Length} 件にスプライトを設定しました。");
        PrefabUtility.SaveAsPrefabAsset(towelRoot, SetupConstants.WetTowelPrefabPath);
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
        GameObject panel = SetupHelpers.FindInScene(scene, panelName);
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
        Button prev = SetupHelpers.CreateTextButton(buttonParent, "PrevPageButton", "◀ 前へ", font, new Color(0.85f, 0.85f, 0.85f));
        Button next = SetupHelpers.CreateTextButton(buttonParent, "NextPageButton", "次へ ▶", font, new Color(0.85f, 0.85f, 0.85f));
        Text pageLabel = SetupHelpers.CreateLabel(buttonParent, "PageLabel", kind == HowToPlayPanel.PanelKind.Rules ? "1 / 4" : "1 / 3", font);

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
            SetupHelpers.SetAnchored(prevTr.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(90f, 40f), new Vector2(120, 48));
        }
        if (nextTr != null)
        {
            nextTr.SetParent(imgTr, false);
            SetupHelpers.SetAnchored(nextTr.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-90f, 40f), new Vector2(120, 48));
        }
        if (labelTr != null)
        {
            labelTr.SetParent(imgTr, false);
            SetupHelpers.SetAnchored(labelTr.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
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
            if (sr.sprite == null && SetupConstants.SlotObjectNames.Contains(sr.gameObject.name))
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

        Canvas mainCanvas = SetupHelpers.FindMainCanvas();
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
            Text hint = SetupHelpers.CreateLabel(mainCanvas.transform, "HintBar",
                "ドラッグ: ストック → 保温/保冷で温度調整 → 客へ提供　　Esc: 終了", font);
            hint.fontSize = 26;
            hint.color = Color.white;
            hint.alignment = TextAnchor.MiddleCenter;
            SetupHelpers.SetAnchored(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 26f), new Vector2(1500, 44));
            Debug.Log($"[WetTowel] {scenePath}: 操作ヒントバーを追加しました。");
        }

        // 「？」ボタン（画面右上・チュートリアル再表示）
        Button helpButton = null;
        if (mainCanvas.transform.Find("HelpButton") == null)
        {
            helpButton = SetupHelpers.CreateTextButton(mainCanvas.transform, "HelpButton", "？", font, new Color(1f, 0.95f, 0.7f));
            helpButton.GetComponentInChildren<Text>().fontSize = 40;
            SetupHelpers.SetAnchored(helpButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f),
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
            SetupHelpers.SetAnchored(box.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 170f), new Vector2(1100, 260));

            // 説明文はボックス内に収める（ボタン分の下部70pxを空ける）。
            // 以前の (0, 70) / (-32, -16) では文がボックスの上に
            // はみ出していた。
            Text stepText = SetupHelpers.CreateLabel(box.transform, "StepText", "", font);
            stepText.color = Color.black;
            stepText.fontSize = 30;
            stepText.alignment = TextAnchor.UpperLeft;
            SetupHelpers.SetAnchored(stepText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0f, 27f), new Vector2(-32f, -86f));

            Text stepLabel = SetupHelpers.CreateLabel(box.transform, "StepLabel", "1 / 5", font);
            stepLabel.color = Color.black;
            stepLabel.fontSize = 24;
            stepLabel.alignment = TextAnchor.MiddleCenter;
            SetupHelpers.SetAnchored(stepLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 30f), new Vector2(140, 40));

            Button nextBtn = SetupHelpers.CreateTextButton(box.transform, "NextButton", "次へ", font, new Color(0.6f, 0.9f, 0.6f));
            SetupHelpers.SetAnchored(nextBtn.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-110f, 32f), new Vector2(170, 56));

            Button skipBtn = SetupHelpers.CreateTextButton(box.transform, "SkipButton", "スキップ", font, new Color(0.85f, 0.85f, 0.85f));
            SetupHelpers.SetAnchored(skipBtn.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
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
                SetupHelpers.SetAnchored(boxTr.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(0f, 170f), new Vector2(1100, 260));
                Transform stTr = boxTr.Find("StepText");
                if (stTr != null)
                    SetupHelpers.SetAnchored(stTr.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f),
                        new Vector2(0f, 27f), new Vector2(-32f, -86f));
                Transform nxTr = boxTr.Find("NextButton");
                if (nxTr != null)
                    SetupHelpers.SetAnchored(nxTr.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                        new Vector2(-110f, 32f), new Vector2(170, 56));
                Transform skTr = boxTr.Find("SkipButton");
                if (skTr != null)
                    SetupHelpers.SetAnchored(skTr.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                        new Vector2(110f, 32f), new Vector2(170, 56));
                Transform lbTr = boxTr.Find("StepLabel");
                if (lbTr != null)
                    SetupHelpers.SetAnchored(lbTr.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(0f, 30f), new Vector2(140, 40));
                Debug.Log($"[WetTowel] {scenePath}: チュートリアルボックスのレイアウトを修正しました。");
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[WetTowel] {scenePath}: ゲーム画面のセットアップが完了しました。");
    }
}
