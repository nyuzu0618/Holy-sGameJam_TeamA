using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using OutGame;

/// <summary>
/// アウトゲームを自動で組み立てるエディタ拡張。
/// ・title.unity のUI
/// ・Prefabs/ResultCanvas.prefab（本編シーンに置くリザルト）
/// ・Prefabs/SceneFader.prefab
/// ・MainGameTest.unity（動作確認用。スペースでリザルト表示）
///
/// ・コンパイル後に自動で行うのは「ビルド一覧への登録」だけ（シーンやプレハブは作らない）
/// ・組み立て直し：メニュー「Tools > Holy > アウトゲームを組み立てる」
///   （既にあるオブジェクトは名前で探して再利用するので、何度実行しても重複しない）
/// ・配布用：メニュー「Tools > Holy > UnityPackageを書き出す」
/// </summary>
[InitializeOnLoad]
public static class OutGameSceneBuilder
{
    private const string HolyFolder = "Assets/Holy";
    private const string PrefabFolder = "Assets/Holy/Prefabs";
    private const string TitleScenePath = "Assets/Holy/title.unity";
    private const string TestScenePath = "Assets/Holy/MainGameTest.unity";
    private const string MainGameSceneName = "MainGame"; // 本編シーンの名前（場所はどこでもよい）
    private const string OldResultScenePath = "Assets/Holy/result.unity";
    private const string ResultPrefabPath = "Assets/Holy/Prefabs/ResultCanvas.prefab";
    private const string FaderPrefabPath = "Assets/Holy/Prefabs/SceneFader.prefab";
    private const string TitleImagePath = "Assets/Holy/Image/タイトル統合.psd";
    private const string TestSceneName = "MainGameTest";
    private const string PackageFileName = "OutGame_Holy.unitypackage";

    /// <summary>UnityPackage に入れないもの（古いファイル・本編担当のシーン）</summary>
    private static readonly string[] PackageExcludes =
    {
        "Assets/Holy/result.unity",
        "Assets/Holy/MainGame.unity",
        "Assets/Holy/Scripts/ResultData.cs",
    };

