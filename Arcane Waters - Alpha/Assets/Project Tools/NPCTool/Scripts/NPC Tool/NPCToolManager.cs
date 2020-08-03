using UnityEngine;
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

public class NPCToolManager : XmlDataToolManager {

   #region Public Variables

   // Self
   public static NPCToolManager instance;

   // The NPC Selection Screen
   public NPCSelectionScreen npcSelectionScreen;

   // Opens the main tool
   public Button openMainTool;

   // Holds the path of the folder
   public const string FOLDER_PATH = "NPC";

   // Collection of achievements fetched from the database
   public Dictionary<int, AchievementData> achievementCollection;

   // List of quest data created in the database
   public List<QuestData> questDataList = new List<QuestData>();

   // Determines if achievement data is loaded
   public bool hasLoadedAchievements;

   // List of monster battler that can be associated with the npc's
   public Dictionary<int, BattlerData> battlerList;

   #endregion

   protected override void Awake () {
      base.Awake();
      instance = this;
      self = this;
      openMainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });
   }

   public void Start () {
      npcSelectionScreen.show();
      Invoke("initializeEquipmentData", MasterToolScene.loadDelay);
      XmlLoadingPanel.self.startLoading();
   }

   public AchievementData getAchievementData (int id) {
      if (achievementCollection.ContainsKey(id)) {
         return achievementCollection[id];
      }

      return new AchievementData();
   }

   private void initializeEquipmentData () {
      // Initialize all craftable item data after equipment data is setup
      EquipmentXMLManager.self.finishedDataSetup.AddListener(() => {
         loadAllDataFiles();
      });

      fetchRecipe();
      initializeOtherData();
      EquipmentXMLManager.self.initializeDataCache();
   }

   public void initializeOtherData () {
      questDataList = new List<QuestData>();
      battlerList = new Dictionary<int, BattlerData>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> questDataPair = DB_Main.getNPCQuestXML();
         List<XMLPair> monsterDataPair = DB_Main.getLandMonsterXML();
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair pair in monsterDataPair) {
               TextAsset newTextAsset = new TextAsset(pair.rawXmlData);
               BattlerData battleData = Util.xmlLoad<BattlerData>(newTextAsset);
               if (!battlerList.ContainsKey(pair.xmlId)) {
                  battlerList.Add(pair.xmlId, battleData);
               } else {
                  D.editorLog("This id is duplicated: " + pair.xmlId, Color.red);
               }
            }

            foreach (XMLPair pair in questDataPair) {
               TextAsset newTextAsset = new TextAsset(pair.rawXmlData);
               QuestData questData = Util.xmlLoad<QuestData>(newTextAsset);
               questData.questId = pair.xmlId;
               if (!questDataList.Exists(_=>_.questId == pair.xmlId)) {
                  questDataList.Add(questData);
               } else {
                  D.editorLog("This id is duplicated: " + pair.xmlId, Color.red);
               }
            }
         });
      });
   }

   public void loadAllDataFiles () {
      _npcData = new Dictionary<int, NPCData>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> achievementXML = DB_Main.getAchievementXML();
         List<string> rawXMLData = DB_Main.getNPCXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               NPCData npcData = Util.xmlLoad<NPCData>(newTextAsset);

               // Save the NPC data in the memory cache
               if (!_npcData.ContainsKey(npcData.npcId)) {
                  _npcData.Add(npcData.npcId, npcData);
               }
            }

            achievementCollection = new Dictionary<int, AchievementData>();
            foreach (XMLPair xmlPair in achievementXML) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               AchievementData achievementData = Util.xmlLoad<AchievementData>(newTextAsset);
               achievementCollection.Add(xmlPair.xmlId, achievementData);
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
      NPCData npcData = new NPCData(npcId, "", "", "", "", "", "", "", "", "NPC", true, true, true, -1, -1,
         new List<NPCGiftData>() { }, "", "", false, 0, -1, false);

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

   public List<NPCData> getNpcDataList () {
      return _npcData.Values.ToList();
   }

   #region Private Variables

   // The cached NPC data
   protected Dictionary<int, NPCData> _npcData = new Dictionary<int, NPCData>();

   #endregion
}
