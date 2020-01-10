using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System.Linq;
using System;

public class SeaMonsterToolManager : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool scene
   public SeaMonsterDataScene monsterToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "SeaMonsterStats";

   #endregion

   private void Start () {
      loadAllDataFiles();
   }

   public void loadAllDataFiles () {
      XmlLoadingPanel.self.startLoading();
      monsterDataList = new Dictionary<string, SeaMonsterEntityData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getSeaMonsterXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               SeaMonsterEntityData newSeaData = Util.xmlLoad<SeaMonsterEntityData>(newTextAsset);
               SeaMonsterEntity.Type typeID = (SeaMonsterEntity.Type) newSeaData.seaMonsterType;

               // Save the Monster data in the memory cache
               if (!monsterDataList.ContainsKey(newSeaData.monsterName)) {
                  monsterDataList.Add(newSeaData.monsterName, newSeaData);
               }
            }
            monsterToolScreen.updatePanelWithData(monsterDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void deleteMonsterDataFile (SeaMonsterEntityData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteSeamonsterXML((int) data.seaMonsterType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public bool ifExists (string nameID) {
      return monsterDataList.ContainsKey(nameID);
   }

   public void saveDataToFile (SeaMonsterEntityData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateSeaMonsterXML(longString, (int) data.seaMonsterType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void duplicateFile (SeaMonsterEntityData data) {
      int uniqueID = 0;
      foreach (SeaMonsterEntity.Type seaMonsterType in Enum.GetValues(typeof(SeaMonsterEntity.Type))) {
         if (!monsterDataList.Values.ToList().Exists(_ => _.seaMonsterType == seaMonsterType) && seaMonsterType != SeaMonsterEntity.Type.None) {
            uniqueID = (int) seaMonsterType;
            break;
         } 
      }
      if (uniqueID == 0) {
         Debug.LogWarning("All ID is accounted for, please edit data instead");
         return;
      }

      data.seaMonsterType = (SeaMonsterEntity.Type) uniqueID;
      data.monsterName += "_copy";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateSeaMonsterXML(longString, uniqueID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   #region Private Variables

   // Holds the list of sea monster data
   private Dictionary<string, SeaMonsterEntityData> monsterDataList = new Dictionary<string, SeaMonsterEntityData>();

   #endregion
}
