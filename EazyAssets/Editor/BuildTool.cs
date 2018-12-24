using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

/// <summary>
/// 打包工具
/// </summary>
public class BuildTool
{
    static readonly string assetPath = @".\Assets\AssetBundles\Raw\";
    static readonly string rawConfigPath = @"Assets\Resources\RawConfig\";

    static string AndroidBuildPath = "{0}/AssetBundles/Android";
    static string IOSBuildPath = "{0}/AssetBundles/IOS";
    static string WINBuildPath = "{0}/AssetBundles/Windows";
    /// <summary>
    /// 打包Asset Bundle
    /// </summary>
    [MenuItem("工具/打包AssetBundle(自动设置)", false, 300)]
    public static void BuildAssetBundle()
    {
        //设置包名
        BuildAssetNameSetting(assetPath);
#if UNITY_ANDROID

        #region Android

        AndroidBuildPath = string.Format(AndroidBuildPath, Application.dataPath);

        if (Directory.Exists(AndroidBuildPath))
        {//清空上次打包数据
            string[] files = Directory.GetFiles(AndroidBuildPath);
            foreach (var f in files)
                File.Delete(f);
        }
        else
            Directory.CreateDirectory(AndroidBuildPath);

        //打包
        BuildPipeline.BuildAssetBundles(AndroidBuildPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);

        //计算md5
        CalculateBundleIdentify(AndroidBuildPath);

        //拷贝文件
        CopyAssetBundle2StreamingAsset(AndroidBuildPath);

        #endregion

#elif UNITY_IOS

        #region IOS

        IOSBuildPath = string.Format(IOSBuildPath, Application.dataPath);

        if (Directory.Exists(IOSBuildPath))
        {//清空上次打包数据
            string[] files = Directory.GetFiles(IOSBuildPath);
            foreach (var f in files)
                File.Delete(f);
        }
        else
            Directory.CreateDirectory(IOSBuildPath);

        //打包
        BuildPipeline.BuildAssetBundles(IOSBuildPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.iOS);

        //计算md5
        CalculateBundleIdentify(IOSBuildPath);

        //拷贝文件
        CopyAssetBundle2StreamingAsset(IOSBuildPath);

        #endregion

#elif UNITY_STANDALONE

        #region Windows

        WINBuildPath = string.Format(WINBuildPath, Application.dataPath);

        if (Directory.Exists(WINBuildPath))
        {//清空上次打包数据
            string[] files = Directory.GetFiles(WINBuildPath);
            foreach (var f in files)
                File.Delete(f);
        }
        else
            Directory.CreateDirectory(WINBuildPath);

        //打包
        BuildPipeline.BuildAssetBundles(WINBuildPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);

        //计算md5
        CalculateBundleIdentify(WINBuildPath);

        //拷贝文件
        CopyAssetBundle2StreamingAsset(WINBuildPath);

        #endregion

#endif

        AssetDatabase.Refresh();
    }

    [MenuItem("工具/按现有配置打包AB", false, 310)]
    public static void OnlyBuildAssetBundle()
    {
#if UNITY_ANDROID
        AndroidBuildPath = string.Format(AndroidBuildPath, Application.dataPath);
        BuildPipeline.BuildAssetBundles(AndroidBuildPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.Android);
#elif UNITY_IOS

        AndroidBuildPath = string.Format(IOSBuildPath, Application.dataPath);
        BuildPipeline.BuildAssetBundles(IOSBuildPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.iOS);

#elif UNITY_STANDALONE
        AndroidBuildPath = string.Format(WINBuildPath, Application.dataPath);
        BuildPipeline.BuildAssetBundles(WINBuildPath, BuildAssetBundleOptions.ChunkBasedCompression, BuildTarget.StandaloneWindows);
#endif
        AssetDatabase.Refresh();
    }

    [MenuItem("工具/一键配置资产(自动设置)",false,400)]
    public static void ConfigAsset()
    {
        //打包Asset Bundle
        BuildAssetBundle();

        //生成资产清单
        AssetsMenuGenerator.GenAssetsMenu();
    }

    [MenuItem("工具/打包项目", false, 500)]
    public static void ShowBuildWindow()
    {
        BuildProjectWindow.ShowWindow();
    }

