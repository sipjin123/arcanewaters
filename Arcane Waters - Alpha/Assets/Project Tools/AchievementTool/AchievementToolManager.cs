using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class AchievementToolManager : XmlDataToolManager {
   #region Public Variables

   // Holds the main scene for the data templates
   public AchievementToolScene achievementToolScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "Achievement";

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void saveXMLData (AchievementData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateAchievementXML(longString, data.achievementName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void overwriteData (AchievementData data, string nameToDelete) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateAchievementXML(longString, data.achievementName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            deleteAchievementDataFile(new AchievementData { achievementName = nameToDelete });
         });
      });
   }

   public void deleteAchievementDataFile (AchievementData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteAchievementXML(data.achievementName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (AchievementData data) {
      data.achievementName = MasterToolScene.UNDEFINED;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateAchievementXML(longString, data.achievementName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData ();
         });
      });
   }

   public void loadXMLData () {
      _achievementDataList = new Dictionary<string, AchievementData>();

      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getAchievementXML();
         userNameData = DB_Main.getSQLDataByName(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               AchievementData achievementData = Util.xmlLoad<AchievementData>(newTextAsset);

               // Save the achievement data in the memory cache
               if (_achievementDataList.ContainsKey(achievementData.achievementName)) {
                  Debug.LogWarning("Duplicated ID: " + achievementData.achievementName + " : " + achievementData.achievementName);
               } else {
                  _achievementDataList.Add(achievementData.achievementName, achievementData);
               }
            }
            achievementToolScene.loadAchievementData(_achievementDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of achievement data
   private Dictionary<string, AchievementData> _achievementDataList = new Dictionary<string, AchievementData>();

   #endregion
}
