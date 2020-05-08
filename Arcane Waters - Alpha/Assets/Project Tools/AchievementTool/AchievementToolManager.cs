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

   public class AchievementDataPair
   {
      // The xml ID of the template
      public int xmlId;

      // The userID of the content creator
      public int creatorID;

      // Data of the achievement template
      public AchievementData achivementData;
   }

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
      EquipmentXMLManager.self.initializeDataCache();
   }

   public void saveXMLData (AchievementData data, int xmlID) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateAchievementXML(longString, data.achievementName, xmlID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
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
         DB_Main.updateAchievementXML(longString, data.achievementName, -1);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData ();
         });
      });
   }

   public void loadXMLData () {
      _achievementDataList = new List<AchievementDataPair>();

      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getAchievementXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               AchievementData achievementData = Util.xmlLoad<AchievementData>(newTextAsset);

               // Save the achievement data in the memory cache
               AchievementDataPair newDataPair = new AchievementDataPair { 
                  achivementData = achievementData,
                  creatorID = xmlPair.xmlOwnerId,
                  xmlId = xmlPair.xmlId
               };
               _achievementDataList.Add(newDataPair);
            }
            achievementToolScene.loadAchievementData(_achievementDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of achievement data
   private List<AchievementDataPair> _achievementDataList = new List<AchievementDataPair>();

   #endregion
}
