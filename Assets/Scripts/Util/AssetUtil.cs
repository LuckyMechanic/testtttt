using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public enum AssetLoadType
{
    AssetBundle = 1,
    Resources = 2,
    EditorAssetBundle = 3,
}

public class AssetLoadInfo
{
    public string Key;
    public UnityEngine.Object Asset;
    public AssetLoadType Type;
    public int Index = 1;   //使用计次
}

public class AssetUtil : Singleton<AssetUtil>
{
    // 已解密完毕的AB包
    private Dictionary<string, byte[]> _bundleBytesMap = new Dictionary<string, byte[]>();
    // 已加载的AB包
    private Dictionary<string, AssetBundle> _bundleMap = new Dictionary<string, AssetBundle>();
    // 已加载的资源信息 用于记录引用卸载AB包
    private Dictionary<string, List<AssetLoadInfo>> _loadInfoMap = new Dictionary<string, List<AssetLoadInfo>>();

    // 资源依赖文件Json对象
    private JObject _relyJObject;
    // 资源版本文件 主要用于查找资源文件名
    private VModel _vModel;

    /// <summary>
    /// 开发环境使用 记录所有ab包资源路径
    /// </summary>
    /// <returns></returns>
    private Dictionary<string, Dictionary<string, string>> _abAssetFileMap = new Dictionary<string, Dictionary<string, string>>();

    public void Load()
    {
        // 开发环境缓存ab包资源路径
#if UNITY_EDITOR
        if (GameConst.PRO_ENV == ENV_TYPE.DEV)
        {
            foreach (var abName in UnityEditor.AssetDatabase.GetAllAssetBundleNames())
            {
                _abAssetFileMap.Add(abName, new Dictionary<string, string>());
                foreach (var assetFilePath in UnityEditor.AssetDatabase.GetAssetPathsFromAssetBundle(abName))
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetFilePath);
                    string dirPath = Path.GetDirectoryName(assetFilePath);
                    dirPath = dirPath.Replace("Assets\\Resources\\", "");
                    dirPath = dirPath.Replace("\\", "/");
                    string path = dirPath + "/" + assetName;
                    _abAssetFileMap[abName].Add(assetName, path);
                }
            }
        }
