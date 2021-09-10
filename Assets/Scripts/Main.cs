using UnityEngine;

public class Main
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void main()
    {
        ThreadUtil.Instance.Load();
        MonoUtil.Instance.Load();
        AssetUtil.Instance.Load();

        LuaEnvManager.Instance.Load();
    }
}