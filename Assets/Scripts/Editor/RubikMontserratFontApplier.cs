#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

[InitializeOnLoad]
public static class RubikMontserratFontApplier
{
    private const string FontSourceDir = "Montserrat";
    private const string FontDestDir = "Assets/Fonts/Montserrat";
    private const string RegularTtfPath = "Assets/Fonts/Montserrat/static/Montserrat-Regular.ttf";
    private const string BoldTtfPath = "Assets/Fonts/Montserrat/static/Montserrat-Bold.ttf";
    private const string SemiBoldTtfPath = "Assets/Fonts/Montserrat/static/Montserrat-SemiBold.ttf";
    private const string MediumTtfPath = "Assets/Fonts/Montserrat/static/Montserrat-Medium.ttf";
    private const string ItalicTtfPath = "Assets/Fonts/Montserrat/static/Montserrat-Italic.ttf";
    private const string BoldItalicTtfPath = "Assets/Fonts/Montserrat/static/Montserrat-BoldItalic.ttf";

    public const string RegularSdfPath = "Assets/Fonts/Montserrat/Montserrat-Regular SDF.asset";
    public const string BoldSdfPath = "Assets/Fonts/Montserrat/Montserrat-Bold SDF.asset";
    public const string SemiBoldSdfPath = "Assets/Fonts/Montserrat/Montserrat-SemiBold SDF.asset";
    public const string MediumSdfPath = "Assets/Fonts/Montserrat/Montserrat-Medium SDF.asset";

    static RubikMontserratFontApplier()
    {
        EditorApplication.delayCall += AutoApplyMontserratOnce;
    }

