using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

public class GenerateScriptableObjects : MonoBehaviour
{
   private static void Process (Object asset) {
      AssetDatabase.CreateAsset(asset, "Assets/" + asset.GetType().ToString() + ".asset");
      AssetDatabase.SaveAssets();

      EditorUtility.FocusProjectWindow();

      Selection.activeObject = asset;
   }

   [MenuItem("Assets/Create/Create CombinationData Object")]
   public static void Create_CombinationData () {
      CombinationData asset = ScriptableObject.CreateInstance<CombinationData>();
      Process(asset);
   }

   [MenuItem("Assets/Create/Create ComboDataList Object")]
   public static void Create_ComboDataList () {
      CombinationDataList asset = ScriptableObject.CreateInstance<CombinationDataList>();
      Process(asset);
   }

   [MenuItem("Assets/Create/Create NPCQuestData Object")]
   public static void Create_NPCQuestData () {
      NPCQuestData asset = ScriptableObject.CreateInstance<NPCQuestData>();
      Process(asset);
   }

   [MenuItem("Assets/Create/Create NPCData Object")]
   public static void Create_NPCData () {
      NPCData asset = ScriptableObject.CreateInstance<NPCData>();
      Process(asset);
   }

   [MenuItem("Assets/Create/Create HuntQuest Object")]
   public static void Create_HuntQuest () {
      HuntQuest asset = ScriptableObject.CreateInstance<HuntQuest>();
      Process(asset);
   }

   [MenuItem("Assets/Create/Create DeliverQuest Object")]
   public static void Create_DeliverQuest () {
      DeliverQuest asset = ScriptableObject.CreateInstance<DeliverQuest>();
      Process(asset);
   }

   [MenuItem("Assets/Create/Create InteractQuest Object")]
   public static void Create_InteractQuest () {
      InteractQuest asset = ScriptableObject.CreateInstance<InteractQuest>();
      Process(asset);
   }
}

#endif