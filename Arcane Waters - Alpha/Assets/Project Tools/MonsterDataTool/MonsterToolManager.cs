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

public class MonsterToolManager : XmlDataToolManager {
   #region Public Variables

   // Reference to the tool scene
   public MonsterDataScene monsterToolScreen;

   // Holds the path of the folder
   public const string FOLDER_PATH = "MonsterStats";

   // Cache all abilities
   public List<BasicAbilityData> basicAbilityList = new List<BasicAbilityData>();

   // Cache offensive abilities only
   public List<AttackAbilityData> attackAbilityList = new List<AttackAbilityData>();

   // Cache buff abilities only
   public List<BuffAbilityData> buffAbilityList = new List<BuffAbilityData>();

   // Self
   public static MonsterToolManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      Invoke("loadAllDataFiles", MasterToolScene.loadDelay);
   }

   public void loadAllDataFiles () {
      XmlLoadingPanel.self.startLoading();
      basicAbilityList = new List<BasicAbilityData>();
      attackAbilityList = new List<AttackAbilityData>();
      buffAbilityList = new List<BuffAbilityData>();
      monsterDataList = new Dictionary<string, BattlerData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<string> rawXMLData = DB_Main.getLandMonsterXML();
         List<AbilityXMLContent> abilityContentList = DB_Main.getBattleAbilityXML();
         userIdData = DB_Main.getSQLDataByID(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string rawText in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawText);
               BattlerData monsterData = Util.xmlLoad<BattlerData>(newTextAsset);
               Enemy.Type typeID = (Enemy.Type) monsterData.enemyType;

               // Save the Monster data in the memory cache
               if (!monsterDataList.ContainsKey(monsterData.enemyName)) {
                  monsterDataList.Add(monsterData.enemyName, monsterData);
               }
            }

            foreach (AbilityXMLContent abilityXML in abilityContentList) {
               AbilityType abilityType = (AbilityType) abilityXML.abilityType;
               TextAsset newTextAsset = new TextAsset(abilityXML.abilityXML);

               switch (abilityType) {
                  case AbilityType.Standard:
                     AttackAbilityData attackAbilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityList.Add(attackAbilityData);
                     basicAbilityList.Add(attackAbilityData);
                     break;
                  case AbilityType.Stance:
                     AttackAbilityData stancebilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityList.Add(stancebilityData);
                     basicAbilityList.Add(stancebilityData);
                     break;
                  case AbilityType.BuffDebuff:
                     BuffAbilityData buffAbilityData = Util.xmlLoad<BuffAbilityData>(newTextAsset);
                     buffAbilityList.Add(buffAbilityData);
                     basicAbilityList.Add(buffAbilityData);
                     break;
                  default:
                     BasicAbilityData abilityData = Util.xmlLoad<BasicAbilityData>(newTextAsset);
                     basicAbilityList.Add(abilityData);
                     break;
               }
            }

            monsterToolScreen.updatePanelWithBattlerData(monsterDataList);
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void deleteMonsterDataFile (BattlerData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteLandmonsterXML((int) data.enemyType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public bool ifExists (string nameID) {
      return monsterDataList.ContainsKey(nameID);
   }
   
   public void saveDataToFile (BattlerData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateLandMonsterXML(longString, (int) data.enemyType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   public void duplicateData (BattlerData data) {
      int uniqueID = 0;
      foreach (Enemy.Type landMonsterType in Enum.GetValues(typeof(Enemy.Type))) {
         if (!monsterDataList.Values.ToList().Exists(_ => _.enemyType == landMonsterType) && landMonsterType != Enemy.Type.None) {
            uniqueID = (int) landMonsterType;
            break;
         }
      }
      if (uniqueID == 0) {
         Debug.LogWarning("All ID is accounted for, please edit data instead");
         return;
      }

      data.enemyName += "_copy";
      data.enemyType = (Enemy.Type) uniqueID;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateLandMonsterXML(longString, uniqueID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadAllDataFiles();
         });
      });
   }

   #region Private Variables

   private Dictionary<string, BattlerData> monsterDataList = new Dictionary<string, BattlerData>();

   #endregion
}
