using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class MonsterToolManager : MonoBehaviour {
   #region Public Variables

   // Reference to the tool scene
   public MonsterDataScene monsterToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "MonsterStats";

   #endregion

   private void Awake () {
      loadAllDataFiles();
   }

   public void loadAllDataFiles () {
      monsterDataList = new Dictionary<string, BattlerData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<BattlerData> battlerList = DB_Main.getLandMonsterXML();

         // Iterate over the files
         for (int i = 0; i < battlerList.Count; i++) {
            // Read and deserialize the file
            BattlerData monsterData = battlerList[i];

            // Save the Monster data in the memory cache
            monsterDataList.Add(monsterData.enemyName, monsterData);
         }
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            monsterToolScreen.updatePanelWithBattlerData(monsterDataList);
         });
      });
   }

   public void deleteMonsterDataFile (BattlerData data) {
      // Build the file name
      string fileName = data.enemyName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.deleteFile(path);
   }

   public bool ifExists (string nameID) {
      return monsterDataList.ContainsKey(nameID);
   }
   
   public void saveDataToFile (BattlerData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateMonsterXML(data);
      });
   }

   public void duplicateData (BattlerData data) {
      string directoryPath = Path.Combine(Application.dataPath, "Data", FOLDER_PATH);
      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      data.enemyName += "_copy";
      string fileName = data.enemyName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data", FOLDER_PATH, fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   #region Private Variables

   private Dictionary<string, BattlerData> monsterDataList = new Dictionary<string, BattlerData>();

   #endregion
}
