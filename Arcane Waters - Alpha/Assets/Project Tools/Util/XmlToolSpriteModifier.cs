using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class XmlToolSpriteModifier : MonoBehaviour {
   #region Public Variables

   // The sprite paths
   public static string OLD_PATH = "Assets/Sprites/";
   public static string NEW_PATH = "Sprites/";

   // Reference to the tool manager
   public XmlDataToolManager xmlToolManager;

   #endregion

   private void Start () {
      xmlToolManager = GetComponent<XmlDataToolManager>();
   }

   public void addXmlData<T>(List<T> objectList) {
      D.editorLog("Adding Xml Type: " + objectList[0].GetType());

      if (objectList[0].GetType() == typeof(NPCData)) {
         List<NPCData> newNpcDataList = new List<NPCData>();
         foreach (T genericClass in objectList) {
            NPCData newNpcData = (NPCData) (object) genericClass;

            string currentIconPath = newNpcData.iconPath;
            newNpcData.iconPath = currentIconPath.Replace(OLD_PATH, NEW_PATH);
            D.editorLog("Replacing path of: (" + currentIconPath + ") into (" + newNpcData.iconPath + ")");

            string currentSpritePath = newNpcData.spritePath;
            newNpcData.spritePath = currentSpritePath.Replace(OLD_PATH, NEW_PATH);
            D.editorLog("Replacing path of: (" + currentSpritePath + ") into (" + newNpcData.spritePath + ")");

            newNpcDataList.Add(newNpcData);
         }

         StartCoroutine(CO_WriteToDatabase<NPCData>(newNpcDataList));
      }
      if (objectList[0].GetType() == typeof(BattlerData)) {

      }
   }

   private IEnumerator CO_WriteToDatabase<T> (List<T> objectList) {
      yield return new WaitForSeconds(1);
      if (objectList[0].GetType() == typeof(NPCData)) {
         int totalDataToUpdate = objectList.Count;
         int dataCounter = 0;
         int breakAfter = 999;
         foreach (T genericClass in objectList) {
            if (dataCounter >= breakAfter) {
               break;
            }
            NPCData newNpcData = (NPCData) (object) genericClass;
            NPCToolManager.instance.saveNPCDataToFile(newNpcData);

            yield return new WaitForSeconds(1);

            D.editorLog("Saved Npc: " + newNpcData.npcId + " : " + newNpcData.name + " to database :: " + dataCounter + " / " + totalDataToUpdate, Color.green);
            dataCounter++;
         }
      }
   }

   private void OnGUI () {
      if (GUILayout.Button("Update Tool: " + xmlToolManager.editorToolType)) {
         switch (xmlToolManager.editorToolType) {
            case EditorSQLManager.EditorToolType.NPC:
               addXmlData<NPCData>(GetComponent<NPCToolManager>().getNpcDataList());
               break;
         }
      }
   }

   #region Private Variables

   #endregion
}
