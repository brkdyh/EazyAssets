using System;
using UnityEngine;

#region 公共路径定义

public static class PUBLIC_PATH_DEFINE
{
    #region 生成相关路径，只在Editor下使用

#if UNITY_EDITOR

    /// <summary>
    /// 资源加载配置清单原始生成路径
    /// </summary>
    public readonly static string RawAssetLoadConfigListGenPath = "Assets/Resources/RawConfig/AssetLoadConfigList.xml";

    /// <summary>
    /// 原始Asset Bundle清单生成路径
    /// </summary>
    public readonly static string RawBundleManifestGenPath = "Assets/Resources/RawConfig/RawBundleManifest.txt";

    public static readonly string RawAssetBundlesGenPath_Android = "Assets/AssetBundles/Android";

    public static readonly string RawAssetBundlesGenPath_IOS = "Assets/AssetBundles/IOS";

    public static readonly string RawAssetBundlesGenPath_Windows = "Assets/AssetBundles/Windows";

#endif
    #endregion

    #region 原始资源路径，即随包资源路径

    /// <summary>
    /// 资源加载配置清单加载原始路径
    /// </summary>
    public readonly static string RawAssetLoadConfigListPath = "RawConfig/AssetLoadConfigList";

    /// <summary>
    /// 资源更新配置清单加载原始路径
    /// </summary>
    public readonly static string RawAssetUpdateConfigListPath = "RawConfig/AssetUpdateConfigList";

    /// <summary>
    /// 原始 Asset Bundles 清单 
    /// </summary>
    public readonly static string RawBundleManifest = "RawConfig/RawBundleManifest";

    #endregion

    #region 资源正式使用时的加载路径,在IO路径下，可更新

    /// <summary>
    /// 资源加载配置清单加载读写路径
    /// </summary>
    public static string AssetLoadConfigListPath = "{0}/AssetLoadConfigList.xml";

    /// <summary>
    /// Assets Bundle Manifest加载路径
    /// </summary>
    public static string AssetBundlesManifestPath = "{0}/AssetBundles/{1}";

    /// <summary>
    /// Assets Bundle 资源根目录
    /// </summary>
    public static string AssetBundlesRootPath = "{0}/AssetBundles/{1}";

    /// <summary>
    /// Streaming Asset 路径
    /// </summary>
    public static string StreamingAssetsPath = "{0}/";

    /// <summary>
    /// 资源包身份列表文件名
    /// </summary>
    public readonly static string BundlesIdentityListName = "BundlesIdentityList.xml";

    #endregion

    #region 远程URL

    public static readonly string AssetsServerUrl = "http://brkdyh-tset.oss-cn-beijing.aliyuncs.com/CardGameUpdateAssets/";

    #endregion

    /// <summary>
    /// 用于初始化运行时路径，必须最先调用
    /// </summary>
    public static void Init()
    {
        string formatPath = Application.persistentDataPath;
        AssetLoadConfigListPath = string.Format(AssetLoadConfigListPath, formatPath);

#if UNITY_EDITOR
        formatPath = Application.dataPath;
#else
        formatPath = Application.persistentDataPath;
#endif

#if UNITY_ANDROID
        AssetBundlesManifestPath = string.Format(AssetBundlesManifestPath, formatPath, "Android/Android");
        AssetBundlesRootPath = string.Format(AssetBundlesRootPath, formatPath, "Android/");
        StreamingAssetsPath = string.Format(StreamingAssetsPath, Application.streamingAssetsPath);//string.Format(StreamingAssetsPath, formatPath);
#elif UNITY_IOS
        AssetBundlesManifestPath = string.Format(AssetBundlesManifestPath, formatPath, "IOS/IOS");
        //AssetBundlesRootPath = string.Format(AssetBundlesRootPath, formatPath);
#elif UNITY_STANDALONE
        AssetBundlesManifestPath = string.Format(AssetBundlesManifestPath, formatPath, "Windows/Windows");
        //AssetBundlesRootPath = string.Format(AssetBundlesRootPath, formatPath);
#endif
    }
}

#endregion

