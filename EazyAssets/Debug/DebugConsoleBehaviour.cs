#define Debug
#define UNITY_DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

/// <summary>
/// Debug Console Tool
/// </summary>
public class DebugConsole
{
    public enum Color
    {
        white = 0xffffff,
        red = 0xff0000,
        green = 0x00ff00,
        blue = 0x0000ff,
        yellow = 0xffff00,
        grey = 0xc0c0c0,
        lgrey = 0xB2B2B2,
    }

    //开启Debug Console
    public static bool Open = true;

#if UNITY_EDITOR
    //开启log
    public static bool isLog = true;
#else
    public static bool isLog = false;
#endif

    //info sb
    public static StringBuilder ConsoleLogInfo = new StringBuilder();

    //文本高度
    public static int lineHeight;

    //行字符数
    public static int perCharCountInLine = 50;

    //字体像素高度
    public const float perCharPixel_h = 18f;
    //字体像素长度
    public const float perCharPixel_w = 9f;

    public static bool openLogSwitch = false;

    public static void Init()
    {
        Clear();
        GameObject go = new GameObject("Debug Console");
        go.AddComponent<DebugConsoleBehaviour>();
        GameObject.DontDestroyOnLoad(go);
        perCharCountInLine = (int)(Screen.width / perCharPixel_w);
        isLog = true;
    }

    public static void Log(object message, Color color = Color.lgrey, bool error = false)
    {
        if (!isLog)
            return;

        openLogSwitch = true;

        if (message == null)
            message = "Null";

        string logInfo = message.ToString();

#if UNITY_EDITOR && UNITY_DEBUG

        if (!error)
        {
            string unityLog = string.Format("<color=#{0}>{1}</color>", ((int)color).ToString("x6"), message);
            Debug.Log(unityLog);
        }
        else
            Debug.LogError(logInfo);
#endif

        logInfo = string.Format("[{0}] {1}", System.DateTime.Now.ToString("hh:mm:ss"), logInfo);

        if (logInfo.IndexOf("\r\n") != -1)
        {
            StringBuilder sb = new StringBuilder();
            //计算line height
            string[] subString = logInfo.Split(new string[] { "\r\n" }, System.StringSplitOptions.RemoveEmptyEntries);

            int lineCount = subString.Length;

            for (int i = 0; i < subString.Length; i++)
            {
                lineCount += CountLine(subString[i], out subString[i]);
                sb.Append(subString[i] + "\r\n");
            }

            lineHeight += lineCount;
            logInfo = sb.ToString();
        }
        else
        {
            lineHeight += CountLine(logInfo, out logInfo);
            logInfo += "\r\n";
        }

        logInfo = string.Format("<color=#{0}>{1}</color>", ((int)color).ToString("x6"), logInfo);

        if (ConsoleLogInfo != null)
            ConsoleLogInfo.Append(logInfo);
    }

    //自动折行
    static int CountLine(string s, out string os)
    {
        if (s == string.Empty)
        {
            os = s;
            return 0;
        }

        int line = (s.Length / perCharCountInLine) + 1;
        //Debug.Log("line = " + line);
        //Debug.Log(s + "    lenght = " + s.Length);
        string ns = s;
        for (int i = 1; i < line; i++)
        {
            int insertIndex = i * perCharCountInLine;
            s = s.Insert(insertIndex, "\r\n");
        }

        os = s;
        return line;
    }

    public static void LogError(object message, Color color = Color.red)
    {
        Log(message, color, true);
    }

    public static void Clear()
    {
        ConsoleLogInfo = new StringBuilder();
        lineHeight = 0;
    }

    //是否添加了 Log？
    public static bool LogSwitch()
    {
        if (openLogSwitch)
        {
            openLogSwitch = false;
            return true;
        }
        else
            return false;
    }
}

public class DebugConsoleBehaviour : MonoBehaviour
{
    //显示Console
    bool showConsole = false;

    Vector2 scrollPos = Vector2.zero;

    Rect debugButton;

    Rect area;

    Rect display;

    Rect view;

    Rect closeButton;

    Rect clearButton;

    //上下滚动按钮
    Rect upButton;
    Rect downButton;

    int screenW;
    int screenH;

    //字体风格
    GUIStyle fontStyle;

    bool isPauseAutoScroll = false;

    //自动滚动
    public bool autoScrollToEnd = true;


    private void Awake()
    {
        screenW = Screen.width;
        screenH = Screen.height;
        Resize(screenW, screenH);
        fontStyle = new GUIStyle();
        fontStyle.fontSize = 16;
        //Debug.Log(fontStyle.font.lineHeight);
    }

    private void Start()
    {
        //DebugConsole.Log("Debug Rect = " + debugButton);
        //DebugConsole.Log("Clear Rect = " + clearButton);
        //DebugConsole.Log("Close Rect = " + closeButton);
    }

    //重置尺寸
    void Resize(int screenW, int ScreenH)
    {
        debugButton = new Rect(0, 0, screenW / 5, screenH / 10);
        area = new Rect(0, 0, screenW, screenH / 2);
        display = new Rect(screenW / 15, screenH / 30, area.width - 2 * screenW / 15, area.height - screenH / 8);
        clearButton = new Rect(screenW / 10, area.height - ScreenH / 12, screenW / 5, ScreenH / 12);
        closeButton = new Rect(screenW - screenW / 10 - screenW / 5, area.height - ScreenH / 12, screenW / 5, ScreenH / 12);

        upButton = new Rect(screenW * 0.3f, area.height - ScreenH / 12, screenW / 5, ScreenH / 12);
        downButton = new Rect(screenW - screenW * 0.1f - screenW * 0.4f, area.height - ScreenH / 12, screenW / 5, ScreenH / 12);
    }

