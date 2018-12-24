using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using System.Xml;

#region Asset Mover


/// <summary>
/// 原始资源搬运工--将原始资源移动到可IO路径
/// </summary>
public class RawAssetsMover
{
    /// <summary>
    /// 将资源配置文件移动到可读写路径
    /// </summary>
    /// <returns></returns>
    public static bool MoveAssetConfig2IOPath(string configPath, string IOPath, bool forceMove = false)
    {
        if (!File.Exists(IOPath) || forceMove)
        {
            //move 
            TextAsset rawAssetLoadConfigList = Resources.Load<TextAsset>(configPath);
            if (rawAssetLoadConfigList != null)
            {
                FileStream fs = File.Create(IOPath);
                using (fs)
                {
                    fs.Write(rawAssetLoadConfigList.bytes, 0, rawAssetLoadConfigList.bytes.Length);
                }

                //保存主版本号
                Version.SaveVersionNum();
                return true;
            }
        }
        return false;
    }

    public static int TotalTaskCount = 0;
    public static int DoneTaskCount = 0;

    static bool MoveStreamingAssets2IOPathFlag = false;
    static Action MoveStreamingAssets2IOPathCallBack;

    /// <summary>
    /// 将Streaming Asset下的资源 移动到 IO 路径下
    /// </summary>
    /// <param name="mono"></param>
    /// <param name="streamingAssetPath"></param>
    /// <param name="IOPath"></param>
    /// <param name="callback"></param>
    /// <param name="forceMove"></param>
    public static void MoveStreamingAssets2IOPath(MonoBehaviour mono, string streamingAssetPath, string IOPath, Action callback, bool forceMove = false)
    {
        if (!Directory.Exists(IOPath))
        {
            Directory.CreateDirectory(IOPath);
        }

        MoveStreamingAssets2IOPathCallBack = callback;
        TextAsset rawBundleManifest = Resources.Load<TextAsset>(PUBLIC_PATH_DEFINE.RawBundleManifest);
        string[] filesName = rawBundleManifest.text.Split('\n');
        TotalTaskCount = filesName.Length - 1;
        foreach (var fileName in filesName)
        {
            if (fileName != String.Empty)
            {
                string nFileName = fileName.Replace("\r", "");
                mono.StartCoroutine(MoveFile(streamingAssetPath + nFileName, IOPath + nFileName, TaskDone));
            }
        }

        MoveStreamingAssets2IOPathFlag = true;
    }

    //任务完成后计数
    static void TaskDone()
    {
        DoneTaskCount++;
    }

    /// <summary>
    /// Tick MoveStreamingAssets2IOPath,检测是否完成
    /// </summary>
    public static void TickMoveStreamingAssets2IOPath()
    {
        if (MoveStreamingAssets2IOPathFlag)
        {
            if (1f - GetProgress() < 0.0001f)
            //float 由于精度问题,不要判等
            {
                MoveStreamingAssets2IOPathFlag = false;

                if (MoveStreamingAssets2IOPathCallBack != null)
                {
                    MoveStreamingAssets2IOPathCallBack.Invoke();
                    MoveStreamingAssets2IOPathCallBack = null;
                }
            }
        }
    }

    public static float GetProgress()
    {
        if (DoneTaskCount == TotalTaskCount)
            return 1f;
        else
            return (float)DoneTaskCount / TotalTaskCount;
    }

    //测试
    public static void Test(MonoBehaviour mono)
    {
        mono.StartCoroutine(MoveFile(PUBLIC_PATH_DEFINE.AssetBundlesRootPath + "scenebase_1", "moved_scenebase_1", null));
    }

    public class MoveState
    {
        public FileStream fs;
        public WWW www;
        public Action callback;
    }

    static IEnumerator MoveFile(string url, string filePath, Action callback)
    {
        WWW www = new WWW(url);
        yield return www;
        if (!string.IsNullOrEmpty(www.error))
        {
            DebugConsole.LogError("www error = " + www.error + "\r\n" + url);
            yield break;
        }

        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        FileStream fs = File.Create(filePath);
        MoveState state = new MoveState();
        state.fs = fs;
        state.www = www;
        state.callback = callback;
        fs.BeginWrite(www.bytes, 0, www.bytes.Length, new AsyncCallback(MoveCallBack), state);
        DebugConsole.Log("Begin Move Path To " + filePath);
    }

