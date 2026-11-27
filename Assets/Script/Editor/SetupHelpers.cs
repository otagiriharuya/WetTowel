using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 複数のラウンドで使う共通ヘルパー（シーン内検索・UI生成・スプライト読込）。
internal static class SetupHelpers
{
    internal static void SetSpriteImport(string path, int pixelsPerUnit)
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

    internal static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogError($"[WetTowel] スプライトが読み込めません: {path}");
        return sprite;
    }

    // キャンバスの最背面にフルスクリーンの背景画像を置く（再実行時は再利用）
    internal static void AddFullscreenBackground(Canvas canvas, string name, Sprite sprite)
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

    internal static bool HasAncestor(Transform t, string name)
    {
        Transform p = t.parent;
        while (p != null)
        {
            if (p.gameObject.name == name) return true;
            p = p.parent;
        }
        return false;
    }

    // ---- ヘルパー ----

    // 非アクティブなオブジェクトも含めてシーン内を名前で探す
    internal static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject found = FindInChildren(root.transform, name);
            if (found != null)
                return found;
        }
        return null;
    }

    internal static GameObject FindInChildren(Transform parent, string name)
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

    internal static Canvas FindMainCanvas()
    {
        foreach (Canvas c in Object.FindObjectsOfType<Canvas>(true))
        {
            if (c.transform.parent == null && c.gameObject.name == "Canvas")
                return c;
        }
        return null;
    }

    internal static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    internal static Button CreateTextButton(Transform parent, string name, string label, Font font, Color bgColor)
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

    internal static Text CreateLabel(Transform parent, string name, string content, Font font)
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