    private static void AutoApplyMontserratOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        // Kiểm tra xem đã tạo font asset Montserrat chưa
        if (!File.Exists(RegularSdfPath))
        {
            ApplyMontserratToProject(false);
        }
    }

    [MenuItem("Rubik/Áp Dụng Phông Chữ Montserrat Cho Toàn Bộ Dự Án", priority = 2)]
    public static void ApplyMontserratMenu()
    {
        ApplyMontserratToProject(true);
    }

    public static void ApplyMontserratToProject(bool showDialog)
    {
        // 1. Đảm bảo copy đủ font từ thư mục gốc vào Assets/Fonts/Montserrat
        EnsureFontFilesCopied();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // 2. Tạo hoặc nạp các TMP Font Asset động (Dynamic SDF) cho Montserrat
        TMP_FontAsset regularSdf = GetOrCreateDynamicSdf(RegularTtfPath, RegularSdfPath, "Montserrat-Regular SDF");
        TMP_FontAsset boldSdf = GetOrCreateDynamicSdf(BoldTtfPath, BoldSdfPath, "Montserrat-Bold SDF");
        TMP_FontAsset semiBoldSdf = GetOrCreateDynamicSdf(SemiBoldTtfPath, SemiBoldSdfPath, "Montserrat-SemiBold SDF");
        TMP_FontAsset mediumSdf = GetOrCreateDynamicSdf(MediumTtfPath, MediumSdfPath, "Montserrat-Medium SDF");

        if (regularSdf == null)
        {
            Debug.LogError("[RubikMontserratFontApplier] Không thể tạo Font Asset cho Montserrat-Regular!");
            if (showDialog) EditorUtility.DisplayDialog("Lỗi", "Không thể tạo Font Asset cho Montserrat. Hãy kiểm tra file TTF.", "OK");
            return;
        }

        // 3. Cấu hình bảng FontWeightTable cho Regular SDF
        ConfigureFontWeights(regularSdf, regularSdf, mediumSdf, semiBoldSdf, boldSdf);
        EditorUtility.SetDirty(regularSdf);

        // 4. Cấu hình Default Font Asset trong TMP Settings
        UpdateTmpSettingsDefaultFont(regularSdf);

        // 5. Cập nhật toàn bộ TextMeshProUGUI trong Scene MainScene
        int updatedCount = UpdateSceneTextMeshProComponents(regularSdf);

        AssetDatabase.SaveAssets();

        Debug.Log($"<color=#10B981><b>[RubikMontserratFontApplier] 🎉 ĐÃ ÁP DỤNG THÀNH CÔNG PHÔNG CHỮ MONTSERRAT CHO DỰ ÁN! Đã cập nhật {updatedCount} đối tượng UI Text trong Scene.</b></color>");

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Thành công", 
                $"Đã áp dụng thành công phông chữ Montserrat cho dự án RubikSolver3D!\n\n- Font mặc định TMP: Montserrat-Regular SDF\n- Đã cập nhật: {updatedCount} TextMeshProUGUI trong Scene.", 
                "Tuyệt vời");
        }
    }

    private static void EnsureFontFilesCopied()
    {
        if (!Directory.Exists(FontDestDir))
        {
            Directory.CreateDirectory(FontDestDir);
        }

        if (Directory.Exists(FontSourceDir))
        {
            CopyDirectory(FontSourceDir, FontDestDir);
        }
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        if (!dir.Exists) return;

        DirectoryInfo[] dirs = dir.GetDirectories();
        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string targetFilePath = Path.Combine(destinationDir, file.Name);
            if (!File.Exists(targetFilePath) || file.Length != new FileInfo(targetFilePath).Length)
            {
                file.CopyTo(targetFilePath, true);
            }
        }

        foreach (DirectoryInfo subDir in dirs)
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    private static TMP_FontAsset GetOrCreateDynamicSdf(string ttfPath, string assetPath, string assetName)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (sourceFont == null)
        {
            Debug.LogWarning($"[RubikMontserratFontApplier] Chưa tìm thấy Font tại {ttfPath}. Đang import lại...");
            AssetDatabase.ImportAsset(ttfPath, ImportAssetOptions.ForceSynchronousImport);
            sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        }

        if (sourceFont == null)
        {
            Debug.LogError($"[RubikMontserratFontApplier] Không thể load Font từ: {ttfPath}");
            return null;
        }

        // Tạo Dynamic Font Asset với TextMesh Pro
        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        if (fontAsset != null)
        {
            fontAsset.name = assetName;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

            // Đảm bảo thư mục tồn tại
            string dir = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // Tạo Material SDF đi kèm
            if (fontAsset.material != null)
            {
                fontAsset.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[RubikMontserratFontApplier] Đã tạo thành công Font Asset: {assetPath}");
        }

        return fontAsset;
    }

    private static void ConfigureFontWeights(TMP_FontAsset baseAsset, TMP_FontAsset regular, TMP_FontAsset medium, TMP_FontAsset semiBold, TMP_FontAsset bold)
    {
        if (baseAsset == null || baseAsset.fontWeightTable == null || baseAsset.fontWeightTable.Length < 10) return;

        // 400 (Normal / Regular)
        baseAsset.fontWeightTable[4].regularTypeface = regular;

        // 500 (Medium)
        baseAsset.fontWeightTable[5].regularTypeface = medium != null ? medium : regular;

        // 600 (SemiBold)
        baseAsset.fontWeightTable[6].regularTypeface = semiBold != null ? semiBold : bold;

        // 700 (Bold)
        baseAsset.fontWeightTable[7].regularTypeface = bold != null ? bold : regular;

        // 800 & 900 (ExtraBold / Black)
        baseAsset.fontWeightTable[8].regularTypeface = bold != null ? bold : regular;
        baseAsset.fontWeightTable[9].regularTypeface = bold != null ? bold : regular;
    }

    private static void UpdateTmpSettingsDefaultFont(TMP_FontAsset newDefaultFont)
    {
        if (newDefaultFont == null) return;

        TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings != null)
        {
            SerializedObject so = new SerializedObject(settings);
            SerializedProperty defaultFontProp = so.FindProperty("m_defaultFontAsset");
            if (defaultFontProp != null)
            {
                defaultFontProp.objectReferenceValue = newDefaultFont;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
                Debug.Log("[RubikMontserratFontApplier] Đã cập nhật Default Font Asset trong TMP Settings sang Montserrat-Regular SDF!");
            }
        }
    }

    private static int UpdateSceneTextMeshProComponents(TMP_FontAsset targetFont)
    {
        int count = 0;
        Scene activeScene = SceneManager.GetActiveScene();
        bool wasSceneOpened = false;

        const string mainScenePath = "Assets/Scenes/MainScene.unity";
        if (activeScene.path != mainScenePath && File.Exists(mainScenePath))
        {
            activeScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
            wasSceneOpened = true;
        }

        TMP_Text[] allTexts = Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        foreach (TMP_Text t in allTexts)
        {
            if (t == null) continue;
            Undo.RecordObject(t, "Apply Montserrat Font");
            t.font = targetFont;
            EditorUtility.SetDirty(t);
            count++;
        }

        if (count > 0 || wasSceneOpened)
        {
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveOpenScenes();
        }

        return count;
    }
}
#endif