    public static void BuildPackage()
    {
        //先配置资产，然后再打包
        ConfigAsset();

#if UNITY_ANDROID
        BuildAssetNameSetting(assetPath);
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();

        var buildScenes = EditorBuildSettings.scenes;
        List<string> scenePath = new List<string>();
        for (int i = 0; i < buildScenes.Length; i++)
        {
            if (buildScenes[i].enabled)
            {
                scenePath.Add(buildScenes[i].path);
                DebugConsole.Log("build scene: " + buildScenes[i].path);
            }
        }

        buildPlayerOptions.scenes = scenePath.ToArray();
        buildPlayerOptions.locationPathName = string.Format(@"..\Output\Android_{0}.apk", System.DateTime.Now.ToString("MMddHHmmss"));
        buildPlayerOptions.target = BuildTarget.Android;
        buildPlayerOptions.options = BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
        }

        if (summary.result == BuildResult.Failed)
        {
            Debug.Log("Build failed");
        }

        System.Diagnostics.Process.Start(@"..\Output");
#endif
    }

    /// <summary>
    /// 设置 Asset Name
    /// </summary>
    static void BuildAssetNameSetting(string assetRootPath)
    {
        if (!Directory.Exists(assetRootPath))
            Directory.CreateDirectory(assetRootPath);

        string[] dirs = Directory.GetDirectories(assetRootPath);

        //Debug.LogError(assetRootPath);
        foreach (string dir in dirs)
        {
            BuildAssetNameSetting(dir);

            string nameDir = dir.Replace(assetPath, "");
            nameDir = nameDir.Replace("\\", "_");
            //string[] spdir = dir.Split('\\');
            string dirName = nameDir;//spdir[spdir.Length - 1];
            string[] files = Directory.GetFiles(dir);
            Debug.Log("bundle name = " + dirName);
            foreach (string file in files)
            {
                if (!file.Contains(".meta"))
                {
                    string refile = file.Replace(@".\", "");
                    AssetImporter ai = AssetImporter.GetAtPath(refile);
                    //DebugConsole.LogError(refile);
                    //DebugConsole.Log(ai);
                    if (ai != null)
                    {
                        ai.SetAssetBundleNameAndVariant(dirName, "");
                        DebugConsole.Log("set name = " + dirName);
                        ai.SaveAndReimport();
                    }
                }
            }
        }
    }

    /// <summary>
    /// 将 AssetBundle 复制到 StreamingAsset
    /// </summary>
    static void CopyAssetBundle2StreamingAsset(string assetbudleBuildPath)
    {
        string outputPath = @".\Assets\StreamingAssets\";
        if (Directory.Exists(outputPath))
        {//清空上次打包数据
            string[] fs = Directory.GetFiles(outputPath);
            foreach (var f in fs)
                File.Delete(f);
        }
        else
            Directory.CreateDirectory(outputPath);

        //生成原始配置文件路径
        if (!Directory.Exists(rawConfigPath))
            Directory.CreateDirectory(rawConfigPath);

        //生成原始资源清单
        StreamWriter sw = File.CreateText(PUBLIC_PATH_DEFINE.RawBundleManifestGenPath);

        string[] files = Directory.GetFiles(assetbudleBuildPath);
        foreach (var file in files)
        {
            if (!file.Contains(".meta")
                && !file.Contains(".manifest"))
            {
                string[] sp = file.Split('\\');
                string name = sp[sp.Length - 1];
                File.Copy(file, outputPath + name);
                sw.WriteLine(name);
                //Debug.Log(sp[sp.Length - 1]);
            }
        }

        sw.Flush();
        sw.Dispose();
    }

    [MenuItem("工具/设置版本号", false, 100)]
    public static void SetVersion()
    {
        Selection.activeObject = GetVersionObj();
    }

    /// <summary>
    /// 获取版本号文件
    /// </summary>
    /// <returns></returns>
    public static VersionObj GetVersionObj()
    {
        VersionObj obj = AssetDatabase.LoadAssetAtPath<VersionObj>(rawConfigPath + "Version.asset");
        if (obj == null)
        {
            obj = ScriptableObject.CreateInstance<VersionObj>();
            if (!Directory.Exists(rawConfigPath))
                Directory.CreateDirectory(rawConfigPath);

            AssetDatabase.CreateAsset(obj, rawConfigPath + "Version.asset");
            AssetDatabase.Refresh();
        }

        return obj;
    }

    //计算Bundle md5值
    static void CalculateBundleIdentify(string assetbudleBuildPath)
    {
        long totalFileLength = 0;
        StreamWriter sw = new StreamWriter(File.Open(assetbudleBuildPath + "/" + PUBLIC_PATH_DEFINE.BundlesIdentityListName, FileMode.CreateNew));

        string version = "-1";
        //设置资源版本号
        VersionObj obj = AssetDatabase.LoadAssetAtPath<VersionObj>("Assets/Resources/RawConfig/Version.asset");
        if (obj != null && obj.Open_Version)
        {
            version = obj.Asset_Version_Number;
        }

        //创建并保存成Xml
        XmlDocument xml = new XmlDocument();
        XmlElement root = xml.CreateElement("BundleIdentity");
        root.SetAttribute("version", version);

        string[] files = Directory.GetFiles(assetbudleBuildPath);

        foreach (var file in files)
        {
            if (!file.Contains(".meta")
                && !file.Contains(".manifest")
                && !file.Contains("BundlesIdentityList")/*不计算自身*/)
            {
                string md5 = "";
                FileStream fs = File.Open(file, FileMode.Open);
                long fileLength = fs.Length;
                totalFileLength += fileLength;       //计算文件总长度

                using (fs)
                {
                    md5 = CalculateFileMD5(fs);          //计算md5值
                }

                string fileName = Path.GetFileName(file);
                //sw.WriteLine(fileName + ":" + md5);
                XmlElement id = xml.CreateElement(fileName);
                id.InnerText = md5;
                id.SetAttribute("Length", fileLength.ToString());//记录单个文件长度
                DebugConsole.Log("已计算AssetBundle（" + fileName + "）长度为" + ((float)fileLength / 1024) + "KB,\r\nMD5 = " + md5);
                root.AppendChild(id);
            }
        }

        root.SetAttribute("TotalLength", totalFileLength.ToString());
        DebugConsole.Log("已计算AssetBundle资源总长度为" + ((float)totalFileLength / 1024) + "KB");
        xml.AppendChild(root);
        xml.Save(sw);
        sw.Flush();
        sw.Dispose();
    }

    //计算文件Md5值
    static string CalculateFileMD5(FileStream fs)
    {
        MD5 md5 = MD5.Create();
        byte[] bs = md5.ComputeHash(fs);
        StringBuilder sb = new StringBuilder();
        foreach (byte b in bs)
        {
            sb.Append(b.ToString("x2"));
        }
        //DebugConsole.Log("file md5 = " + sb.ToString() + "  lenght = " + sb.Length);
        return sb.ToString();
    }

    [MenuItem("工具/清除编辑器版本号", false, 110)]
    public static void ClearVersion()
    {
        PlayerPrefs.DeleteKey("Main_Version_Number");
        PlayerPrefs.DeleteKey("Asset_Version_Number");
    }

    [MenuItem("工具/测试", false, 1000)]
    public static void Test()
    {
        //BuildProjectWindow.ShowWindow();
        BuildAssetNameSetting(assetPath);
    }
}