    static void MoveCallBack(IAsyncResult result)
    {
        //Debug.LogError("call back!");
        if (result.AsyncWaitHandle.WaitOne())
        {
            MoveState state = (MoveState)result.AsyncState;
            state.fs.Flush();
            state.fs.Dispose();
            state.www.Dispose();
            //Debug.LogError("move Call Back!");
            if(state.callback != null)
            {
                state.callback.Invoke();
            }
        }
    }
}

#endregion

#region Asset Updater

/// <summary>
/// 资源更新逻辑
/// </summary>
public class AssetsUpdate : Singleton<AssetsUpdate>
{
    #region Data Define

    // AssetBundle 信息结构类
    public class BundleInfo
    {
        public string bundleName;        //bundle名
        public string md5;               //md5值
        public long length;              //文件长度
    }

    //下载任务数据结构
    public class DownloadInfo
    {
        public WWW www;
        public string url;
        public float progress
        {
            get
            {
                if (www != null)
                    return www.progress;
                return -1;
            }
        }
        //计时器
        public float timer = 0f;
        //超时时间
        public float timeout = 0f;
        //超时回调
        public HandleDownloadError errorCallback;
        //是否正在下载
        public bool flag = false;
        //文件长度
        public long length = 0;
        //当前下载字节数
        public long downloadByte
        {
            get
            {
                return (long)(progress * length);
            }
        }
        //最近一次check的进度
        float lastCheckProgress = 0f;
        //是否更新了进度
        public bool CheckUpdateProgress()
        {
            float cur = progress;

            //DebugConsole.Log("cur progress = " + progress);

            if (cur < 0)
                return false;

            if (cur - lastCheckProgress <= 0.0001)
            {
                return false;
            }

            //更新last
            lastCheckProgress = cur;
            return true;
        }

        //重置计时
        public void ResetTimer()
        {
            timer = 0f;
        }

        public void Reset()
        {
            //DebugConsole.Log("Reset!");
            timer = 0f;
            timeout = 0f;
            errorCallback = null;
            www = null;
            url = "invaild";
            flag = false;
            length = 0;
        }

        public override string ToString()
        {
            string s = string.Format("  URL = {0},Progress = {1}", url, progress);
            return s;
        }

        //错误回调
        public void InvokeErrorCallback()
        {
            try
            {
                if (errorCallback != null)
                    errorCallback.Invoke();
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError(ex.Message);
                DebugConsole.LogError(ex.StackTrace);
            }
        }
    }

    public delegate void HandleFinishDownload(WWW www);                                        //下载回调

    public delegate void HandleDownloadError();                                                //下载出错回调

    #endregion

    #region Private Field
    //mono运行环境
    private MonoBehaviour _mono = null;

    private string LocalPath;                                                            //本地资源路径
    private string ServerPath;                                                           //服务器资源路径(伪服务器)
    private string ConfigurePathName;                                                    //资源配置信息

    private Dictionary<string, BundleInfo> LocalDic;                                      //本地资源配置信息
    private Dictionary<string, BundleInfo> ServerDic;                                     //服务器资源配置信息
    private Stack<string> NeedUpadteAsstesPathName;                                       //需要更新的资源


    private bool isConfigureUpadate;                                                      //本地资源配置信息是否需要更新

    //保存资源服务器的XML
    private XmlDocument serverXML = null;

    //下载完成回调
    private Action downloadCallback;

    //下载完成？
    private bool downloadAssetsDone = true;

    //下载标志
    private bool downloadFlag = false;

    //当前下载任务
    private DownloadInfo curDownload;

    //bundle总尺寸
    private long totalBundleLength = 0;

    //下载文件总量
    private long totalDownloadLength = 0;

    //当前下载量
    private long curDownloadLength = 0;

    //下载速度
    private float downloadSpeed = 0f;

    //上一秒下载量
    private float lastSecondDownAmount = 0f;

    //下载计时器
    private float downloadTimer = 0f;

    #endregion

