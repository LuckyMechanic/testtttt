using System;
using UnityEngine;
using XLua;
public class LuaEnvManager : Singleton<LuaEnvManager>
{
    public LuaEnv LuaEnv;

    public LuaEnvManager()
    {

    }

    public void Load()
    {
        LuaEnv = new LuaEnv();
        LuaEnv.AddBuildin("rapidjson", XLua.LuaDLL.Lua.LoadRapidJson);
        LuaEnv.AddBuildin("pb", XLua.LuaDLL.Lua.LoadPB);

        LuaEnv.DoString(@"
            local applicationDataPath = CS.UnityEngine.Application.dataPath..'/../Tool/ExtendLuaDLL/emmy_core.dll'
            CS.UnityEngine.Debug.Log('EditorLog:'..applicationDataPath)
            package.cpath = package.cpath .. ';'..applicationDataPath
            local dbg = require('emmy_core')
            dbg.tcpConnect('localhost', 9966)
        ");

        LuaEnv.AddLoader(OnLoadLuaFile);
        LuaEnv.DoString(@"require 'main'");

        MonoUtil.Instance.MonoComponent.UpdateEvent += () =>
        {
            LuaEnv.GC();
        };
        MonoUtil.Instance.MonoComponent.OnDestroyEvent += () =>
        {
            LuaEnv.Dispose();
        };
    }

    private byte[] OnLoadLuaFile(ref string filepath)
    {
        TextAsset asset = AssetUtil.Instance.LoadAsset(typeof(TextAsset), "lua", string.Format("{0}.lua", filepath)) as TextAsset;
        if (asset == null)
        {
            UnityEngine.Debug.LogError(string.Format("找不到Lua文件[{0}]", filepath));
            return new byte[] { };
        }
        return asset.bytes;
    }
}
