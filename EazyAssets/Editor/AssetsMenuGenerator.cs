using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Xml;
using Assets.Script;

public class AssetsMenuGenerator
{
    //资产过滤
    public static string[] filterConfig = new string[] { "Packages", "NGUI", "XLua" };

    [MenuItem("工具/生成资产清单", false, 200)]
    public static void GenAssetsMenu()
    {
        string[] assetsMenu = AssetDatabase.GetAllAssetPaths();

        Dictionary<string, BundleAssetConfigTableData> assetsData = new Dictionary<string, BundleAssetConfigTableData>();

        foreach (var s in assetsMenu)
        {//遍历Assets资产库下所有路径

            //过滤
            bool pass = false;
            foreach (var f in filterConfig)
            {
                if (s.Contains(f))
                {
                    pass = true;
                    break;
                }
            }

            //生成资产目录文件
            if (!pass)
            {
                if (!File.Exists(s))
                {//文件存在验证
                    //DebugConsole.LogError("file doesn't exists path = " + s);
                    continue;
                }

                if (s.Contains("Resources"))
                {//resources data
                    //DebugConsole.Log("Resources Assets Path = " + s);
                    AddData(s, AssetType.ResourcesAsset, ref assetsData);

                }
                else if (s.Contains("AssetBundles/Raw"))
                {//bundle data
                    //DebugConsole.Log("StreamingAssets Assets Path = " + s);
                    AddData(s, AssetType.BundleAsset, ref assetsData);
                }
            }
        }

        //生成配置
        GenAssetMenuFile(PUBLIC_PATH_DEFINE.RawAssetLoadConfigListGenPath, assetsData.Values);
#if UNITY_EDITOR
        // Editor下强制更新资产库
        RawAssetsMover.MoveAssetConfig2IOPath(PUBLIC_PATH_DEFINE.RawAssetLoadConfigListPath, Application.persistentDataPath + "/AssetLoadConfigList.xml", true);
        DebugConsole.Log("本地资产清单已输出到：" + Application.persistentDataPath);
#endif
    }

    //添加数据
    static void AddData(string assetPath, Assets.Script.AssetType assetType, ref Dictionary<string, BundleAssetConfigTableData> assetsData)
    {
        BundleAssetConfigTableData data = new BundleAssetConfigTableData();
        string[] ps = assetPath.Split('/');
        string assetName = ps[ps.Length - 1];
        if (assetsData.ContainsKey(assetName))
        {
            DebugConsole.LogError(string.Format("Asset Menu Gen Error : \r\nasset(path = {0}) \r\nconflits with\r\nasset(path = {1})",
                assetPath, assetsData[assetName].AssetPath));

            return;
        }

        string assetBundleName = string.Empty;
        string assetBundleVariant = string.Empty;

        if (assetType == AssetType.BundleAsset)
        {//获取bundle name
            AssetImporter ai = AssetImporter.GetAtPath(assetPath);
            if (ai != null)
            {
                assetBundleName = ai.assetBundleName;
                assetBundleVariant = ai.assetBundleVariant;
                if (assetBundleVariant != string.Empty
                    && assetBundleVariant != null)
                {
                    assetBundleName = assetBundleName + "." + assetBundleVariant;
                }

                assetPath = string.Empty;
            }
            else
            {
                DebugConsole.LogError(string.Format("Asset Menu Gen Error : can't import asset(path = {0})", assetPath));
            }
        }
        else if (assetType == AssetType.ResourcesAsset)
        {//Resoureces拼接相对路径
            assetPath = assetPath.Replace("Assets/", "");           //去掉Assets
            assetPath = assetPath.Replace("Resources/", "");        //Resoucress
            //去除扩展名
            string[] sp = assetPath.Split('.');
            assetPath = assetPath.Replace("." + sp[sp.Length - 1], "");
        }

        data.AssetName = assetName;
        data.AssetPath = assetPath;
        data.BundleName = assetBundleName;
        data.assetType = (int)assetType;
        assetsData.Add(data.AssetName, data);
    }

    static void GenAssetMenuFile(string filePath, IEnumerable<BundleAssetConfigTableData> data)
    {//生成xml文件
        string[] ps = filePath.Split('/');
        string dir = filePath.Replace("/" + ps[ps.Length - 1], "");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(filePath))
            using (File.Create(filePath)) ;

        XmlDocument xml = new XmlDocument();
        XmlElement root = xml.CreateElement("root");
        xml.AppendChild(root);
        foreach (var d in data)
        {
            XmlElement el = xml.CreateElement("BundleAssetConfigData");
            el.SetAttribute("AssetName", d.AssetName);
            el.SetAttribute("BundleName", d.BundleName);
            el.SetAttribute("AssetPath", d.AssetPath);
            el.SetAttribute("assetType", d.assetType.ToString());
            root.AppendChild(el);
        }
        xml.Save(filePath);

        AssetDatabase.Refresh();
    }

    [MenuItem("工具/移动资产清单", false, 210)]
    public static void MoveAssetMenu()
    {
        RawAssetsMover.MoveAssetConfig2IOPath(PUBLIC_PATH_DEFINE.RawAssetLoadConfigListPath, Application.persistentDataPath + "/AssetsLoadConfigList.xml", true);
        DebugConsole.Log("本地资产清单已输出到：" + Application.persistentDataPath);
    }
}