    #region Public Function

    public override void Init(params object[] paramList)
    {
        base.Init(paramList);
        _mono = (MonoBehaviour)paramList[0];
        LocalPath = PUBLIC_PATH_DEFINE.AssetBundlesRootPath;
        ServerPath = PUBLIC_PATH_DEFINE.AssetsServerUrl;
        //资源配置表
        ConfigurePathName = PUBLIC_PATH_DEFINE.BundlesIdentityListName;
        LocalDic = new Dictionary<string, BundleInfo>();
        ServerDic = new Dictionary<string, BundleInfo>();
        NeedUpadteAsstesPathName = null;
        isConfigureUpadate = false;
        curDownload = new DownloadInfo();
        curDownload.Reset();
    }

    /// <summary>
    /// 检测资源更新
    /// </summary>
    public void CheckUpdate(Action callback)
    {
        downloadCallback = callback;
        _mono.StartCoroutine(Download("file://" + LocalPath + ConfigurePathName, -1, delegate (WWW LocalPathwww)
               {
                  //解析本地Assets
                  if (LocalPathwww != null)
                       SaveConfigure(LocalPathwww.text, LocalDic);

                  //下载远程Assets并比较
                  _mono.StartCoroutine(Download(ServerPath + ConfigurePathName, -1, delegate (WWW ServerPathwww)
                     {
                         serverXML = new XmlDocument();
                         serverXML.LoadXml(ServerPathwww.text);
                         totalBundleLength = long.Parse(serverXML.DocumentElement.GetAttribute("TotalLength"));
                        //DebugConsole.Log("server bundle total length = " + ((float)totalBundleLength / 1024).ToString("0.00") + "KB");
                        string version = SaveConfigure(ServerPathwww.text, ServerDic);
                         if (!Version.CheckAssetVersionNum(version))
                         {
                            //计算需要下载的文件
                            CalculationUpdateInfo();

                            //开始下载更新资源
                            downloadFlag = true;
                             downloadAssetsDone = false;
                             UpdateAssets(version);
                         }
                         else
                         {//不需要更新资源
                            InvokeDoneCallback();
                         }
                     },
                     10000, () =>
                     {//error callback
                      //暂时先进游戏
                        downloadFlag = true;
                         downloadAssetsDone = true;
                         _mono.StopAllCoroutines();
                         DebugConsole.LogError("更新文件下载失败，请检查您的网络状况。");
                        //DebugConsole.LogError("error call back!");
                    }));
               },
              10000, () =>
               {//error callback
                   //暂时先进游戏
                   downloadFlag = true;
                   downloadAssetsDone = true;
                   _mono.StopAllCoroutines();
                   DebugConsole.LogError("本地文件加载失败");
                   //DebugConsole.LogError("error call back!");
               }));
    }

    public void Tick()
    {
        TickDownloadTimeout();
        TickDownloadAsset();
    }

    /// <summary>
    /// 获取当前下载文件量
    /// </summary>
    /// <returns></returns>
    public float GetDownloadLength()
    {
        float len = (float)curDownloadLength;
        if (curDownload != null)
        {
            len += curDownload.downloadByte;
        }
        return len / 1024;
    }

    /// <summary>
    /// 获取下载文件总量
    /// </summary>
    /// <returns></returns>
    public float GetTotalDownloadLength()
    {
        return ((float)totalDownloadLength / 1024);
    }

    /// <summary>
    /// 获取下载速度
    /// </summary>
    /// <returns></returns>
    public float GetDownloadSpeed()
    {
        return downloadSpeed;
    }

    #endregion

    #region Private Function

