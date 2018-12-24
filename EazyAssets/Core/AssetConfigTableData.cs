using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Xml;
using System.IO;

/// <summary>
/// Resources AB 资源的加载
/// </summary>
namespace Assets.Script
{
    #region 数据定义

    /// <summary>
    ///资源类型
    /// </summary>
    public enum AssetType
    {
        ResourcesAsset,
        BundleAsset
    }

    /// <summary>
    /// AB资源的配置信息类
    /// 资源名, 资源路径,集合名直接的映射关系
    /// </summary>
    public class BundleAssetConfigTableData
    {
        public string AssetName { get; set; } // 资源的名字

        public string BundleName { get; set; } // Bundle名字

        public string AssetPath { get; set; } // 资源路径

        public int assetType { get; set; }//资源类型
    }

    /// <summary>
    /// 资源集合配置类
    /// 获取AB资源的具体信息信息
    /// </summary>
    public class BundleAssetConfig
    {
        private static BundleAssetConfig instance;

        /// <summary>
        /// 单例
        /// </summary>
        public static BundleAssetConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BundleAssetConfig();
                }
                return instance;
            }
        }

        //AB包的资源配置表
        private BundleAssetConfigTableData[] datas;

        /// <summary>
        /// 资源集合配置表
        /// </summary>
        /// <returns></returns>
        public bool Load(string xmlText)
        {//读取xml
            datas = null;
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlText);
            if (xml.FirstChild.Name == "root")
            {
                var childNodes = xml.FirstChild.ChildNodes;
                datas = new BundleAssetConfigTableData[childNodes.Count];
                int index = 0;
                foreach (var c in childNodes)
                {
                    XmlElement ele = c as XmlElement;
                    if (ele.Name == "BundleAssetConfigData")
                    {
                        BundleAssetConfigTableData data = new BundleAssetConfigTableData();
                        data.AssetName = ele.GetAttribute("AssetName");
                        data.AssetPath = ele.GetAttribute("AssetPath");
                        data.BundleName = ele.GetAttribute("BundleName");
                        data.assetType = int.Parse(ele.GetAttribute("assetType"));
                        datas[index] = data;
                        index++;
                    }
                    else
                        continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// 取得所有资源集合数据
        /// </summary>
        /// <returns></returns>
        public BundleAssetConfigTableData[] All()
        {
            return datas;
        }

        /// <summary>
        /// 资源名称到集合名称的映射
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <returns>集合名称</returns>
        public string AssetNameToBundleName(string assetName)
        {
            foreach (var it in datas)
            {
                if (it.AssetName == assetName)
                    return it.BundleName;
            }
            return string.Empty;
        }

        public string AsserNameToAssetPath(string assetName)
        {
            foreach (var it in datas)
            {
                if (it.AssetName == assetName)
                    return it.AssetPath;
            }
            return string.Empty;
        }

        /// <summary>
        /// 获取资源类型的映射
        /// </summary>
        /// <param name="assetType">资源类型</param>
        /// <returns></returns>
        public int AssetNameToAssetType(string assetName)
        {
            foreach (var it in datas)
            {
                if (it.AssetName == assetName)
                    return it.assetType;
            }

            DebugConsole.LogError(string.Format("Can't Find Asset({0}) Load Type!", assetName));
            return -1;
        }
    }

    #endregion

    /// <summary>
    /// 资源管理类
    /// 根据不同类型的资源进行资源的加载
    /// </summary>
    public class AssetManager : Singleton<AssetManager>, IAssetProvider
    {
        ResourcesAssetProvider resourcesAssetProvider = new ResourcesAssetProvider();
        BundleAssetProvider bundleAssetProvider = null;

        public void InitAssetManager(GameObject go, System.Action callback)
        {
            //首先加载 资产加载路径清单
            string assetLoadConfigXml = "";

            if (!File.Exists(PUBLIC_PATH_DEFINE.AssetLoadConfigListPath)    //若读写路径中不存在资源配置文件，则拷贝包中自带的文件。
                || !Version.CheckMainVersionNum())                              //若主版本号不同，则拷贝
            {
                RawAssetsMover.MoveAssetConfig2IOPath(PUBLIC_PATH_DEFINE.RawAssetLoadConfigListPath, PUBLIC_PATH_DEFINE.AssetLoadConfigListPath, true);
            }

            StreamReader sr = File.OpenText(PUBLIC_PATH_DEFINE.AssetLoadConfigListPath);
            using (sr)
            {
                assetLoadConfigXml = sr.ReadToEnd();
            }

            //DebugConsole.Log("xml = " + assetLoadConfigXml, DebugConsole.Color.green);
            BundleAssetConfig.Instance.Load(assetLoadConfigXml);

            //创建Bundle Provider
            bundleAssetProvider = go.AddComponent<BundleAssetProvider>();

            bundleAssetProvider.LoadCommon("common", callback);
        }

        #region prefab

        public GameObject GetPrefab(string prefabName)
        {
            return GetAsset<GameObject>(prefabName);
        }

        public void GetPrefabAsync(string prefabName, DelegateDefine.GetPrefabCallback callback)
        {
            GetAssetAsync<GameObject>(prefabName, t => { callback.Invoke(t); });
        }

        #endregion

        #region AudioClip

        public AudioClip GetAudioClip(string clipName)
        {
            return GetAsset<AudioClip>(clipName);
        }

        public void GetAudioClipAsync(string clipName, DelegateDefine.GetAudioClipCallback callback)
        {
            GetAssetAsync<AudioClip>(clipName, t => { callback.Invoke(t); });
        }

        #endregion AudioClip

        #region TextAsset
        public TextAsset GetText(string textName)
        {
            return GetAsset<TextAsset>(textName);
        }

        public void GetTextAsync(string textName, DelegateDefine.GetTextCallback callback)
        {
            GetAssetAsync<TextAsset>(textName, t => { callback.Invoke(t); });
        }
        #endregion

        #region Animation
        public Animation GetAnimation(string anima)
        {
            return GetAsset<Animation>(anima);
        }

        public void GetAnimationAsync(string anima, DelegateDefine.GetAnimationCallback callback)
        {
            GetAssetAsync<Animation>(anima, t => { callback.Invoke(t); });
        }
        #endregion

        #region Material
        public Material GetMaterial(string materialName)
        {
            return GetAsset<Material>(materialName);
        }
        public void GetMaterialAsync(string materialName, DelegateDefine.GetMaterialCallback callback)
        {
            GetAssetAsync<Material>(materialName, t => { callback.Invoke(t); });
        }
        #endregion

        #region Shader
        public Shader GetShader(string ShaderPath)
        {
            return GetAsset<Shader>(ShaderPath);
        }
        public void GetShaderAsync(string ShaderPath, DelegateDefine.GetShaderCallback callback)
        {
            GetAssetAsync<Shader>(ShaderPath, t => { callback.Invoke(t); });
        }
        #endregion

        #region Texture
        public Texture GetTexture(string TexturePath)
        {
            return GetAsset<Texture>(TexturePath);
        }
        public void GetTextureAsync(string TexturePath, DelegateDefine.GetTextureCallback callback)
        {
            GetAssetAsync<Texture>(TexturePath, t => { callback.Invoke(t); });
        }
        #endregion

        /// <summary>
        /// 异步获取场景资源
        /// </summary>
        /// <param name="sceneName">场景名</param>
        /// <param name="callback">加载回调</param>
        public void GetSceneAsync(string sceneName, System.Action callback)
        {
            string bundleName = BundleAssetConfig.Instance.AssetNameToBundleName(sceneName);
            LoadBundles(bundleName, callback);
        }

        public T GetAsset<T>(string assetName)
            where T : UnityEngine.Object
        {
            int bundleType = BundleAssetConfig.Instance.AssetNameToAssetType(assetName);
            DebugConsole.Log("load bundle type = " + bundleType + " assetname = " + assetName, DebugConsole.Color.yellow);
            T temp = null;
            if (bundleType == (int)AssetType.BundleAsset)
            {
                temp = bundleAssetProvider.LoadAsset<T>(assetName);
            }
            else if (bundleType == (int)AssetType.ResourcesAsset)
            {
                string assetPath = BundleAssetConfig.Instance.AsserNameToAssetPath(assetName);
                temp = resourcesAssetProvider.LoadAsset<T>(assetPath);
            }
            return temp;
        }

        public void GetAssetAsync<T>(string assetName, System.Action<T> callback)
            where T : UnityEngine.Object
        {
            int bundleType = BundleAssetConfig.Instance.AssetNameToAssetType(assetName);
            if (bundleType == (int)AssetType.ResourcesAsset)
            {
                DebugConsole.LogError(string.Format("Only Can Load Bundle Assets({0}) Asynclly!", assetName));
                return;
            }

            bundleAssetProvider.LoadAssetAsync<T>(assetName, callback);
        }

        /// <summary>
        /// 加载资源包
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="callback"></param>
        public void LoadBundles(string bundle, System.Action callback)
        {
            bundle = bundle.ToLower();
            bundleAssetProvider.Load(bundle, callback);
        }

        /// <summary>
        /// 加载公共资源包
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="callback"></param>
        public void LoadCommonBundles(string bundle, System.Action callback)
        {
            bundle = bundle.ToLower();
            bundleAssetProvider.LoadCommon(bundle, callback);
        }

        public void Unload(bool option)
        {
            resourcesAssetProvider.Unload(option);
            bundleAssetProvider.Unload(option);
        }

        public void Unload(string assetName)
        {
            resourcesAssetProvider.Unload(assetName);
            bundleAssetProvider.Unload(assetName);
        }
    }

    /// <summary>
    /// 动态资源（Resource)提供类
    /// </summary>
    public class ResourcesAssetProvider
    {
        public T LoadAsset<T>(string assetName)
            where T : UnityEngine.Object
        {
            return Resources.Load<T>(assetName);
        }

        public void Unload(bool option)
        {
            Resources.UnloadUnusedAssets();
        }

        //无需实现
        public void Unload(string assetName) { }
    }

    /// <summary>
    /// 根据AssetsBundle包进行资源的缓冲,
    /// </summary>
    public class BundleAssetProvider : MonoBehaviour
    {
        //资源所在的路径
        public string urlbase = "/?";
        // 资源名称跟AssetBundle资源的映射关系 需要卸载的
        Dictionary<string, AssetBundle> packageBundles = new Dictionary<string, AssetBundle>();

        //commmon bundles 不需要卸载的 
        Dictionary<string, AssetBundle> commonBundles = new Dictionary<string, AssetBundle>();

        //AB资源的依赖信息
        AssetBundleManifest manifest = null;

        void Awake()
        {
            urlbase = "file://" + PUBLIC_PATH_DEFINE.AssetBundlesRootPath;
        }

        #region Core

        /// <summary>
        /// 检测资源包是否已加载过
        /// </summary>
        /// <returns></returns>
        bool CheckBundleLoaded(string bundleName)
        {
            if (commonBundles.ContainsKey(bundleName)
                || packageBundles.ContainsKey(bundleName))
                return true;

            return false;
        }

        /// <summary>
        /// 加载资源集合
        /// </summary>
        /// <returns></returns>
        IEnumerator RealLoad(string assetBundle, System.Action callback, bool loadCommon = false)
        {
            yield return DownloadManifest(PUBLIC_PATH_DEFINE.AssetBundlesManifestPath);
            if (manifest == null)
            {
                DebugConsole.LogError("BundleAssetProvider Error : Can't Load AssetBundle Manifest\r\nFile Path = " + PUBLIC_PATH_DEFINE.AssetBundlesManifestPath);
                yield break;
            }
            string[] allToLoad = GetAllBundlesOrderByDependency(assetBundle);
            DebugConsole.Log("all to load length = " + allToLoad.Length, DebugConsole.Color.yellow);
            foreach (string toLoad in allToLoad)
            {
                if (CheckBundleLoaded(toLoad))      //不要重复加载同一AssetBundle
                    continue;

                DebugConsole.Log("bundleName = " + toLoad, DebugConsole.Color.green);
                string url = urlbase + toLoad;
                Hash128 hash = manifest.GetAssetBundleHash(toLoad);
                WWW www = WWW.LoadFromCacheOrDownload(url, hash);
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    AssetBundle bundle = www.assetBundle;

                    if (!loadCommon)
                        packageBundles.Add(toLoad, bundle);
                    else
                        commonBundles.Add(toLoad, bundle);
                }
                else
                    DebugConsole.LogError("RealLoad AssetBundle => WWW Error = " + www.error);
            }

            if (callback != null)
            {
                try
                {
                    DebugConsole.Log("Load All Bundles Done!");
                    callback.Invoke();
                }
                catch (System.Exception ex)
                {
                    DebugConsole.LogError("Load Bundles Invoke Error!");
                    DebugConsole.LogError(ex.Message);
                    DebugConsole.LogError(ex.StackTrace);
                }
            }
        }

        /// <summary>
        /// 获取所有要加载的结合（AssetBundle），按加载所需的顺序排序。
        /// </summary>
        /// <param name="manifest"></param>
        /// <returns>排序后的结合名称列表</returns>
        string[] GetAllBundlesOrderByDependency(string assetBudle)
        {
            string[] allToOrder = manifest.GetAllDependencies(assetBudle);
            List<string> ordered = new List<string>();
            foreach (string dep in allToOrder)
            {
                if (!ordered.Contains(dep))
                    ordered.Add(dep);
            }

            if (!ordered.Contains(assetBudle))
                ordered.Add(assetBudle);

            return ordered.ToArray();
        }

        /// <summary>
        /// 递归获取资源的所有依赖
        /// </summary>
        /// <param name="manifest">依赖</param>
        /// <param name="name">名称</param>
        /// <param name="orderedList"></param>
        void OrderOneBundleCycle(AssetBundleManifest manifest, string name, ref List<string> orderedList)
        {
            if (orderedList.Contains(name))
            {
                return;
            }
            //获取本资源的所有依赖信息
            string[] allToOrder = manifest.GetAllDependencies(name);
            if (allToOrder.Length == 0)
            {
                orderedList.Add(name);
            }
            foreach (string dep in allToOrder)
            {
                OrderOneBundleCycle(manifest, dep, ref orderedList);
            }
        }

        /// <summary>
        /// 下载本地的资源的依赖信息
        /// </summary>
        /// <returns></returns>
        IEnumerator DownloadManifest(string loadManifest)
        {
            if (manifest != null)
                yield break;

            string url = loadManifest;
            if (!url.Contains("file://"))
                url = "file://" + url;

            WWW www = new WWW(url);
            yield return www;
            if (string.IsNullOrEmpty(www.error))
            {
                AssetBundle bundle = www.assetBundle;
                //DebugConsole.Log("loaded manifest file name = " + bundle.ToString());
                //获取AssetBundle的依赖关系印象
                manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            }
            else
                DebugConsole.LogError("WWW Error = " + www.error);

        }

        #endregion

        #region 资源加载接口

        /// <summary>
        /// 加载到Package
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="callback"></param>
        public void Load(string assetBundle, System.Action callback)
        {
            StartCoroutine(RealLoad(assetBundle, callback));
        }

        /// <summary>
        /// 加载到Common
        /// </summary>
        /// <param name="assetBundle"></param>
        /// <param name="callback"></param>
        public void LoadCommon(string assetBundle, System.Action callback)
        {
            StartCoroutine(RealLoad(assetBundle, callback, true));
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <param name="callback"></param>
        public void LoadAssetAsync<T>(string assetName,System.Action<T> callback)
            where T : UnityEngine.Object
        {
            string bundleName = BundleAssetConfig.Instance.AssetNameToBundleName(assetName);
            if (string.IsNullOrEmpty(bundleName))
            {//找不到映射包，返回
                DebugConsole.LogError(string.Format("Empty Mapping Bundle by Asset({0})", assetName));
                return;
            }

            T asset = null;

            if (commonBundles.ContainsKey(bundleName))
            {//先看Common中有无资源
                asset = commonBundles[bundleName].LoadAsset<T>(assetName);
                callback.Invoke(asset);
                return;
            }

            //若package中无资源包，尝试加载资源包
            if (!packageBundles.ContainsKey(bundleName))
            {
                Load(bundleName, () =>
                {
                    if (!packageBundles.ContainsKey(bundleName))
                    {
                        DebugConsole.LogError(string.Format("BundleAssetProvider Load Error: Can't Find Bundle({0}) by Raw Resource({1})", bundleName, assetName));
                        return;
                    }

                    //加载资源
                    asset = packageBundles[bundleName].LoadAsset<T>(assetName);
                    if (asset == null)
                        DebugConsole.LogError(string.Format("Can't Load Asset({0})", assetName));
                    
                    try
                    {
                        callback.Invoke(asset);
                        return;
                    }
                    catch (System.Exception ex)
                    {
                        DebugConsole.LogError("Load Asset Async Callback Error ==> ");
                        DebugConsole.LogError(ex.Message);
                        DebugConsole.LogError(ex.StackTrace);
                    }
                });
            }
        }

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="assetName"></param>
        /// <returns></returns>
        public T LoadAsset<T>(string assetName)
            where T : UnityEngine.Object
        {
            string bundleName = BundleAssetConfig.Instance.AssetNameToBundleName(assetName);
            if (string.IsNullOrEmpty(bundleName))
            {
                DebugConsole.LogError(string.Format("Empty Mapping Bundle by Asset({0})", assetName));
                return null;
            }

            if (commonBundles.ContainsKey(bundleName))
            {//先看Common中有无资源
                return commonBundles[bundleName].LoadAsset<T>(assetName);
            }
            if (!packageBundles.ContainsKey(bundleName))
            {
                DebugConsole.LogError(string.Format("BundleAssetProvider Load Error: Can't Find Bundle({0}) by Raw Resource({1})", bundleName, assetName));
                return null;
            }
            AssetBundle bundle = packageBundles[bundleName];
            return bundle.LoadAsset<T>(assetName);
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        public void Unload(bool common)
        {
            if (common)
            {//如果清除common
                foreach (var cm in commonBundles)
                {
                    cm.Value.Unload(true);
                }
                commonBundles.Clear();
            }

            foreach (var ab in packageBundles)
            {
                ab.Value.Unload(true);
            }
            packageBundles.Clear();
            manifest = null;
        }

        public void Unload(string bundleName)
        {
            if (commonBundles.ContainsKey(bundleName))
            {
                commonBundles[bundleName].Unload(true);
                commonBundles.Remove(bundleName);
            }

            if (packageBundles.ContainsKey(bundleName))
            {
                packageBundles[bundleName].Unload(true);
                packageBundles.Remove(bundleName);
            }
        }

        #endregion
    }

    /// <summary>
    /// 资源提供接口
    /// 这个类的接口是按资源的种类确定的
    /// </summary>
    public interface IAssetProvider
    {
        /// <summary>
        /// 获取预制体
        /// </summary>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        GameObject GetPrefab(string prefabName);
        void GetPrefabAsync(string prefabName, DelegateDefine.GetPrefabCallback callback);

        /// <summary>
        /// 获取声音片段
        /// </summary>
        /// <param name="clipName"></param>
        /// <returns></returns>
        AudioClip GetAudioClip(string clipName);
        void GetAudioClipAsync(string prefabName, DelegateDefine.GetAudioClipCallback callback);

        /// <summary>
        /// Get Text Asset
        /// </summary>
        /// <param name="textName"></param>
        /// <returns></returns>
        TextAsset GetText(string textName);
        void GetTextAsync(string textName, DelegateDefine.GetTextCallback callback);

        /// <summary>
        /// Get Animation
        /// </summary>
        /// <param name="anima"></param>
        /// <returns></returns>
        Animation GetAnimation(string anima);
        void GetAnimationAsync(string anima, DelegateDefine.GetAnimationCallback callback);

        /// <summary>
        /// Get Material
        /// </summary>
        /// <param name="materialPath"></param>
        /// <returns></returns>
        Material GetMaterial(string materialPath);
        void GetMaterialAsync(string materialPath, DelegateDefine.GetMaterialCallback callback);

        /// <summary>
        ///  Get Shader
        /// </summary>
        /// <param name="ShaderPath"></param>
        /// <returns></returns>
        Shader GetShader(string ShaderPath);
        void GetShaderAsync(string ShaderPath, DelegateDefine.GetShaderCallback callback);

        /// <summary>
        /// Get Texture
        /// </summary>
        /// <param name="TexturePath"></param>
        /// <returns></returns>
        Texture GetTexture(string TexturePath);
        void GetTextureAsync(string TexturePath, DelegateDefine.GetTextureCallback callback);

        void Unload(bool option);

        void Unload(string assetName);
    }

    public class DelegateDefine
    {
        public delegate void GetPrefabCallback(GameObject asset);
        public delegate void GetAudioClipCallback(AudioClip asset);
        public delegate void GetTextCallback(TextAsset asset);
        public delegate void GetAnimationCallback(Animation asset);
        public delegate void GetMaterialCallback(Material asset);
        public delegate void GetShaderCallback(Shader asset);
        public delegate void GetTextureCallback(Texture asset);
    }
}