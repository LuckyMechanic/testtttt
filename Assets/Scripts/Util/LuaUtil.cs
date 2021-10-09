using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

public class LuaUtil : Singleton<LuaUtil>
{
    public bool IsNull(UnityEngine.Object obj)
    {
        return obj == null;
    }
    public void LoadScene(string name, Action<AsyncOperation> cb = null, Action<AsyncOperation, float> loadingFunc = null, bool allowSceneActivation = true, LoadSceneMode mode = LoadSceneMode.Single)
    {
        MonoUtil.Instance.StartCoroutine(_loadScene(name, cb, loadingFunc, allowSceneActivation, mode));
    }
    IEnumerator _loadScene(string name, Action<AsyncOperation> cb = null, Action<AsyncOperation, float> loadingFunc = null, bool allowSceneActivation = true, LoadSceneMode mode = LoadSceneMode.Single)
    {
        yield return null;
        var ao = SceneManager.LoadSceneAsync(name, mode);
        ao.allowSceneActivation = false;
        while (!ao.isDone)
        {
            float progress = Mathf.Clamp01(ao.progress / 0.9f);

            if (loadingFunc != null) loadingFunc(ao, progress);

            if (Mathf.Approximately(progress, 1f))
            {
                ao.allowSceneActivation = allowSceneActivation;
                if (cb != null) cb(ao);
                break;
            }

            yield return null;
        }

    }
}