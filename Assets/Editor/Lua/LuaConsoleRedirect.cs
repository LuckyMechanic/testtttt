
using System.Net.Mime;
using System;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Text.RegularExpressions;
using UnityEngine;

public class LuaConsoleRedirect
{
    private static int s_InstanceID = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/ThirdParty/XLua/Gen/UnityEngine_DebugWrap.cs").GetInstanceID();
    private static int s_Line = 295;
    [OnOpenAssetAttribute(0)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        if (!EditorWindow.focusedWindow.titleContent.text.Equals("Console"))//只对控制台的开启进行重定向
            return false;
        if (instanceID != s_InstanceID || line != s_Line)
            return false;
        // 获取控制台信息
        var consoleWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ConsoleWindow");
        var fieldInfo = consoleWindowType.GetField("ms_ConsoleWindow", BindingFlags.Static | BindingFlags.NonPublic);
        var consoleWindowInstance = fieldInfo.GetValue(null);
        if (consoleWindowInstance == null)
            return false;
        if ((object)EditorWindow.focusedWindow != consoleWindowInstance)
            return false;
        fieldInfo = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
        string activeText = fieldInfo.GetValue(consoleWindowInstance).ToString();

        // 匹配Lua文件信息
        Regex reg = new Regex(@"<color=#BE81F7>\[(\S+):(\d+)\]</color>");   //日志打印规则
        Match match = reg.Match(activeText);
        if (match.Groups.Count != 3)
        {
            return false;
        }
        var luaFileName = match.Groups[1].Value;
        var luaFileLine = int.Parse(match.Groups[2].Value);
        var luaFileInfo = FileUtil.Instance.GetChildFile(GameConst.LUA_FILE_ROOT, luaFileName);
        if (luaFileInfo == null)
        {
            return false;
        }
        var assetPath = luaFileInfo.FullName.Replace("\\", "/").Replace(Application.dataPath, "Assets");

        AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath), luaFileLine);
        return true;
    }
}
