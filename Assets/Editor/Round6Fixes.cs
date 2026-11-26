using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Round6（居酒屋らしさ・ボタンの見やすさ・客バリエーション・表情）。
public static class Round6Fixes
{
    // ---- Round6: 居酒屋らしさ・ボタンの見やすさ・客バリエーション・表情 ----
    // 舞台は居酒屋。タイトルに赤提灯・のれん、ゲーム画面にお品書き・酒瓶・のれん。
    // ボタンは生成り＋藍文字＋濃い縁取りにして背景から見やすくする。
    // 客は3パターン（来店ごとにランダム）、表情は大きく描き直した絵に差し替える。
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

    private static void SetupRound6SpriteImports()
    {
        SetupHelpers.SetSpriteImport("Assets/Image/shop_bg.png", 108);
        SetupHelpers.SetSpriteImport("Assets/Image/title_bg.png", 100);
        SetupHelpers.SetSpriteImport("Assets/Image/customer.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_happy.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_angry.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_b.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_b_happy.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_b_angry.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_c.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_c_happy.png", 200);
        SetupHelpers.SetSpriteImport("Assets/Image/customer_c_angry.png", 200);
    }

    // 客プレハブに3パターンの見た目（通常・笑顔・怒り顔）を割り当てる
    private static void ApplyRound6CustomerPrefab()
    {
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(SetupConstants.CustomerPrefabPath);
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
            look.FindPropertyRelative("normal").objectReferenceValue = SetupHelpers.LoadSprite(normal[i]);
            look.FindPropertyRelative("happy").objectReferenceValue = SetupHelpers.LoadSprite(happy[i]);
            look.FindPropertyRelative("angry").objectReferenceValue = SetupHelpers.LoadSprite(angry[i]);
        }
        // フォールバック（looks未設定時）の表情も新しい絵にしておく
        so.FindProperty("normalSprite").objectReferenceValue = SetupHelpers.LoadSprite("Assets/Image/customer.png");
        so.FindProperty("happySprite").objectReferenceValue = SetupHelpers.LoadSprite("Assets/Image/customer_happy.png");
        so.FindProperty("angrySprite").objectReferenceValue = SetupHelpers.LoadSprite("Assets/Image/customer_angry.png");
        so.ApplyModifiedProperties();
        PrefabUtility.SaveAsPrefabAsset(customerRoot, SetupConstants.CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);
        Debug.Log("[WetTowel] Customerプレハブに3パターンの見た目を割り当てました。");
    }

    private static void ApplyRound6GameScene()
    {
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);
        // 店の背景は差し替え済み（同じアセットパス・GUID）。再割り当てして確実にする。
        GameObject shopBg = SetupHelpers.FindInScene(gameScene, "ShopBackground");
        if (shopBg != null)
        {
            SpriteRenderer bgSr = shopBg.GetComponent<SpriteRenderer>();
            if (bgSr != null) bgSr.sprite = SetupHelpers.LoadSprite("Assets/Image/shop_bg.png");
        }
        RestyleHelpButton(gameScene);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);
        Debug.Log("[WetTowel] ゲーム画面のRound6修正が完了しました。");
    }

    private static void ApplyRound6TitleScene()
    {
        Scene titleScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Title_Windows.unity", OpenSceneMode.Single);
        Canvas canvas = SetupHelpers.FindMainCanvas();
        if (canvas != null)
            SetupHelpers.AddFullscreenBackground(canvas, "TitleBackground", SetupHelpers.LoadSprite("Assets/Image/title_bg.png"));
        foreach (string buttonName in new[] { "StartButton", "TutorialButton", "EndButton" })
        {
            GameObject buttonGo = SetupHelpers.FindInScene(titleScene, buttonName);
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
        Canvas canvas = SetupHelpers.FindMainCanvas();
        if (canvas != null)
            SetupHelpers.AddFullscreenBackground(canvas, "SelectBackground", SetupHelpers.LoadSprite("Assets/Image/title_bg.png"));
        // 遊び方・ルールパネル内の小さいボタンは白パネルの上で読めるので変えない
        foreach (Button button in Object.FindObjectsOfType<Button>(true))
        {
            if (SetupHelpers.HasAncestor(button.transform, "RulePanel")) continue;
            if (SetupHelpers.HasAncestor(button.transform, "TutorialPanel")) continue;
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
        if (img != null) img.color = SetupConstants.PaletteCream;
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.96f, 0.96f, 0.96f, 1f);
        cb.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        cb.selectedColor = Color.white;
        button.colors = cb;
        Text label = button.GetComponentInChildren<Text>();
        if (label != null) label.color = SetupConstants.PaletteIndigo;
        // 濃い縁取り（背景が生成りの場所でもボタンが見えるように）
        Outline outline = button.GetComponent<Outline>();
        if (outline == null)
            outline = button.gameObject.AddComponent<Outline>();
        outline.effectColor = SetupConstants.PaletteButtonOutline;
        outline.effectDistance = new Vector2(4f, -4f);
        outline.useGraphicAlpha = false;
    }

    // 「?」ボタン（HelpButton、または「?」の文字を持つボタン）を見やすくする
    private static void RestyleHelpButton(Scene scene)
    {
        GameObject helpGo = SetupHelpers.FindInScene(scene, "HelpButton");
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
}
