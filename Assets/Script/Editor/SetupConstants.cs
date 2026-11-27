using System.Collections.Generic;
using UnityEngine;

// WetTowelセットアップ用の共通定数（パス・シーン一覧・配色）。
// Round5の配色はRound6のボタン再配色でも使うため、ラウンド固有ではなく共有にする。
internal static class SetupConstants
{
    internal const string WhiteSquarePath = "Assets/Image/white_square.png";
    internal const string WetTowelPath = "Assets/Image/wet_towel.png";
    internal const string CustomerPath = "Assets/Image/customer.png";
    internal const string CustomerPrefabPath = "Assets/Prefab/Customer.prefab";
    internal const string WetTowelPrefabPath = "Assets/Prefab/WetTowel.prefab";

    internal static readonly string[] TitleScenes =
        { "Assets/Scenes/Windows/Title_Windows.unity", "Assets/Scenes/Mobile/Title_Mobile.unity" };
    internal static readonly string[] SelectScenes =
        { "Assets/Scenes/Windows/Select_Windows.unity", "Assets/Scenes/Mobile/Select_Mobile.unity" };
    internal static readonly string[] GameScenes =
        { "Assets/Scenes/Windows/Game_Windows.unity", "Assets/Scenes/Mobile/Game_Mobile.unity" };

    // 汎用白四角スプライトを使うGame画面のオブジェクト名
    internal static readonly HashSet<string> SlotObjectNames = new HashSet<string>
        { "WarmerSlot", "WarmerSlot (1)", "CoolerSlot", "CoolerSlot (1)", "TrashCan", "WetTowelSpawner" };

    // ---- Round5: 見た目改善 ----
    // 配色（和風の店）：藍＋生成り＋暖かい赤。タイトル・セレクト・ゲーム・リザルトで統一する。
    internal static readonly Color PaletteIndigo = new Color(0.18f, 0.29f, 0.42f, 1f);
    internal static readonly Color PaletteCream = new Color(0.97f, 0.94f, 0.85f, 1f);
    internal static readonly Color PaletteLogoCream = new Color(0.98f, 0.95f, 0.88f, 1f);
    internal static readonly Color PaletteResultCream = new Color(0.95f, 0.90f, 0.79f, 1f);
    internal static readonly Color PaletteWarmRed = new Color(0.70f, 0.25f, 0.16f, 1f);
    internal static readonly Color PaletteSlotCream = new Color(0.97f, 0.94f, 0.85f, 1f);

    internal static readonly Color PaletteButtonOutline = new Color(0.10f, 0.16f, 0.25f, 1f);
}
