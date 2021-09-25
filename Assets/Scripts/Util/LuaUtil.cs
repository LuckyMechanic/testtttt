using System.Collections;
using System;
using UnityEngine.SceneManagement;
using UnityEngine;

public class LuaUtil : Singleton<LuaUtil>
{
    public void LoadScene(string name, Action<AsyncOperation> cb = null, Action<AsyncOperation, float> loadingFunc = null, bool allowSceneActivation = true, LoadSceneMode model = LoadSceneMode.Single)
    {
        MonoUtil.Instance.StartCoroutine(_loadScene(name, cb, loadingFunc, allowSceneActivation, model));
    }
    IEnumerator _loadScene(string name, Action<AsyncOperation> cb = null, Action<AsyncOperation, float> loadingFunc = null, bool allowSceneActivation = true, LoadSceneMode model = LoadSceneMode.Single)
    {
        var ao = SceneManager.LoadSceneAsync(name, model);
        ao.allowSceneActivation = false;
        while (!ao.isDone)
        {
            float progress = Mathf.Clamp01(ao.progress / 0.9f);

            if (loadingFunc != null) loadingFunc(ao, progress);
            Debug.Log("Loading progress:" + (ao.progress) + "%");


            yield return null;
        }
        Debug.Log("Almost loaded!");
        ao.allowSceneActivation = allowSceneActivation;
        if (cb != null) cb(ao);
    }
}