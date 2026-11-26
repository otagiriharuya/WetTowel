using UnityEditor;
using UnityEngine;

// Round8（客を約1.4倍に大きくする）。
public static class Round8Fixes
{
    // Round8: 客を約1.4倍に大きくする（顔と肩がはっきり読めるように）。
    // 構造：Customer ルート > MainUnit（体スプライト、scale 0.5）> Canvas（scale 0.01、吹き出し・ゲージ）
    // 体だけを大きくし、吹き出し・ゲージは同じ大きさに保つため Canvas を 1/1.4 で打ち消す。
    // 頭が高くなる分、吹き出しを上げる（anchoredPosition y 241 → 300）。
    // 提供の当たり判定（CircleCollider2D）も体に合わせて 0.5 → 0.7 にする。
    [MenuItem("WetTowel/Apply Round8 Fixes")]
    public static void ApplyRound8Fixes()
    {
        Debug.Log("[WetTowel] Round8 fixes を開始します。");
        const float bodyScale = 1.4f;
        GameObject customerRoot = PrefabUtility.LoadPrefabContents(SetupConstants.CustomerPrefabPath);

        Transform mainUnit = customerRoot.transform.Find("MainUnit");
        if (mainUnit == null)
        {
            Debug.LogError("[WetTowel] MainUnit が見つかりません。");
        }
        else
        {
            mainUnit.localScale = Vector3.one * (0.5f * bodyScale);
            Debug.Log($"[WetTowel] MainUnit の scale を 0.5 → {0.5f * bodyScale} にしました。");

            Transform canvas = mainUnit.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[WetTowel] MainUnit/Canvas が見つかりません。");
            }
            else
            {
                // 吹き出し・ゲージの表示サイズを維持（MainUnit の拡大を打ち消す）
                canvas.localScale = Vector3.one * (0.01f / bodyScale);
                Debug.Log("[WetTowel] Canvas の scale を打ち消して吹き出し・ゲージの大きさを維持しました。");

                Transform bubble = canvas.Find("Image");
                if (bubble == null)
                {
                    Debug.LogError("[WetTowel] 吹き出し Image が見つかりません。");
                }
                else
                {
                    RectTransform bubbleRt = bubble as RectTransform;
                    if (bubbleRt != null)
                    {
                        // 頭が高くなった分だけ吹き出しを上げる（頭の上に隙間ができる位置）
                        bubbleRt.anchoredPosition = new Vector2(0f, 300f);
                        Debug.Log("[WetTowel] 吹き出しを上げました（anchoredPosition y 241 → 300）。");
                    }
                }
            }
        }

        CircleCollider2D collider = customerRoot.GetComponent<CircleCollider2D>();
        if (collider == null)
        {
            Debug.LogError("[WetTowel] CircleCollider2D が見つかりません。");
        }
        else
        {
            collider.radius = 0.5f * bodyScale;
            Debug.Log($"[WetTowel] CircleCollider2D の radius を 0.5 → {0.5f * bodyScale} にしました。");
        }

        PrefabUtility.SaveAsPrefabAsset(customerRoot, SetupConstants.CustomerPrefabPath);
        PrefabUtility.UnloadPrefabContents(customerRoot);
        AssetDatabase.SaveAssets();
        Debug.Log("[WetTowel] Round8 fixes が完了しました。");
    }
}