    bool isDebug = false;
    bool isClose = false;
    bool isClear = false;
    bool isUp = false;
    bool isDown = false;

    //绘制UI
    private void OnGUI()
    {
        if (screenH != Screen.height || screenW != Screen.width)
        {
            screenW = Screen.width;
            screenH = Screen.height;
            Resize(screenW, screenH);
        }

        if (DebugConsole.Open)
        {
            if (!showConsole)
            {//console button
#if UNITY_EDITOR || UNITY_STANDALONE
                GUI.Box(debugButton, "Open Console", "button");
                if (isDebug)
                {
                    showConsole = true;
                    isDebug = false;
                }
#endif
            }
            else
            {//console ui
                GUI.Box(area, "DebugConsole");
                GUI.Box(display, "");
                view = new Rect(display.x, display.y, display.width, DebugConsole.lineHeight * DebugConsole.perCharPixel_h);
                //自动滚屏
                if (autoScrollToEnd && !isPauseAutoScroll && DebugConsole.LogSwitch())
                    scrollToEnd();

                //显示信息
                GUI.BeginScrollView(display, scrollPos, view, false, false);
                GUI.Label(view, DebugConsole.ConsoleLogInfo.ToString(), fontStyle);
                GUI.EndScrollView();

                //close
                GUI.Box(closeButton, "Close", "button");
                if (isClose)
                {
                    isClose = false;
                    showConsole = false;
                }

                GUI.Box(clearButton, "Clear", "button");
                if (isClear)
                {
                    isClear = false;
                    DebugConsole.Clear();
                }

                GUI.Box(upButton, "Up", "button");
                GUI.Box(downButton, "Down", "button");

            }
        }
    }

    Vector2 touchPos;
    private void FixedUpdate()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonUp(0))
        {
            Vector2 mousePos = Input.mousePosition;
            touchPos = new Vector2(mousePos.x, screenH - mousePos.y);
            PressCheckUp(touchPos);
        }
        else if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Input.mousePosition;
            touchPos = new Vector2(mousePos.x, screenH - mousePos.y);
            PressCheckDown(touchPos);
        }
#elif UNITY_ANDROID
        Touch touch = Input.GetTouch(0);
        touchPos = touch.position;
        touchPos = new Vector2(touchPos.x, screenH - touchPos.y);
        if (touch.phase == TouchPhase.Ended)
        {
            PressCheckUp(touchPos);
        }
        else if(touch.phase == TouchPhase.Began)
        {
            PressCheckDown(touchPos);
        }

        if(Input.GetTouch(0).phase == TouchPhase.Moved
            &&Input.GetTouch(1).phase == TouchPhase.Moved
            &&Input.GetTouch(2).phase == TouchPhase.Moved)
        {//安卓端三指滑动显示
            showConsole = true;
        }
#endif

        //if (Input.GetKey(KeyCode.P))
        //{
        //    DebugConsole.Log("point GUI pos = ");
        //}
        if (isUp)
            scrollUp();
        if (isDown)
            scrollDown();
    }

    //滚动到底部
    void scrollToEnd()
    {
        float ymax = view.height - display.height + DebugConsole.lineHeight;
        scrollPos = new Vector2(0, ymax);
    }

    void scrollDown()
    {
        float ymax = view.height - display.height + DebugConsole.lineHeight;

        if (scrollPos.y == ymax)
            return;

        if (scrollPos.y < ymax)
            scrollPos += new Vector2(0, Time.fixedDeltaTime * screenH * 0.5f);
        else
        {
            scrollPos = new Vector2(0, ymax);
            isPauseAutoScroll = false;
        }
    }

    void scrollUp()
    {
        float ymin = 0f;

        if (scrollPos.y == ymin)
            return;

        if (scrollPos.y > ymin)
            scrollPos -= new Vector2(0, Time.fixedDeltaTime * screenH * 0.5f);
        else
            scrollPos = new Vector2(0, ymin);
    }

    void PressCheckDown(Vector2 touchPos)
    {
        if (inRect(touchPos, upButton))
        {
            isPauseAutoScroll = true;        //暂停自动滚动
            isUp = true;
            isDown = false;
        }

        if (inRect(touchPos, downButton))
        {
            isPauseAutoScroll = true;        //暂停自动滚动
            isDown = true;
            isUp = false;
        }
    }

    void PressCheckUp(Vector2 touchPos)
    {
        Vector2 point = touchPos;
        // = GUIUtility.ScreenToGUIPoint(touchPos);
        //DebugConsole.Log("point pos = " + touchPos);
        if (inRect(point, debugButton))
        {
            isDebug = true;
        }

        if (inRect(point, closeButton))
        {
            isClose = true;
        }

        if (inRect(point, clearButton))
        {
            isClear = true;
        }

        if (inRect(point, upButton))
        {
            //Debug.Log("up-up");
            isUp = false;
        }

        if (inRect(point, downButton))
        {
            //Debug.Log("down-up");
            isDown = false;
        }
    }

    bool inRect(Vector2 pos, Rect rect)
    {
        if (pos.x >= rect.xMin && pos.x <= rect.xMax
            && pos.y >= rect.yMin && pos.y <= rect.yMax)
        {
            return true;
        }
        return false;
    }

}