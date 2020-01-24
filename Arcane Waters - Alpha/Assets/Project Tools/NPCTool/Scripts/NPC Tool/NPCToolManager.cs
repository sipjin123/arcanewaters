﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Linq;
using UnityEngine.SceneManagement;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class NPCToolManager : MonoBehaviour {

   #region Public Variables

   // Self
   public static NPCToolManager self;

   // The NPC Selection Screen
   public NPCSelectionScreen npcSelectionScreen;

   // Opens the main tool
   public Button openMainTool;

   // Holds the path of the folder
   public const string FOLDER_PATH = "NPC";

   // Holds the collection of user id that created the data entry
   public List<SQLEntryIDClass> _userIdData = new List<SQLEntryIDClass>();

   #endregion

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

   public void Awake () {
      self = this;
      openMainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   public void Start () {
      npcSelectionScreen.show();
      loadAllDataFiles();
      Invoke("loadAllDataFiles", MasterToolScene.loadDelay);
   }

   public void loadAllDataFiles () {
      _npcData = new Dictionary<int, NPCData>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getNPCXML();
         _userIdData = DB_Main.getSQLDataByID(EditorSQLManager.EditorToolType.NPC);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               NPCData npcData = Util.xmlLoad<NPCData>(newTextAsset);

               // Save the NPC data in the memory cache
               if (!_npcData.ContainsKey(npcData.npcId)) {
                  _npcData.Add(npcData.npcId, npcData);
               }
            }

            npcSelectionScreen.updatePanelWithNPCs(_npcData);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void createNewNPC(int npcId) {
      // Verify that the NPC id is free
      if (!isNPCIdFree(npcId)) {
         Debug.LogError(string.Format("A NPC with the same ID {0} already exists.", npcId.ToString()));
         return;
      }

      // Create an empty npc
      NPCData npcData = new NPCData(npcId, "", "", "", "", "", "", "", "", "NPC", Faction.Type.Neutral,
         Specialty.Type.Adventurer, true, true, -1, new List<Quest>() { },
         new List<NPCGiftData>() { }, "", "");

      // Add the data to the dictionary
      _npcData.Add(npcData.npcId, npcData);

      // Save the data to the file
      saveNPCDataToFile(npcData);

      // Update the list of NPCs
      npcSelectionScreen.updatePanelWithNPCs(_npcData);
   }

   public bool isNPCIdFree (int npcId) {
      bool free = true;
      foreach (NPCData npcData in _npcData.Values) {
         if (npcData.npcId == npcId) {
            free = false;
            break;
         }
      }
      return free;
   }

   public NPCData getNPCData(int npcId) {
      if (_npcData.ContainsKey(npcId)) {
         return _npcData[npcId];
      } else {
         Debug.LogError(string.Format("The NPC {0} does not exist.", npcId));
         return null;
      }
   }

   public void overWriteNPC (NPCData data, int toDeleteID) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateNPCXML(longString, data.npcId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            deleteNPCDataFile(new NPCData { npcId = toDeleteID });
         });
      });
   }

   public void deleteNPCDataFile (NPCData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteNPCXML(data.npcId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void saveNPCDataToFile (NPCData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateNPCXML(longString, data.npcId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void duplicateFile (NPCData data) {
      data.npcId = 0;
      data.name = "Undefined NPC";
      data.iconPath = "";
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateNPCXML(longString, data.npcId);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   #region Private Variables

   // The cached NPC data
   private Dictionary<int, NPCData> _npcData = new Dictionary<int, NPCData>();

   #endregion
}
