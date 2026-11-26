using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Round5（見た目改善）：表情スプライト・店の背景・スロットのアイコンとラベル・湯気・配色。
public static class Round5Fixes
{
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
        SetupHelpers.SetSpriteImport("Assets/Image/customer.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/wet_towel.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_happy.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_angry.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/shop_bg.png", 108);
        SetupHelpers.SetSpriteImport("Assets/Image/title_bg.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/sparkle.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/steam.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/icon_warmer.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/icon_cooler.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/icon_stock.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/icon_trash.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/label_warmer.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/label_cooler.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/label_stock.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/label_trash.png", 100);
    }

    // 客プレハブに表情スプライトとキラキラを割り当てる
    private static void ApplyRound5CustomerPrefab()
    {
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(SetupConstants.CustomerPrefabPath);
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
        so.FindProperty("normalSprite").objectReferenceValue = SetupHelpers.LoadSprite("Assets/Image/customer.png");
        so.FindProperty("happySprite").objectReferenceValue = SetupHelpers.LoadSprite("Assets/Image/customer_happy.png");
        so.FindProperty("angrySprite").objectReferenceValue = SetupHelpers.LoadSprite("Assets/Image/customer_angry.png");
        so.FindProperty("sparkleSprite").objectReferenceValue = SetupHelpers.LoadSprite("Assets/Image/sparkle.png");
        so.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(customerRoot, SetupConstants.CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);
        Debug.Log("[WetTowel] Customerプレハブに表情・キラキラを割り当てました。");
    }

    private static void ApplyRound5GameScene()
    {
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);

        // 店の背景（最背面）。カメラは orthographic size 5 なので、
        // 画面は 17.78 x 10 ユニット。shop_bg は PPU 108 でちょうどその大きさになる。
        GameObject shopBg = SetupHelpers.FindInScene(gameScene, "ShopBackground");
        if (shopBg == null)
        {
            shopBg = new GameObject("ShopBackground", typeof(SpriteRenderer));
            Debug.Log("[WetTowel] ShopBackground を作成しました。");
        }
        SpriteRenderer bgSr = shopBg.GetComponent<SpriteRenderer>();
        bgSr.sprite = SetupHelpers.LoadSprite("Assets/Image/shop_bg.png");
        bgSr.sortingOrder = -10;
        shopBg.transform.position = Vector3.zero;

        // スロット：土台を生成り色にし、アイコンとラベルを付ける
        ApplyRound5Slot(gameScene, "WarmerSlot", "Assets/Image/icon_warmer.png", "Assets/Image/label_warmer.png");
        ApplyRound5Slot(gameScene, "CoolerSlot", "Assets/Image/icon_cooler.png", "Assets/Image/label_cooler.png");
        ApplyRound5Slot(gameScene, "WetTowelSpawner", "Assets/Image/icon_stock.png", "Assets/Image/label_stock.png");
        ApplyRound5Slot(gameScene, "TrashCan", "Assets/Image/icon_trash.png", "Assets/Image/label_trash.png");

        // 保温庫の湯気
        GameObject warmer = SetupHelpers.FindInScene(gameScene, "WarmerSlot");
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
            Sprite steamSprite = SetupHelpers.LoadSprite("Assets/Image/steam.png");
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
        GameObject background = SetupHelpers.FindInScene(gameScene, "Background");
        if (background != null)
        {
            Image bgImage = background.GetComponent<Image>();
            if (bgImage != null) bgImage.color = SetupConstants.PaletteResultCream;
        }
        GameObject finalScore = SetupHelpers.FindInScene(gameScene, "FinalScoreText");
        if (finalScore != null)
        {
            Text t = finalScore.GetComponent<Text>();
            if (t != null) t.color = SetupConstants.PaletteIndigo;
        }
        GameObject highScore = SetupHelpers.FindInScene(gameScene, "HighScoreText");
        if (highScore != null)
        {
            Text t = highScore.GetComponent<Text>();
            if (t != null) t.color = SetupConstants.PaletteWarmRed;
        }
        GameObject resultPanel = SetupHelpers.FindInScene(gameScene, "ResultPanell");
        if (resultPanel != null)
        {
            foreach (Button button in resultPanel.GetComponentsInChildren<Button>(true))
                RestyleMenuButton(button, SetupConstants.PaletteIndigo, SetupConstants.PaletteCream);
        }

        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[WetTowel] ゲーム画面のRound5修正が完了しました。");
    }

    // スロット1件分：土台の色を生成りにし、アイコンとラベルを子として付ける
    private static void ApplyRound5Slot(Scene scene, string slotName, string iconPath, string labelPath)
    {
        GameObject slot = SetupHelpers.FindInScene(scene, slotName);
        if (slot == null)
        {
            Debug.LogError($"[WetTowel] {slotName} が見つかりません。");
            return;
        }
        SpriteRenderer baseSr = slot.GetComponent<SpriteRenderer>();
        if (baseSr != null)
            baseSr.color = SetupConstants.PaletteSlotCream;
        AddSlotChild(slot, "SlotIcon", SetupHelpers.LoadSprite(iconPath), new Vector3(0f, 0.05f, -0.1f), 0.4f, 1);
        AddSlotChild(slot, "SlotLabel", SetupHelpers.LoadSprite(labelPath), new Vector3(0f, 0.85f, -0.1f), 0.5f, 1);
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
        Canvas canvas = SetupHelpers.FindMainCanvas();
        if (canvas != null)
            SetupHelpers.AddFullscreenBackground(canvas, "TitleBackground", SetupHelpers.LoadSprite("Assets/Image/title_bg.png"));

        GameObject logo = SetupHelpers.FindInScene(titleScene, "TitleLogo");
        if (logo != null)
        {
            Image logoImage = logo.GetComponent<Image>();
            if (logoImage != null) logoImage.color = SetupConstants.PaletteLogoCream;
        }
        foreach (string buttonName in new[] { "StartButton", "TutorialButton", "EndButton" })
        {
            GameObject buttonGo = SetupHelpers.FindInScene(titleScene, buttonName);
            if (buttonGo == null) continue;
            Button button = buttonGo.GetComponent<Button>();
            if (button != null) RestyleMenuButton(button, SetupConstants.PaletteIndigo, SetupConstants.PaletteCream);
        }
        EditorSceneManager.MarkSceneDirty(titleScene);
        EditorSceneManager.SaveScene(titleScene);
        Debug.Log("[WetTowel] タイトル画面のRound5修正が完了しました。");
    }

    private static void ApplyRound5SelectScene()
    {
        Scene selectScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Select_Windows.unity", OpenSceneMode.Single);
        Canvas canvas = SetupHelpers.FindMainCanvas();
        if (canvas != null)
            SetupHelpers.AddFullscreenBackground(canvas, "SelectBackground", SetupHelpers.LoadSprite("Assets/Image/title_bg.png"));

        // ルールパネル内の小さいボタン（前へ・次へ・閉じる）は変えない
        foreach (Button button in Object.FindObjectsOfType<Button>(true))
        {
            if (SetupHelpers.HasAncestor(button.transform, "RulePanel")) continue;
            RestyleMenuButton(button, SetupConstants.PaletteIndigo, SetupConstants.PaletteCream);
        }
        EditorSceneManager.MarkSceneDirty(selectScene);
        EditorSceneManager.SaveScene(selectScene);
        Debug.Log("[WetTowel] セレクト画面のRound5修正が完了しました。");
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
}
