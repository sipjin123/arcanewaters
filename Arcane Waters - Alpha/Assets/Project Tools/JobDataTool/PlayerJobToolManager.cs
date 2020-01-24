﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using static ClassManager;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

public class PlayerJobToolManager : MonoBehaviour {
   #region Public Variables

   // Holds the main scene for the player job
   public PlayerJobScene jobSceneScene;

   // Holds the path of the folder
   public const string FOLDER_PATH = "PlayerJob";

   // Self
   public static PlayerJobToolManager self;

   // Holds the collection of user id that created the data entry
   public List<SQLEntryIDClass> _userIdData = new List<SQLEntryIDClass>();

   #endregion

   private void Awake () {
      self = this;
   }

   public bool didUserCreateData (int entryID) {
      SQLEntryIDClass sqlEntry = _userIdData.Find(_ => _.dataID == entryID);
      if (sqlEntry != null) {
         if (sqlEntry.ownerID == MasterToolAccountManager.self.currentAccountID) {
            return true;
         }
      } else {
         Debug.LogWarning("Entry does not exist: " + entryID);
      }

      return false;
   }

   private void Start () {
      Invoke("loadXMLData", MasterToolScene.loadDelay);
   }

   public void saveXMLData (PlayerJobData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int) data.type, PlayerStatType.Job);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void deleteDataFile (PlayerJobData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deletePlayerClassXML(PlayerStatType.Job, (int) data.type);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void duplicateXMLData (PlayerJobData data) {
      data.type = 0;
      data.jobName = "Undefined Job";
      data.jobIconPath = "";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updatePlayerClassXML(longString, (int) data.type, PlayerStatType.Job);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXMLData();
         });
      });
   }

   public void loadXMLData () {
      _playerJobData = new Dictionary<Jobs.Type, PlayerJobData>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getPlayerClassXML(PlayerStatType.Job);
         _userIdData = DB_Main.getSQLDataByID(EditorSQLManager.EditorToolType.PlayerJob);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               PlayerJobData jobData = Util.xmlLoad<PlayerJobData>(newTextAsset);

               // Save the data in the memory cache
               if (!_playerJobData.ContainsKey(jobData.type)) {
                  _playerJobData.Add(jobData.type, jobData);
               }
            }

            jobSceneScene.loadPlayerJobData(_playerJobData);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   #region Private Variables

   // Holds the list of player job data
   private Dictionary<Jobs.Type, PlayerJobData> _playerJobData = new Dictionary<Jobs.Type, PlayerJobData>();

   #endregion
}