#endif
    }
    /// <summary>
    /// 获取开发环境缓存ab包资源路径
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public string getABAssetFilePath(string abName, string assetName)
    {
        if (!_abAssetFileMap.ContainsKey(abName))
        {
            return null;
        }
        var map = _abAssetFileMap[abName];
        if (!map.ContainsKey(assetName))
        {
            return null;
        }
        var path = map[assetName];
        return path;
    }


    /// <summary>
    /// 查找资源文件字节集
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public byte[] getAssetFileBytes(string name)
    {
        // 从本地资源文件读取
        string filePath = Path.Combine(GameConst.ASSET_ROOT, name);
        if (File.Exists(filePath))
        {
            return FileUtil.Instance.ReadBytes(filePath);
        }
        // 从StreamingAssetsPath调用
        WWW www = new WWW(Path.Combine(Application.streamingAssetsPath, name));
        while (!www.isDone) { }
        if (www.bytes.Length > 0)
        {
            return www.bytes;
        }

        // 通过版本文件查找携带md5的名字 递归回调
        string fileName = Path.GetFileNameWithoutExtension(name);
        if (_vModel == null)
        {
            string json = EncryptUtil.Instance.AesDecrypt(System.Text.Encoding.UTF8.GetString(getAssetFileBytes("Version")));
            _vModel = JsonConvert.DeserializeObject<VModel>(json);
        }
        foreach (var asset in _vModel.Assets)
        {
            if (asset.name.IndexOf(fileName) != -1)
            {
                return getAssetFileBytes(asset.fileName);
            }
        }

        return new byte[] { };
    }

    /// <summary>
    /// 获取资源依赖关系
    /// </summary>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public string[] getRelyBundleKeys(string key, string assetName = null)
    {
        _relyJObject = _relyJObject ?? JObject.Parse(EncryptUtil.Instance.AesDecrypt(System.Text.Encoding.UTF8.GetString(getAssetFileBytes("AssetBundleRely"))));
        List<string> bundleNameList = new List<string> { key };
        JToken jToken;
        if (_relyJObject.TryGetValue(key, out jToken))
        {
            if (assetName != null)
            {
                // 获取固定资源关联AB包信息
                JToken jArray = jToken[assetName];
                if (jArray != null)
                {
                    foreach (var ab in jArray.Values<string>())
                    {
                        bundleNameList.Add(ab);
                    }
                }
            }
            else
            {
                // 获取所有资源关联AB包信息
                foreach (var item in jToken.Children<JToken>())
                {
                    foreach (var ab in item.ToObject<JProperty>().Value.ToObject<JArray>().Values<string>())
                    {
                        if (bundleNameList.IndexOf(ab) == -1)
                        {
                            bundleNameList.Add(ab);
                        }
                    }
                }
            }
        }
        return bundleNameList.ToArray();
    }

    /// <summary>
    /// 获得解密AB包bytes，并保存到缓存
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public byte[] DecryptBundleBytes(string key)
    {
        if (_bundleBytesMap.ContainsKey(key))
        {
            return _bundleBytesMap[key];
        }
        byte[] data = EncryptUtil.Instance.AesDecrypt(getAssetFileBytes("AssetBundles/" + key));
        _bundleBytesMap.Add(key, data);
        return data;
    }

    /// <summary>
    /// 异步获得解密AB包bytes，并保存到缓存
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool DecryptBundleBytesAsync(string key, Action<byte[]> cb)
    {
        if (_bundleBytesMap.ContainsKey(key))
        {
            cb(_bundleBytesMap[key]);
            return true;
        }
        EncryptUtil.Instance.AesDecryptAsync(getAssetFileBytes("AssetBundles/" + key), (data) =>
        {
            _bundleBytesMap.Add(key, data);
            cb(data);
        });
        return true;
    }
    /// <summary>
    /// AB包加载完毕
    /// </summary>
    /// <param name="key"></param>
    /// <param name="bundle"></param>
    private void _loadBundleOver(string key, AssetBundle bundle)
    {
        Debug.LogFormat("AssetBundle加载完毕>>>{0}", key);
        _bundleMap.Add(key, bundle);
    }
    /// <summary>
    /// AB包卸载完毕
    /// </summary>
    /// <param name="key"></param>
    private void _unloadBundleOver(string key)
    {
        Debug.LogFormat("AssetBundle卸载完毕>>>{0}", key);
        _bundleMap.Remove(key);
    }
    /// <summary>
    /// 加载AB包
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public AssetBundle LoadBundle(string key)
    {
        if (_bundleMap.ContainsKey(key))
        {
            return _bundleMap[key];
        }
        byte[] data = DecryptBundleBytes(key);
        if (data == null) return null;
        AssetBundle bundle = AssetBundle.LoadFromMemory(data);

        _loadBundleOver(key, bundle);

        return bundle;
    }
    /// <summary>
    /// 异步加载AB包
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cb"></param>
    /// <returns></returns>
    public void LoadBundleAsync(string key, Action<AssetBundle> cb = null)
    {
        MonoUtil.Instance.StartCoroutine(_loadBundleAsync(key, cb));
    }
    System.Collections.IEnumerator _loadBundleAsync(string key, Action<AssetBundle> cb = null)
    {
        if (_bundleMap.ContainsKey(key))
        {
            if (cb != null) cb(_bundleMap[key]);
            yield break;
        }
        byte[] data = null;
        bool flag = DecryptBundleBytesAsync(key, (d) =>
        {
            data = d;
        });
        if (!flag)
        {
            if (cb != null) cb(null);
            yield break;
        }
        yield return new WaitUntil(() => { return data != null; });
        var assetLoadRequest = AssetBundle.LoadFromMemoryAsync(data);
        yield return assetLoadRequest;
        AssetBundle bundle = assetLoadRequest.assetBundle;

        _loadBundleOver(key, bundle);

        if (cb != null) cb(bundle);
    }
    /// <summary>
    /// 卸载AB包
    /// </summary>
    /// <param name="key"></param>
    /// <param name="unloadAllLoadedObjects"></param>
    public void UnloadBundle(string key, bool unloadAllLoadedObjects = true)
    {
        if (!_bundleMap.ContainsKey(key))
        {
            return;
        }
        _bundleMap[key].Unload(unloadAllLoadedObjects);
        _unloadBundleOver(key);
    }
    #region AssetBundle加载资源
    /// <summary>
    /// 从AB包中加载资源
    /// </summary>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    public UnityEngine.Object LoadAssetFromBundle(Type type, string key, string assetName)
    {
        // 加载AB包 包括依赖AB包
        string[] bundleKeys = getRelyBundleKeys(key, assetName);
        List<AssetBundle> bundles = new List<AssetBundle>();
        for (int i = 0; i < bundleKeys.Length; i++)
        {
            var bundle = LoadBundle(bundleKeys[i]);
            if (bundle != null)
            {
                bundles.Add(bundle);
            }
        }
        if (bundles.Count == 0) return null;
        // 加载资源
        var asset = bundles[0].LoadAsset(assetName, type);
        if (asset != null)
        {
            _loadAssetFromBundleOver(key, asset);
            foreach (var bundle in bundles)
            {
                _addLoadAssetInfo(bundle.name, asset, AssetLoadType.AssetBundle);
            }
        }
        return asset;
    }
    public void LoadAssetFromBundleAsync(Type type, string key, string assetName, Action<UnityEngine.Object> cb)
    {
        MonoUtil.Instance.StartCoroutine(_loadAssetFromBundleAsync(type, key, assetName, cb));
    }
    System.Collections.IEnumerator _loadAssetFromBundleAsync(Type type, string key, string assetName, Action<UnityEngine.Object> cb)
    {
        // 加载AB包 包括依赖AB包
        string[] bundleKeys = getRelyBundleKeys(key, assetName);
        List<AssetBundle> bundles = new List<AssetBundle>();
        int sum = 0;
        for (int i = 0; i < bundleKeys.Length; i++)
        {
            LoadBundleAsync(bundleKeys[i], (bundle) =>
            {
                sum += 1;
                if (bundle != null)
                {
                    bundles.Add(bundle);
                }
            });
        }
        yield return new WaitUntil(() => { return bundleKeys.Length <= sum; });
        if (bundles.Count == 0) yield break;
        var assetRequest = bundles[0].LoadAssetAsync(assetName, type);
        yield return assetRequest;
        var asset = assetRequest.asset;
        if (asset != null)
        {
            _loadAssetFromBundleOver(key, asset);
            foreach (var bundle in bundles)
            {
                _addLoadAssetInfo(bundle.name, asset, AssetLoadType.AssetBundle);
            }
        }
        cb(asset);
    }
    public UnityEngine.Object[] LoadAllAssetFromBundle(Type type, string key)
    {
        List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        var bundle = LoadBundle(key);
        var assetNames = bundle.GetAllAssetNames();
        foreach (var assetName in assetNames)
        {
            assets.Add(LoadAssetFromBundle(type, key, assetName));
        }
        return assets.ToArray();
    }
    public void LoadAllAssetFromBundleAsync(Type type, string key, Action<UnityEngine.Object[]> cb)
    {
        List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        LoadBundleAsync(key, (bundle) =>
        {
            int sum = 0;
            var assetNames = bundle.GetAllAssetNames();
            foreach (var assetName in assetNames)
            {
                LoadAssetFromBundleAsync(type, key, assetName, (asset) =>
                {
                    sum += 1;
                    assets.Add(asset);
                    if (assets.Count >= sum)
                    {
                        cb(assets.ToArray());
                    }
                });
            }
        });
    }
    private void _loadAssetFromBundleOver(string key, UnityEngine.Object asset)
    {
        Debug.LogFormat("从AssetBundle加载资源 - key：【{0}】 assetName：【{1}】", key, asset.name);
    }
    #endregion

    #region 模拟AssetBundle加载资源
    /// <summary>
    /// 开发环境时使用的模拟AB包加载资源
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public UnityEngine.Object LoadAssetFromEditorBundle(Type type, string key, string assetName)
    {
        var path = getABAssetFilePath(key, assetName);
        if (path == null)
        {
            return null;
        }
        var asset = Resources.Load(path, type);
        if (asset != null)
        {
            _loadAssetFromEditorBundleOver(key, asset);
        }
        return asset;
    }
    public void LoadAssetFromEditorBundleAsync(Type type, string key, string assetName, Action<UnityEngine.Object> cb)
    {
        var path = getABAssetFilePath(key, assetName);
        if (path == null)
        {
            cb(null);
            return;
        }
        MonoUtil.Instance.StartCoroutine(_loadAssetFromResourcesAsync(type, path, (asset) =>
        {
            if (asset != null)
            {
                _loadAssetFromEditorBundleOver(key, asset);
            }
            cb(asset);
        }));
    }
    public UnityEngine.Object[] LoadAllAssetFromEditorBundle(Type type, string key)
    {
        List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        Dictionary<string, string> dic;
        if (_abAssetFileMap.TryGetValue(key, out dic))
        {
            foreach (var assetName in dic.Keys)
            {
                assets.Add(LoadAssetFromEditorBundle(type, key, assetName));
            }
        }
        return assets.ToArray();
    }
    public void LoadAllAssetFromEditorBundleAsync(Type type, string key, Action<UnityEngine.Object[]> cb)
    {
        List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        Dictionary<string, string> dic;
        if (_abAssetFileMap.TryGetValue(key, out dic))
        {
            int sum = 0;
            foreach (var assetName in dic.Keys)
            {
                LoadAssetFromEditorBundleAsync(type, key, assetName, (asset) =>
                {
                    sum += 1;
                    assets.Add(asset);
                    if (assets.Count >= sum)
                    {
                        cb(assets.ToArray());
                    }
                });
            }
        }
    }
    private void _loadAssetFromEditorBundleOver(string key, UnityEngine.Object asset)
    {
        Debug.LogFormat("从模拟AssetBundle加载资源 - key：【{0}】 assetName：【{1}】", key, asset.name);

        _addLoadAssetInfo(key, asset, AssetLoadType.EditorAssetBundle);
    }
    #endregion

    #region 本地资源Resources加载
    /// <summary>
    /// 从本地资源Resources加载
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public UnityEngine.Object LoadAssetFromResources(Type type, string key, string assetName)
    {
        string path = key + "/" + assetName;
        var asset = Resources.Load(path, type);
        if (asset != null)
        {
            _loadAssetFromResourcesOver(key, asset);
        }
        return asset;
    }
    public void LoadAssetFromResourcesAsync(Type type, string key, string assetName, Action<UnityEngine.Object> cb)
    {
        string path = key + "/" + assetName;
        MonoUtil.Instance.StartCoroutine(_loadAssetFromResourcesAsync(type, path, (asset) =>
        {
            if (asset != null)
            {
                _loadAssetFromResourcesOver(key, asset);
            }
            cb(asset);
        }));
    }
    System.Collections.IEnumerator _loadAssetFromResourcesAsync(Type type, string path, Action<UnityEngine.Object> cb)
    {
        ResourceRequest request = Resources.LoadAsync(path, type);
        yield return request;
        var asset = request.asset;
        cb(asset);
    }
    public UnityEngine.Object[] LoadAllAssetFromResources(Type type, string key)
    {
        string path = key;
        var assets = Resources.LoadAll(path, type);
        foreach (var asset in assets)
        {
            _loadAssetFromResourcesOver(key, asset);
        }
        return assets;
    }
    // 事实上Resources并没有异步加载方式，这里凑格式多写一个，本质上还是异步加载
    public void LoadAllAssetFromResourcesAsync(Type type, string key, Action<UnityEngine.Object[]> cb)
    {
        cb(LoadAllAssetFromResources(type, key));
    }
    private void _loadAssetFromResourcesOver(string key, UnityEngine.Object asset)
    {
        Debug.LogFormat("从Resources加载资源 - key：【{0}】 assetName：【{1}】", key, asset.name);
        _addLoadAssetInfo(key, asset, AssetLoadType.Resources);
    }
    #endregion


    private void _addLoadAssetInfo(string key, UnityEngine.Object asset, AssetLoadType Type)
    {
        var loadInfo = new AssetLoadInfo();
        loadInfo.Key = key;
        loadInfo.Asset = asset;
        loadInfo.Type = Type;
        loadInfo.Index = 1;

        if (_loadInfoMap.ContainsKey(key))
        {

            AssetLoadInfo info = null;
            var arr = _loadInfoMap[key];
            for (int i = 0; i < arr.Count; i++)
            {
                if (arr[i].Asset.name == asset.name)
                {
                    info = arr[i];
                    break;
                }
            }
            if (info == null)
            {
                arr.Add(loadInfo);
            }
            else
            {
                info.Index++;
            }
        }
        else
        {
            _loadInfoMap.Add(key, new List<AssetLoadInfo>() { loadInfo });
        }
    }

    /// <summary>
    /// 资源加载汇总
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public UnityEngine.Object LoadAsset(Type type, string key, string assetName)
    {
        UnityEngine.Object asset = null;
        if (GameConst.PRO_ENV == ENV_TYPE.MASTER)
        {
            // 正式环境通过AB包加载
            asset = LoadAssetFromBundle(type, key, assetName);
        }
        else
        {
            // 开发环境模拟AB包本地加载
            asset = LoadAssetFromEditorBundle(type, key, assetName);
        }
        if (asset == null)
        {
            // AB包方式无法加载，则尝试通过本地Resources加载
            asset = LoadAssetFromResources(type, key, assetName);
        }
        return asset;
    }
    /// <summary>
    /// 资源异步加载汇总
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    /// <param name="cb"></param>
    public void LoadAssetAsync(Type type, string key, string assetName, Action<UnityEngine.Object> cb)
    {
        if (GameConst.PRO_ENV == ENV_TYPE.MASTER)
        {
            LoadAssetFromBundleAsync(type, key, assetName, (asset) =>
            {
                if (asset == null)
                {
                    // AB包方式无法加载，则尝试通过本地Resources加载
                    LoadAssetFromResourcesAsync(type, key, assetName, cb);
                }
                else
                {
                    cb(asset);
                }
            });
        }
        else
        {
            LoadAssetFromEditorBundleAsync(type, key, assetName, (asset) =>
            {
                if (asset == null)
                {
                    // AB包方式无法加载，则尝试通过本地Resources加载
                    LoadAssetFromResourcesAsync(type, key, assetName, cb);
                }
                else
                {
                    cb(asset);
                }
            });
        }
    }

    /// <summary>
    /// 所有资源加载汇总
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    /// <returns></returns>
    public UnityEngine.Object[] LoadAllAsset(Type type, string key)
    {
        UnityEngine.Object[] assets = new UnityEngine.Object[0];
        if (GameConst.PRO_ENV == ENV_TYPE.MASTER)
        {
            // 正式环境通过AB包加载
            assets = LoadAllAssetFromBundle(type, key);
        }
        else
        {
            // 开发环境模拟AB包本地加载
            assets = LoadAllAssetFromEditorBundle(type, key);
        }
        if (assets.Length == 0)
        {
            // AB包方式加载无资源，则尝试通过本地Resources加载
            assets = LoadAllAssetFromResources(type, key);
        }
        return assets;
    }
    /// <summary>
    /// 所有资源异步加载汇总
    /// </summary>
    /// <param name="type"></param>
    /// <param name="key"></param>
    /// <param name="assetName"></param>
    /// <param name="cb"></param>
    public void LoadAllAssetAsync(Type type, string key, Action<UnityEngine.Object[]> cb)
    {
        if (GameConst.PRO_ENV == ENV_TYPE.MASTER)
        {
            LoadAllAssetFromBundleAsync(type, key, (assets) =>
            {
                if (assets.Length == 0)
                {
                    // AB包方式加载无资源，则尝试通过本地Resources加载
                    LoadAllAssetFromResourcesAsync(type, key, cb);
                }
                else
                {
                    cb(assets);
                }
            });
        }
        else
        {
            LoadAllAssetFromEditorBundleAsync(type, key, (assets) =>
            {
                if (assets.Length == 0)
                {
                    // AB包方式加载无资源，则尝试通过本地Resources加载
                    LoadAllAssetFromResourcesAsync(type, key, cb);
                }
                else
                {
                    cb(assets);
                }
            });
        }
    }

    public void UnloadAsset(UnityEngine.Object asset)
    {
        foreach (var key in _loadInfoMap.Keys)
        {
            _unloadAsset(key, asset);
        }
    }

    private void _unloadAsset(string key, UnityEngine.Object asset)
    {
        if (!_loadInfoMap.ContainsKey(key)) return;
        var list = _loadInfoMap[key];
        AssetLoadInfo info = null;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Asset == asset)
            {
                info = list[i];
                break;
            }
        }
        if (info == null) return;
        info.Index--;
        if (info.Index == 0)
        {
            list.Remove(info);
        }
        if (info.Type == AssetLoadType.AssetBundle)
        {
            if (list.Count == 0)
            {
                UnloadBundle(info.Key);
            }
        }
        else if (info.Type == AssetLoadType.EditorAssetBundle)
        {
            Debug.LogFormat("从模拟AssetBundle加载资源 - key：【{0}】 assetName：【{1}】", info.Key, info.Asset.name);
            if (info.Asset.GetType() != typeof(UnityEngine.GameObject))
            {
                Resources.UnloadAsset(info.Asset);
            }
            else
            {
                // 预制体暂时无法强行释放，没占多少内存，先不处理
            }
        }
        else if (info.Type == AssetLoadType.Resources)
        {
            Debug.LogFormat("从Resources卸载资源 - key：【{0}】 assetName：【{1}】", info.Key, info.Asset.name);
            if (info.Asset.GetType() != typeof(UnityEngine.GameObject))
            {
                Resources.UnloadAsset(info.Asset);
            }
            else
            {
                // 预制体暂时无法强行释放，没占多少内存，先不处理
            }
        }
        Resources.UnloadUnusedAssets();
    }

}