
using System.IO;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UIEditor
{
    /// <summary>
    /// UI 代码所在目录
    /// </summary>
    static string UI_Script_DIR = Path.Combine(GameConst.LUA_FILE_ROOT, "./ui");
    /// <summary>
    /// UI AB包名
    /// </summary>
    static string UI_ASSETBUNDLE_NAME = "ui";
    /// <summary>
    /// UI 生成代码模板
    /// </summary>
    static string CODE_TEMPLATE_FILEPATH = "";

    [MenuItem("Assets/Generate UI Script", true)]
    static bool ValidateGenerateUIScript()
    {
        return GetSelectUIPrefabs().Length > 0;
    }
    [MenuItem("Assets/Generate UI Script")]
    static void GenerateUIScript()
    {
        foreach (var item in GetSelectUIPrefabs())
        {
            Debug.Log(item.name + " " + item);
        }
    }

    static GameObject[] GetSelectUIPrefabs()
    {
        List<GameObject> goList = new List<GameObject>();
        string[] guids = Selection.assetGUIDs;
        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);//通过GUID获取路径
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (uiPrefab != null && AssetDatabase.GetImplicitAssetBundleName(assetPath) == UI_ASSETBUNDLE_NAME)
            {
                goList.Add(uiPrefab);
            }
        }
        return goList.ToArray();
    }
}
