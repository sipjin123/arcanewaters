using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class AbilityToolManager : XmlDataToolManager
{
   #region Public Variables

   // Reference to the tool scene
   public AbilityDataScene abilityScene;

   // Reference to the tool for the monster ability manager
   public MonsterAbilityManager monsterAbilityManager;

   // Holds the path of the folder
   public const string FOLDER_PATH = "Ability";

   public enum DirectoryType
   {
      BasicAbility,
      AttackAbility,
      BuffAbility
   }

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start () {
      Invoke("loadXML", MasterToolScene.loadDelay);

      SoundEffectManager.self.initializeDataCache();
   }

   public void loadXML () {
      abilityDataList = new Dictionary<int, BasicAbilityData>();
      attackAbilityList = new Dictionary<int, AttackAbilityData>();
      buffAbilityList = new Dictionary<int, BuffAbilityData>();

      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<AbilityXMLContent> xmlContentList = DB_Main.getBattleAbilityXML();
         userNameData = DB_Main.getSQLDataByName(editorToolType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (AbilityXMLContent xmlContent in xmlContentList) {
               TextAsset newTextAsset = new TextAsset(xmlContent.abilityXML);

               AbilityType abilityType = (AbilityType) xmlContent.abilityType;
               // Read and deserialize the file
               switch (abilityType) {
                  case AbilityType.Standard:
                     AttackAbilityData attackAbilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityData.itemID = xmlContent.abilityId;
                     attackAbilityList.Add(xmlContent.abilityId, attackAbilityData);
                     abilityDataList.Add(xmlContent.abilityId, attackAbilityData);
                     break;
                  case AbilityType.Stance:
                     AttackAbilityData stancebilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     stancebilityData.itemID = xmlContent.abilityId;
                     attackAbilityList.Add(xmlContent.abilityId, stancebilityData);
                     abilityDataList.Add(xmlContent.abilityId, stancebilityData);
                     break;
                  case AbilityType.BuffDebuff:
                     BuffAbilityData buffAbilityData = Util.xmlLoad<BuffAbilityData>(newTextAsset);
                     buffAbilityData.itemID = xmlContent.abilityId;
                     buffAbilityList.Add(xmlContent.abilityId, buffAbilityData);
                     abilityDataList.Add(xmlContent.abilityId, buffAbilityData);
                     break;
                  default:
                     BasicAbilityData abilityData = Util.xmlLoad<BasicAbilityData>(newTextAsset);
                     abilityData.itemID = xmlContent.abilityId;
                     abilityDataList.Add(xmlContent.abilityId, abilityData);
                     break;
               }
            }

            if (abilityScene != null) {
               abilityScene.updateWithAbilityData(abilityDataList, attackAbilityList, buffAbilityList);
            }
            if (monsterAbilityManager != null) {
               monsterAbilityManager.updateWithAbilityData(abilityDataList, attackAbilityList, buffAbilityList);
            }
            XmlLoadingPanel.self.finishLoading();
         });
      });
   }
   

   public void duplicateFile (BasicAbilityData data) {
      data.itemName = MasterToolScene.UNDEFINED;
      data.itemID++;
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBattleAbilities(-1, data.itemName, longString, (int) data.abilityType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   public void deleteSkillDataFile (int skillID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteBattleAbilityXML(skillID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   public void saveAbility (BasicAbilityData data) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBattleAbilities(data.itemID, data.itemName, longString, (int)data.abilityType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   #region Private Variables

   // Ability Cache
   private Dictionary<int, BasicAbilityData> abilityDataList = new Dictionary<int, BasicAbilityData>();
   private Dictionary<int, AttackAbilityData> attackAbilityList = new Dictionary<int, AttackAbilityData>();
   private Dictionary<int, BuffAbilityData> buffAbilityList = new Dictionary<int, BuffAbilityData>();

   #endregion
}

public class AbilityXMLContent
{
   // Type of ability
   public int abilityType = 0;

   // Ability Content
   public string abilityXML = "";

   // Id of the creator
   public int ownderId = 0;

   // Unique Id of the ability
   public int abilityId = 0;
}