using System;
using UnityEngine;

/// <summary>
/// 处理版本号
/// </summary>
public class Version
{
    private static bool open;
    /// <summary>
    /// 主版本号-用于包体更新判断
    /// </summary>
    private static string Main_Version_Number = "";

    /// <summary>
    /// 资源版本号
    /// </summary>
    private static string Asset_Version_Number = "";

    public static void Init()
    {
        VersionObj version = Resources.Load<VersionObj>("RawConfig/Version");

        if (version == null)
        {
            DebugConsole.LogError("Can't Load Version Object!");
            open = false;
            return;
        }

        open = version.Open_Version;

        if (open)
        {
            Main_Version_Number = version.Main_Version_Number;
            DebugConsole.Log("程序主版本号为:" + Main_Version_Number, DebugConsole.Color.blue);
            DebugConsole.Log("本地主版本号为:" + GetLocalMainVersionNum(), DebugConsole.Color.blue);
            Asset_Version_Number = version.Asset_Version_Number;
            DebugConsole.Log("程序资源版本号为:" + Asset_Version_Number, DebugConsole.Color.blue);
            DebugConsole.Log("本地资源版本号为:" + GetLocalAssetVersionNum(), DebugConsole.Color.blue);
        }
        else
            DebugConsole.Log("未开启程序版本号控制");
    }

    /// <summary>
    /// 校验主版本号
    /// </summary>
    /// <returns></returns>
    public static bool CheckMainVersionNum()
    {
        if (!open)//如果未开启,默认返回假
            return false;

        string cur_version = PlayerPrefs.GetString("Main_Version_Number");
        if (cur_version == Main_Version_Number)
            return true;

        return false;
    }

    /// <summary>
    /// 获取本地版本号
    /// </summary>
    /// <returns></returns>
    public static string GetLocalMainVersionNum()
    {
        string cur_version = PlayerPrefs.GetString("Main_Version_Number");
        return cur_version;
    }

    /// <summary>
    /// 获取本地资源版本号
    /// </summary>
    /// <returns></returns>
    public static string GetLocalAssetVersionNum()
    {
        string cur_version = PlayerPrefs.GetString("Asset_Version_Number");
        return cur_version;
    }

    /// <summary>
    /// 检测资源版本号
    /// </summary>
    /// <param name="assetVersion"></param>
    /// <returns></returns>
    public static bool CheckAssetVersionNum(string assetVersion)
    {
        if (!open)//如果未开启,默认返回真
            return true;

        string cur_version = PlayerPrefs.GetString("Asset_Version_Number");
        if (cur_version == assetVersion)
            return true;

        return false;
    }

    /// <summary>
    /// 保存主版本号，资源版本号一同保存
    /// </summary>
    public static void SaveVersionNum()
    {
        if (!open)
            return;

        string cur_version = PlayerPrefs.GetString("Main_Version_Number");
        if (cur_version != Main_Version_Number)
            PlayerPrefs.SetString("Main_Version_Number", Main_Version_Number);

        //主版本号有变化，资源版本号一同保存
        string cur_asset_version = PlayerPrefs.GetString("Asset_Version_Number");
        if (cur_asset_version != Asset_Version_Number)
            PlayerPrefs.SetString("Asset_Version_Number", Asset_Version_Number);

        PlayerPrefs.Save();

        DebugConsole.Log("保存本地主版本号：" + Main_Version_Number);
        DebugConsole.Log("保存本地资源版本号：" + Asset_Version_Number);
    }

    /// <summary>
    /// 保存资源版本号
    /// </summary>
    /// <param name="assetVersion"></param>
    public static void SaveAssetVersionNum(string assetVersion)
    {
        PlayerPrefs.SetString("Asset_Version_Number", assetVersion);
        PlayerPrefs.Save();
        DebugConsole.Log("保存本地资源版本号：" + assetVersion);
    }

    /// <summary>
    /// 检测版本，拷贝资源
    /// </summary>
    /// <param name="mono">Monobehavior环境</param>
    /// <param name="callback">回调</param>
    public static void CheckVersionAndCopyAssets(MonoBehaviour mono, Action callback)
    {
        try
        {
            if (!Version.CheckMainVersionNum())   //主版本号不同移动资源
            {
                RawAssetsMover.MoveStreamingAssets2IOPath(mono, PUBLIC_PATH_DEFINE.StreamingAssetsPath, PUBLIC_PATH_DEFINE.AssetBundlesRootPath, callback);
                DebugConsole.Log("拷贝 ./StreamingAsset/ 下的资源");
            }
            else
            {//不需移动资源直接调用回调
                if (callback != null)
                {
                    callback.Invoke();
                }
            }
        }
        catch (Exception ex)
        {
            DebugConsole.LogError(ex.Message);
            DebugConsole.LogError(ex.StackTrace);
        }
    }
}