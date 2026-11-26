using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Round7（客は椅子に座る構図・枠の見やすさ・質感）。
public static class Round7Fixes
{
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
        SetupHelpers.SetSpriteImport("Assets/Image/shop_bg.png", 108);
        SetupHelpers.SetSpriteImport("Assets/Image/counter_front.png", 108);
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

    // 客プレハブ：我慢ゲージを頭の横へ（カウンターの上に見える位置に）
    private static void ApplyRound7CustomerPrefab()
    {
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(SetupConstants.CustomerPrefabPath);
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
        PrefabUtility.SaveAsPrefabAsset(customerRoot, SetupConstants.CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);
    }

    // おしぼりプレハブ：影をスロットの土台より上に（カウンターの前面より上、タオル本体の下に）
    // 注意：本体はルート（scale 0.5）、影は子（scale 1.2、黒）にある。
    private static void ApplyRound7WetTowelPrefab()
    {
        GameObject towelRoot = PrefabUtility.LoadPrefabContents(SetupConstants.WetTowelPrefabPath);
        SpriteRenderer mainSr = towelRoot.GetComponent<SpriteRenderer>();
        if (mainSr != null)
            mainSr.sortingOrder = 10;
        foreach (SpriteRenderer sr in towelRoot.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.gameObject == towelRoot) continue;
            sr.sortingOrder = 7;
        }
        PrefabUtility.SaveAsPrefabAsset(towelRoot, SetupConstants.WetTowelPrefabPath);
        PrefabUtility.UnloadPrefabContents(towelRoot);
        Debug.Log("[WetTowel] おしぼりの影の sortingOrder を 7、本体を 10 にしました。");
    }

    private static void ApplyRound7GameScene()
    {
        Scene gameScene = EditorSceneManager.OpenScene("Assets/Scenes/Windows/Game_Windows.unity", OpenSceneMode.Single);
        // 背景の差し替え（同じアセットパス・GUID）
        GameObject shopBg = SetupHelpers.FindInScene(gameScene, "ShopBackground");
        if (shopBg != null)
        {
            SpriteRenderer bgSr = shopBg.GetComponent<SpriteRenderer>();
            if (bgSr != null) bgSr.sprite = SetupHelpers.LoadSprite("Assets/Image/shop_bg.png");
        }

        // カウンター前面：客の下半身を隠す。shop_bg のカウンター部分（画面y 800〜1080）を切り出した絵。
        // 1920x280 @ PPU108 → 17.78 x 2.59。中心の画面y 940 → wy = 5 - 940/108 = -3.70
        GameObject counterFront = SetupHelpers.FindInScene(gameScene, "CounterFront");
        if (counterFront == null)
        {
            counterFront = new GameObject("CounterFront", typeof(SpriteRenderer));
            Debug.Log("[WetTowel] CounterFront を作成しました。");
        }
        SpriteRenderer cfSr = counterFront.GetComponent<SpriteRenderer>();
        cfSr.sprite = SetupHelpers.LoadSprite("Assets/Image/counter_front.png");
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
        GameObject slot = SetupHelpers.FindInScene(scene, slotName);
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
        frameSr.sprite = SetupHelpers.LoadSprite(SetupConstants.WhiteSquarePath);
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
                backSr.sprite = SetupHelpers.LoadSprite(SetupConstants.WhiteSquarePath);
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
}