    private static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);
    private static readonly Color TextColor = new Color32(0x0A, 0x5A, 0xA0, 0xFF); // タイトルロゴに合わせた青

    static OutGameSceneBuilder()
    {
        // 自動で行うのはビルド一覧への登録だけ
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            EnsureScenesInBuild();
        };
    }

    [MenuItem("Tools/Holy/アウトゲームを組み立てる")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder(HolyFolder, "Prefabs");

        var titleSprite = PrepareTitleSprite();

        BuildTitleScene(titleSprite);
        BuildFaderPrefab();
        BuildResultPrefab();
        BuildTestScene();
        EnsureScenesInBuild();

        EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        Debug.Log("[OutGameSceneBuilder] タイトル／リザルト(Prefab)／MainGameTest を組み立てました。");
    }

    // ------------------------------------------------------------
    // タイトル
    // ------------------------------------------------------------
    private static void BuildTitleScene(Sprite titleSprite)
    {
        var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

        var canvas = GetOrCreateRoot(scene, "TitleCanvas");
        SetupCanvas(canvas, 0);

        var bg = GetOrCreateChild(canvas.transform, "Background");
        var bgImage = GetOrAdd<Image>(bg);
        bgImage.sprite = titleSprite;
        bgImage.color = Color.white;
        bgImage.raycastTarget = false;
        MakeFullScreen(bg, titleSprite);
        bg.transform.SetSiblingIndex(0);

        var start = GetOrCreateChild(canvas.transform, "StartText");
        var startText = SetupText(start, "START", 110, TextAlignmentOptions.Center);
        startText.fontStyle = FontStyles.Bold;
        SetRect(start, new Vector2(0.5f, 0f), new Vector2(0, 170), new Vector2(800, 180));
        GetOrAdd<PulseScale>(start);

        GetOrAdd<SceneFader>(GetOrCreateRoot(scene, "SceneFader"));

        var titleManager = GetOrAdd<TitleManager>(GetOrCreateRoot(scene, "TitleManager"));
        var so = new SerializedObject(titleManager);
        var sceneProp = so.FindProperty("gameSceneName");
        // 空や古い仮の名前なら MainGame に変更（自分で入力した名前は変えない）
        if (string.IsNullOrEmpty(sceneProp.stringValue) || sceneProp.stringValue == "GameScene")
            sceneProp.stringValue = MainGameSceneName;
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ------------------------------------------------------------
    // SceneFader プレハブ
    // ------------------------------------------------------------
    private static void BuildFaderPrefab()
    {
        bool exists = File.Exists(FaderPrefabPath);
        var root = exists ? PrefabUtility.LoadPrefabContents(FaderPrefabPath) : new GameObject("SceneFader");
        GetOrAdd<SceneFader>(root);
        PrefabUtility.SaveAsPrefabAsset(root, FaderPrefabPath);
        if (exists) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root);
    }

    // ------------------------------------------------------------
    // リザルト プレハブ
    // ------------------------------------------------------------
    private static void BuildResultPrefab()
    {
        bool exists = File.Exists(ResultPrefabPath);
        var root = exists ? PrefabUtility.LoadPrefabContents(ResultPrefabPath) : new GameObject("ResultCanvas", typeof(RectTransform));

        SetupCanvas(root, 100); // ゲームのUIより手前に出す
        var group = GetOrAdd<CanvasGroup>(root);
        group.alpha = 1f; // エディタでは見えるように（実行時は ResultManager が隠す）

        var uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // 背景（画像を指定しなければ半透明の黒）
        var bg = GetOrCreateChild(root.transform, "Background");
        var bgImage = GetOrAdd<Image>(bg);
        bgImage.raycastTarget = true; // 後ろのゲーム画面をクリックできないようにする
        MakeFullScreen(bg, null);
        bg.transform.SetSiblingIndex(0);

        // パネル
        var panel = GetOrCreateChild(root.transform, "Panel");
        var panelImage = GetOrAdd<Image>(panel);
        panelImage.sprite = uiSprite;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = new Color(1f, 1f, 1f, 0.9f);
        panelImage.raycastTarget = false;
        SetRect(panel, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(1200, 860));
        panel.transform.SetSiblingIndex(1);

        var header = GetOrCreateChild(panel.transform, "ResultHeader");
        var headerText = SetupText(header, "RESULT", 120, TextAlignmentOptions.Center);
        headerText.fontStyle = FontStyles.Bold;
        SetRect(header, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1000, 160));

        var item1 = BuildResultItem(panel.transform, "Value1", new Vector2(0, 90));
        var item2 = BuildResultItem(panel.transform, "Value2", new Vector2(0, -110));

        var titleButton = BuildButton(panel.transform, "TitleButton", "TITLE", new Vector2(-260, -310), uiSprite);
        var retryButton = BuildButton(panel.transform, "RetryButton", "RETRY", new Vector2(260, -310), uiSprite);

        var manager = GetOrAdd<ResultManager>(root);
        var so = new SerializedObject(manager);
        so.FindProperty("backgroundImage").objectReferenceValue = bgImage;
        SetItem(so.FindProperty("value1Item"), item1);
        SetItem(so.FindProperty("value2Item"), item2);
        so.FindProperty("titleButton").objectReferenceValue = titleButton;
        so.FindProperty("retryButton").objectReferenceValue = retryButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, ResultPrefabPath);
        if (exists) PrefabUtility.UnloadPrefabContents(root); else Object.DestroyImmediate(root);
    }

    private static (Image icon, TMP_Text count) BuildResultItem(Transform parent, string name, Vector2 pos)
    {
        var root = GetOrCreateChild(parent, name);
        SetRect(root, new Vector2(0.5f, 0.5f), pos, new Vector2(700, 170));

        var icon = GetOrCreateChild(root.transform, "Icon");
        var iconImage = GetOrAdd<Image>(icon);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        SetRect(icon, new Vector2(0.5f, 0.5f), new Vector2(-180, 0), new Vector2(160, 160));

        var count = GetOrCreateChild(root.transform, "Count");
        var countText = SetupText(count, "×99", 120, TextAlignmentOptions.Left);
        countText.fontStyle = FontStyles.Bold;
        SetRect(count, new Vector2(0.5f, 0.5f), new Vector2(140, 0), new Vector2(420, 170));

        return (iconImage, countText);
    }

    private static void SetItem(SerializedProperty element, (Image icon, TMP_Text count) item)
    {
        element.FindPropertyRelative("iconImage").objectReferenceValue = item.icon;
        element.FindPropertyRelative("countText").objectReferenceValue = item.count;
        var prefixProp = element.FindPropertyRelative("prefix");
        if (string.IsNullOrEmpty(prefixProp.stringValue)) prefixProp.stringValue = "×";
    }

    private static Button BuildButton(Transform parent, string name, string label, Vector2 pos, Sprite sprite)
    {
        var go = GetOrCreateChild(parent, name);
        SetRect(go, new Vector2(0.5f, 0.5f), pos, new Vector2(420, 130));

        var image = GetOrAdd<Image>(go);
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = TextColor;

        var button = GetOrAdd<Button>(go);
        button.targetGraphic = image;

        var text = GetOrCreateChild(go.transform, "Label");
        var tmp = SetupText(text, label, 70, TextAlignmentOptions.Center);
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        var rt = (RectTransform)text.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return button;
    }

    // ------------------------------------------------------------
    // MainGameTest シーン（動作確認用）
    // ------------------------------------------------------------
    private static void BuildTestScene()
    {
        Scene scene;
        if (File.Exists(TestScenePath))
        {
            scene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, TestScenePath);
        }

        // 説明表示
        var canvas = GetOrCreateRoot(scene, "TestCanvas");
        SetupCanvas(canvas, 0);
        var info = GetOrCreateChild(canvas.transform, "InfoText");
        SetupText(info, "MAIN GAME TEST\n<size=60%>Press SPACE to show result</size>", 100, TextAlignmentOptions.Center);
        SetRect(info, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 400));

        // プレハブを配置
        var faderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FaderPrefabPath);
        var resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath);
        if (FindRoot(scene, "SceneFader") == null) PrefabUtility.InstantiatePrefab(faderPrefab, scene);
        var resultGo = FindRoot(scene, "ResultCanvas");
        if (resultGo == null) resultGo = (GameObject)PrefabUtility.InstantiatePrefab(resultPrefab, scene);

        // スペースでリザルトを出すテスト用スクリプト
        var tester = GetOrAdd<MainGameTest>(GetOrCreateRoot(scene, "MainGameTest"));
        var so = new SerializedObject(tester);
        so.FindProperty("result").objectReferenceValue = resultGo.GetComponent<ResultManager>();
        so.ApplyModifiedPropertiesWithoutUndo();

        EnsureEventSystem(scene);
        EditorSceneManager.SaveScene(scene);
    }

    // ------------------------------------------------------------
    // ビルド一覧
    // ------------------------------------------------------------
    /// <summary>
    /// title（先頭）・MainGame（プロジェクト内のどこにあってもよい）・MainGameTest をビルド一覧に登録し、使わなくなった result.unity を一覧から外す。
    /// 一覧に無いシーンには SceneManager.LoadScene で移動できないため。
    /// </summary>
    [MenuItem("Tools/Holy/シーンをビルド一覧に登録")]
    public static void EnsureScenesInBuild()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        var before = string.Join("|", scenes.ConvertAll(s => s.path + s.enabled));

        scenes.RemoveAll(s => s.path == OldResultScenePath);

        if (File.Exists(TitleScenePath))
        {
            scenes.RemoveAll(s => s.path == TitleScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(TitleScenePath, true));
        }
        var targets = new List<string>(FindScenesByName(MainGameSceneName));
        if (targets.Count > 1)
            Debug.LogWarning($"[OutGameSceneBuilder] 「{MainGameSceneName}」という名前のシーンが複数あります。先に一覧に入っている方が読み込まれます: " + string.Join(", ", targets));
        targets.Add(TestScenePath);

        foreach (var path in targets)
        {
            if (!File.Exists(path) || scenes.Exists(s => s.path == path && s.enabled)) continue;
            scenes.RemoveAll(s => s.path == path);
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        var after = string.Join("|", scenes.ConvertAll(s => s.path + s.enabled));
        if (before != after)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets(); // ProjectSettings にすぐ書き込む
            Debug.Log("[OutGameSceneBuilder] ビルド一覧を更新しました: " + string.Join(", ", scenes.ConvertAll(s => s.path)));
        }
    }

    private static IEnumerable<string> FindScenesByName(string sceneName)
    {
        foreach (var guid in AssetDatabase.FindAssets(sceneName + " t:Scene"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == sceneName) yield return path;
        }
    }

    // ------------------------------------------------------------
    // UnityPackage 書き出し
    // ------------------------------------------------------------
    [MenuItem("Tools/Holy/UnityPackageを書き出す")]
    public static void ExportPackage()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        AssetDatabase.SaveAssets();

        var paths = new List<string>();
        foreach (var guid in AssetDatabase.FindAssets("", new[] { HolyFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.Array.IndexOf(PackageExcludes, path) >= 0) continue;
            if (!paths.Contains(path)) paths.Add(path);
        }

        var outPath = EditorUtility.SaveFilePanel("UnityPackageを書き出す", Path.GetDirectoryName(Application.dataPath), PackageFileName, "unitypackage");
        if (string.IsNullOrEmpty(outPath)) return;

        // 依存ファイル（TMPフォントなど）は相手のプロジェクトにもあるので含めない
        AssetDatabase.ExportPackage(paths.ToArray(), outPath, ExportPackageOptions.Default);
        EditorUtility.RevealInFinder(outPath);
        Debug.Log($"[OutGameSceneBuilder] {paths.Count} 個のファイルを書き出しました: {outPath}");
    }

    // ------------------------------------------------------------
    // 共通処理
    // ------------------------------------------------------------
    private static Sprite PrepareTitleSprite()
    {
        var importer = AssetImporter.GetAtPath(TitleImagePath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.maxTextureSize < 4096) { importer.maxTextureSize = 4096; changed = true; }
            if (changed) importer.SaveAndReimport();
        }
        else
        {
            Debug.LogWarning($"[OutGameSceneBuilder] {TitleImagePath} が見つかりません。");
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(TitleImagePath);
    }

    private static void SetupCanvas(GameObject go, int sortingOrder)
    {
        go.layer = LayerMask.NameToLayer("UI");
        if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();

        var canvas = GetOrAdd<Canvas>(go);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = GetOrAdd<CanvasScaler>(go);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = ReferenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GetOrAdd<GraphicRaycaster>(go);
    }

    /// <summary>画面いっぱいに表示（比率を保ったまま画面を覆う）</summary>
    private static void MakeFullScreen(GameObject go, Sprite sprite)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = ReferenceResolution;

        var fitter = GetOrAdd<AspectRatioFitter>(go);
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = sprite != null ? sprite.rect.width / sprite.rect.height : 16f / 9f;
    }

    private static TextMeshProUGUI SetupText(GameObject go, string text, float size, TextAlignmentOptions align)
    {
        var tmp = GetOrAdd<TextMeshProUGUI>(go);
        if (tmp.font == null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = TextColor;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private static void SetRect(GameObject go, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void EnsureEventSystem(Scene scene)
    {
        EventSystem es = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            es = root.GetComponentInChildren<EventSystem>(true);
            if (es != null) break;
        }
        var go = es != null ? es.gameObject : GetOrCreateRoot(scene, "EventSystem");
        GetOrAdd<EventSystem>(go);

        var legacy = go.GetComponent<StandaloneInputModule>();
        if (legacy != null) Object.DestroyImmediate(legacy);
        GetOrAdd<InputSystemUIInputModule>(go);
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    private static GameObject GetOrCreateRoot(Scene scene, string name)
    {
        var found = FindRoot(scene, name);
        if (found != null) return found;

        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static GameObject GetOrCreateChild(Transform parent, string name)
    {
        var found = parent.Find(name);
        if (found != null) return found.gameObject;

        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}
