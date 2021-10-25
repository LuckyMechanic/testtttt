﻿
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
    static string UI_SCRIPT_DIR = Path.Combine(GameConst.LUA_FILE_ROOT, "./ui");
    /// <summary>
    /// UI AB包名
    /// </summary>
    static string UI_ASSETBUNDLE_NAME = "ui";
    /// <summary>
    /// UIControl Tag
    /// </summary>
    static string UICONTROL_TAG = "UIControl";

    [MenuItem("Assets/Generate UI Script", true)]
    static bool ValidateGenerateUIScript()
    {
        return GetSelectUIPrefabs().Length > 0;
    }
    [MenuItem("Assets/Generate UI Script")]
    static void GenerateUIScript()
    {
        foreach (var uiPrefab in GetSelectUIPrefabs())
        {
            Dictionary<string, Transform> dic = new Dictionary<string, Transform>();
            TryGetChildNodeInfo(uiPrefab.transform, ref dic);
            List<string> codeList = new List<string>();
            foreach (var item in dic)
            {
                if (item.Value.CompareTag(UICONTROL_TAG))
                {
                    codeList.Add(string.Format("\t{0}", CodeTemplate.GenerateCode("code_template_uiControl", item.Value.name, item.Key)));
                }
            }
            string uiControlCodeText = string.Join("\n", codeList);

            string filePath = new FileInfo(string.Format("{0}/{1}.lua.txt", UI_SCRIPT_DIR, uiPrefab.name)).FullName;
            string codeContent = "";
            if (File.Exists(filePath))
            {
                codeContent = File.ReadAllText(filePath);
            }
            codeContent = CodeTemplate.GenerateEditorCode(codeContent, "code_template_ui", uiPrefab.name.ToUpper(), uiPrefab.name, uiControlCodeText);
            File.WriteAllText(filePath, codeContent);
            Debug.LogFormat("[{0}]UI代码生成成功 >>> {1}", uiPrefab.name, filePath);
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
    /// <summary>
    /// 遍历获得所有子物体信息
    /// </summary>
    static void TryGetChildNodeInfo(Transform node, ref Dictionary<string, Transform> infoDic, string root = "")
    {
        for (int i = 0; i < node.childCount; i++)
        {
            var child = node.GetChild(i);
            var path = root + "/" + child.name;
            if (child.childCount == 0)
            {
                infoDic.Add(path, child);
            }
            else
            {
                TryGetChildNodeInfo(child, ref infoDic, path);
            }
        }

    }
}