#region 打包工程设置窗口

/// <summary>
/// 打包工程设置窗口
/// </summary>
public class BuildProjectWindow : EditorWindow
{
    private static BuildProjectWindow instance;

    EditorBuildSettingsScene[] scenes;

    public void Init()
    {
        string[] assets = AssetDatabase.GetAllAssetPaths();
        List<string> scenes = new List<string>();
        foreach (var f in assets)
        {
            if (f.Contains(".unity")
                && !f.Contains(".meta")
                && !f.Contains("Packages"))
            {
                scenes.Add(f);
                //Debug.LogError(f);
            }
        }

        List<EditorBuildSettingsScene> curScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        List<EditorBuildSettingsScene> addScene = new List<EditorBuildSettingsScene>();

        EditorBuildSettingsScene[] buildScene = new EditorBuildSettingsScene[scenes.Count];
        for (int i = 0; i < scenes.Count; i++)
        {
            EditorBuildSettingsScene set = GetSceneSet(scenes[i], curScenes.GetEnumerator());

            if (set == null)
            {
                set = new EditorBuildSettingsScene(scenes[i], false);
                addScene.Add(set);
            }
        }

        curScenes.AddRange(addScene);

        buildScene = curScenes.ToArray();

        try
        {
            EditorBuildSettings.scenes = buildScene;// EditorBuildSettings.scenes;
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.Message);
        }

