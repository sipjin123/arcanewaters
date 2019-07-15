
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GenerateScriptableObjects : MonoBehaviour
{
    private static void Process(Object asset)
    {
        AssetDatabase.CreateAsset(asset, "Assets/" + asset.GetType().ToString() + ".asset");
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();

        Selection.activeObject = asset;
    }

    [MenuItem("Assets/Create/Create CombinationData Object")]
    public static void Create_CombinationData()
    {
        CombinationData asset = ScriptableObject.CreateInstance<CombinationData>();
        Process(asset);
    }
    [MenuItem("Assets/Create/Create ComboDataList Object")]
    public static void Create_ComboDataList()
    {
        CombinationDataList asset = ScriptableObject.CreateInstance<CombinationDataList>();
        Process(asset);
    }
    
}