    /// <summary>
        /// 更新资源
        /// </summary>
    private void UpdateAssets(string version)
    {
        if (!isConfigureUpadate
            || NeedUpadteAsstesPathName == null)
        {
            downloadAssetsDone = true;
            DebugConsole.Log(string.Format("本地 version =（{0}）资源已是最新，不需要更新。", Version.GetLocalAssetVersionNum()), DebugConsole.Color.green);
            return;//不需要更新，返回
        }

        if (NeedUpadteAsstesPathName.Count == 0)
        {//下载任务完成，返回
            UpdateLocalConfigInfo(version);
            downloadAssetsDone = true;
            DebugConsole.Log(string.Format("本地 version =（{0}）资源更新完毕。", Version.GetLocalAssetVersionNum()), DebugConsole.Color.green);
            return;
        }

        string tempPath = NeedUpadteAsstesPathName.Pop();
        _mono.StartCoroutine(Download(ServerPath + tempPath, ServerDic[tempPath].length, delegate (WWW www)
             {
                 ReplaceAssets(LocalPath + tempPath, www.bytes);
                 UpdateAssets(version);
             }, 60000, () =>
               {//单文件下载失败
                //todo 日后做断点续传
                   DebugConsole.LogError("下载资源文件失败，请检查您的网络状况！");
                   _mono.StopAllCoroutines();//停止全部Coroutinues
                   downloadAssetsDone = true;//暂时进游戏
               }));
    }

    /// <summary>
    /// 替换资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="Asstesbytes"></param>
    private void ReplaceAssets(string path, byte[] Asstesbytes)
    {
        DateTime t1 = DateTime.Now;
        if (File.Exists(path))
            File.Delete(path);

        FileStream fileStream = File.Create(path);
        using (fileStream)
        {
            fileStream.Write(Asstesbytes, 0, Asstesbytes.Length);
            fileStream.Flush();
        }
        DateTime t2 = DateTime.Now;
        DebugConsole.Log("替换文件耗时：" + (t2 - t1).TotalMilliseconds + " 毫秒", DebugConsole.Color.white);
    }

    /// <summary>
    /// 更新本地配置信息
    /// </summary>
    private void UpdateLocalConfigInfo(string version)
    {
        //
        DebugConsole.Log("update version = " + version);
        Version.SaveAssetVersionNum(version);
        //
        ReplaceAssets(LocalPath + ConfigurePathName, Encoding.UTF8.GetBytes(serverXML.InnerXml));
        //DebugConsole.LogError("Replace local xml = " + LocalPath + ConfigurePathName);
    }

    /// <summary>
    /// 保存配置信息
    /// </summary>
    /// <param name="configureMessage">配置信息</param>
    /// <param name="dic">要保存到那个容器</param>
    /// <returns>版本号</returns>
    private string SaveConfigure(string configureMessage, Dictionary<string, BundleInfo> dic)
    {
        string version = "-1";

        if (configureMessage.Length == 0 || configureMessage == null)
        {
            return version;
        }
        //DebugConsole.Log("xml text = " + configureMessage);
        //解析xml
        XmlDocument xml = new XmlDocument();
        xml.LoadXml(configureMessage);
        //DebugConsole.Log(xml.InnerXml);
        if (xml.LastChild.Name != "BundleIdentity")
        {
            //DebugConsole.Log(xml.LastChild.Name);
            DebugConsole.LogError("AssetUpdate Error: XML Root Node Doesn't Match With BundleIdentity");
            return version;
        }

        XmlElement root = xml.LastChild as XmlElement;
        version = root.GetAttribute("version");

        XmlNodeList nodes = xml.LastChild.ChildNodes;

        foreach (XmlNode node in nodes)
        {
            if (!dic.ContainsKey(node.Name))
            {
                BundleInfo info = new BundleInfo();
                info.bundleName = node.Name;
                info.md5 = node.InnerText;
                info.length = long.Parse((node as XmlElement).GetAttribute("Length"));
                dic.Add(info.bundleName, info);
                //DebugConsole.Log("add key = " + node.Name + "     value = " + node.InnerText);
            }
        }
        return version;
    }

    /// <summary>
    /// 计算更新信息
    /// </summary>
    /// <param name="force">是否强制更新</param>
    private void CalculationUpdateInfo(bool force = false)
    {
        Stack<string> fileStack = new Stack<string>();
        foreach (string key in ServerDic.Keys)
        {
            if (LocalDic.ContainsKey(key) == false)//本地没有这个资源信息
            {
                fileStack.Push(key);                //存储需要添加的信息
                DebugConsole.Log(string.Format("Need Added Asset ({0})", key), DebugConsole.Color.yellow);
            }
            else //本地有这个资源信息
            {
                if (LocalDic[key].md5 != ServerDic[key].md5
                    || force)
                {
                    fileStack.Push(key);            //存储需要更新的信息
                    totalDownloadLength += ServerDic[key].length;
                    DebugConsole.Log(string.Format("Need Updated Asset ({0}),File Size = {1}", key, ServerDic[key].length), DebugConsole.Color.yellow);
                }
            }
        }

        if (fileStack.Count > 0)
            NeedUpadteAsstesPathName = fileStack;

        isConfigureUpadate = fileStack.Count > 0;

        DebugConsole.Log("CalculationUpdateInfo");
    }

