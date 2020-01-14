using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

public class AbilityToolManager : MonoBehaviour
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

   private void Start () {
      Invoke("loadXML", MasterToolScene.loadDelay);
   }

   public void loadXML () {
      abilityDataList = new Dictionary<string, BasicAbilityData>();
      attackAbilityList = new Dictionary<string, AttackAbilityData>();
      buffAbilityList = new Dictionary<string, BuffAbilityData>();

      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List< AbilityXMLContent> xmlContentList = DB_Main.getBattleAbilityXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (AbilityXMLContent xmlContent in xmlContentList) {
               TextAsset newTextAsset = new TextAsset(xmlContent.abilityXML);

               AbilityType abilityType = (AbilityType) xmlContent.abilityType;
               // Read and deserialize the file
               switch (abilityType) {
                  case AbilityType.Standard:
                     AttackAbilityData attackAbilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityList.Add(attackAbilityData.itemName, attackAbilityData);
                     abilityDataList.Add(attackAbilityData.itemName, attackAbilityData);
                     break;
                  case AbilityType.Stance:
                     AttackAbilityData stancebilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                     attackAbilityList.Add(stancebilityData.itemName, stancebilityData);
                     abilityDataList.Add(stancebilityData.itemName, stancebilityData);
                     break;
                  case AbilityType.BuffDebuff:
                     BuffAbilityData buffAbilityData = Util.xmlLoad<BuffAbilityData>(newTextAsset);
                     buffAbilityList.Add(buffAbilityData.itemName, buffAbilityData);
                     abilityDataList.Add(buffAbilityData.itemName, buffAbilityData);
                     break;
                  default:
                     BasicAbilityData abilityData = Util.xmlLoad<BasicAbilityData>(newTextAsset);
                     abilityDataList.Add(abilityData.itemName, abilityData);
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
      data.itemName = "Undefined Faction";

      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBattleAbilities(data.itemName, longString, (int) data.abilityType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   public void deleteSkillDataFile (BasicAbilityData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteBattleAbilityXML(data.itemName);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   public bool ifExists (string nameID) {
      return abilityDataList.ContainsKey(nameID);
   }

   public void saveAbility (BasicAbilityData data, DirectoryType directoryType) {
      XmlSerializer ser = new XmlSerializer(data.GetType());
      var sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, data);
      }

      string longString = sb.ToString();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateBattleAbilities(data.itemName, longString, (int)data.abilityType);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   #region Private Variables

   // Ability Cache
   private Dictionary<string, BasicAbilityData> abilityDataList = new Dictionary<string, BasicAbilityData>();
   private Dictionary<string, AttackAbilityData> attackAbilityList = new Dictionary<string, AttackAbilityData>();
   private Dictionary<string, BuffAbilityData> buffAbilityList = new Dictionary<string, BuffAbilityData>();

   #endregion
}

public class AbilityXMLContent
{
   // Type of ability
   public int abilityType = 0;

   // Ability Content
   public string abilityXML = "";
}