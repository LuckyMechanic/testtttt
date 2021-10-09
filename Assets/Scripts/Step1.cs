using System.Net.Mime;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;
using UnityEngine.UI;

[LuaCallCSharp]
public class Step1 : MonoBehaviour
{

    public static byte[] buff
    {
        get
        {
            return _buff;
        }
        set
        {
            foreach (var item in value)
            {
                UnityEngine.Debug.Log(item);
            }
            _buff = value;
        }
    }
    private static byte[] _buff = new byte[] { 1, 43, 24 };
    public static byte[] pbBuff
    {
        get
        {
            UnityEngine.Debug.Log(Resources.Load<TextAsset>("hello_pb").bytes);
            return Resources.Load<TextAsset>("hello_pb").bytes;
        }
    }

    // Start is called before the first frame update
    void Start()
    {

        ThreadUtil.Instance.MainThreadSynContext = System.Threading.SynchronizationContext.Current;

        var t = new System.Threading.Thread((obj) =>
        {
            ThreadUtil.Instance.PostMainThreadAction<string>(cll, "我是线程传递的");
        });
        t.Start();
        // 实例化调用一下
        // var mgr = LuaEnvManager.Instance;

        // Camera camera = gameObject.GetComponent<Camera>();
        // camera.clearFlags = CameraClearFlags.Depth;
        // camera.cullingMask = LayerMask.NameToLayer("UI");
        // camera.orthographic = true;
        // camera.s

        // CanvasScaler canvas = gameObject.GetComponent<CanvasScaler>();
        // canvas.uiScaleMode = ScaleMode.ScaleToFit;
        // canvas.uiScaleMode = ScaleMode.StretchToFill
        // canvas.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize
        // canvas.referenceResolution =
        // canvas.renderMode =
        // GraphicRaycaster graphicRaycaster = gameObject.GetComponent<GraphicRaycaster>()

        // transform.layer
        // transform.GetComponent<RectTransform>()
    }

    private void cll(string obj)
    {
        UnityEngine.Debug.Log(gameObject.name + " " + obj);
    }


    // Update is called once per frame
    void Update()
    {
        // LuaEnvManager.Instance.LuaEnv.GC();
    }

    private void OnDestroy()
    {
        // LuaEnvManager.Instance.LuaEnv.Dispose();
    }


}