    /// <summary>
    /// 下载资源
    /// </summary>
    /// <param name="url">url</param>
    /// <param name="handle">下载完成回调</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="errorCall">超时或错误回调</param>
    /// <returns></returns>
    private IEnumerator Download(string url, long length,HandleFinishDownload handle, int timeout = 10000, HandleDownloadError errorCall = null)
    {
        DebugConsole.Log("==>Download url = " + url);
        //创建download info
        curDownload.url = url;
        curDownload.length = length;
        curDownload.timeout = timeout;
        curDownload.errorCallback = errorCall;
        curDownload.flag = true;
        curDownload.www = new WWW(url);                   //打开下载路径

        //DebugConsole.Log("new www");
        yield return curDownload.www;            //等待下载完成

        //下载完成后
        DebugConsole.Log("<==www return url = " + url);

        WWW doneWWW = curDownload.www;  //保存下载好的info
        curDownloadLength += curDownload.length;
        //重置curDownload参数
        curDownload.Reset();

        if (string.IsNullOrEmpty(doneWWW.error))
        {
            if (handle != null)              //回调不为空
            {
                handle(doneWWW);             //传递下载信息
            }
            doneWWW.Dispose();               //释放资源
            //doneInfo.Reset();
            doneWWW = null;
        }
        else
        {
            DebugConsole.LogError("WWW Error = " + doneWWW.error);

            try
            {
                if (errorCall != null)
                    errorCall.Invoke();
            }
            catch (System.Exception ex)
            {
                DebugConsole.LogError("Error Handle Callback Exception!");
                DebugConsole.LogError(ex.Message);
                DebugConsole.LogError(ex.StackTrace);
            }
        }
    }

    /// <summary>
    /// 检测下载任务是否完成
    /// </summary>
    private void TickDownloadAsset()
    {
        if (downloadFlag)
        {
            if (downloadAssetsDone)
            {
                InvokeDoneCallback();
                downloadFlag = false;
            }
            else
            {
                downloadTimer += Time.deltaTime;
                if (downloadTimer >= 1.0f)
                {
                    downloadTimer -= 1.0f;
                    downloadSpeed = GetDownloadLength() - lastSecondDownAmount; //当前下载量 - 前一秒下载量
                    lastSecondDownAmount = GetDownloadLength();
                }
            }
        }
    }

    /// <summary>
    /// 检测当前下载时候超时
    /// </summary>
    private void TickDownloadTimeout()
    {
        //DebugConsole.LogError(curDownload);
        if (curDownload != null && curDownload.flag)
        {
            if (!curDownload.CheckUpdateProgress())
            {//如果下载进度没有更新，则计算超时
                curDownload.timer += (Time.deltaTime * 1000);   //转换成毫秒
                if (curDownload.timer >= curDownload.timeout)
                {//下载超时，超时回调
                    curDownload.InvokeErrorCallback();
                    DebugConsole.Log("AssetUpdate: Download Timeout,Info =" + curDownload.ToString());
                }
            }
            else
            {//下载进度更新，重置超时计时器
                curDownload.ResetTimer();
            }
        }
    }

    /// <summary>
    /// 调用下载任务完成回调
    /// </summary>
    private void InvokeDoneCallback()
    {
        try
        {
            if (downloadCallback != null)
                downloadCallback.Invoke();
            DebugConsole.Log("资源更新检查结束");
        }
        catch (System.Exception ex)
        {
            DebugConsole.LogError(ex.Message);
            DebugConsole.LogError(ex.StackTrace);
        }
    }

    #endregion
}

#endregion