        this.scenes = buildScene;
    }

    private void OnGUI()
    {
#region  场景设置

        GUILayout.Space(10);
        GUILayout.BeginVertical();
        GUILayout.Label("设置打包场景：", "capsulebutton");
        GUILayout.Space(10);

        int enableIndex = 0;
        for (int i = 0; i < scenes.Length; i++)
        {
            int curIndex = i; //保存当前索引

            GUILayout.BeginHorizontal();
            scenes[i].enabled = GUILayout.Toggle(scenes[i].enabled, "");
            GUILayout.Label(scenes[i].path, (GUIStyle)"sv_label_2");
            GUILayout.FlexibleSpace();
            if (scenes[i].enabled)
            {
                if (GUILayout.Button("", (GUIStyle)"Grad Up Swatch"))
                {
                    SwitchBuildSceneOrder(curIndex, curIndex - 1);
                }
                //scene id
                GUILayout.Label(enableIndex.ToString());
                enableIndex++;

                if (GUILayout.Button("", (GUIStyle)"Grad Down Swatch"))
                {
                    SwitchBuildSceneOrder(curIndex, curIndex + 1);
                }
            }
            GUILayout.EndHorizontal();
        }

        //GUILayout.Space(10);
        //GUILayout.Label("", "capsulebutton");
        GUILayout.EndVertical();

#endregion

#region 版本号设置

        VersionObj version = BuildTool.GetVersionObj();
        GUILayout.Space(10);
        GUILayout.BeginVertical();
        GUILayout.Label("版本号设置：","capsulebutton");
        GUILayout.Space(10);

        version.Open_Version = GUILayout.Toggle(version.Open_Version, "开启版本号控制：");

        if (version.Open_Version)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("主版本号", (GUIStyle)"ChannelStripAttenuationBar");
            GUILayout.FlexibleSpace();
            version.Main_Version_Number = GUILayout.TextField(version.Main_Version_Number);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("资源版本号", (GUIStyle)"ChannelStripAttenuationBar");
            GUILayout.FlexibleSpace();
            version.Asset_Version_Number = GUILayout.TextField(version.Asset_Version_Number);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        GUILayout.Label("", "capsulebutton");
        GUILayout.EndVertical();
#endregion

        GUILayout.Space(10);

        if (GUILayout.Button("打包项目"))
        {
            SaveBuildScenes();
            BuildTool.BuildPackage();
            this.Close();
        }
        if (GUILayout.Button("关闭窗口"))
        {
            this.Close();
        }
    }

    void SaveBuildScenes()
    {
        EditorBuildSettings.scenes = this.scenes;
    }

    public static void ShowWindow()
    {
        if (instance != null)
            instance.Close();

        instance = new BuildProjectWindow();
        instance.Init();
        instance.Show();
    }

    EditorBuildSettingsScene GetSceneSet(string scenePath, IEnumerator<EditorBuildSettingsScene> e)
    {
        while (e.MoveNext())
        {
            if (e.Current.path == scenePath)
                return e.Current;
        }
        return null;
    }

    //交换打包场景的顺序
    void SwitchBuildSceneOrder(int index_1, int index_2)
    {
        SaveBuildScenes();      //先存一下设置

        var s_scenes = EditorBuildSettings.scenes;
        if (index_1 > -1 && index_1 < s_scenes.Length
            && index_2 > -1 && index_2 < s_scenes.Length)
        {
            var temp = s_scenes[index_1];
            s_scenes[index_1] = s_scenes[index_2];
            s_scenes[index_2] = temp;
        }

        EditorBuildSettings.scenes = s_scenes;
        RefreshScenes();
    }

    void RefreshScenes()
    {
        this.scenes = EditorBuildSettings.scenes;
    }

    private void OnDestroy()
    {
        SaveBuildScenes();
    }
}

#endregion
