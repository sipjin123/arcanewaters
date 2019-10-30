using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;

public class AbilityToolManager : MonoBehaviour
{
   #region Public Variables

   // Reference to the tool scene
   public AbilityDataScene abilityScene;

   // Reference to the tool for the monster scene
   public MonsterSceneAbilityManager monsterAbilityScene;

   public enum DirectoryType
   {
      BasicAbility,
      AttackAbility,
      BuffAbility
   }

   #endregion

   private void Start () {
      loadAllDataFiles();
   }

   public void loadAllDataFiles () {
      abilityDataList = new Dictionary<string, BasicAbilityData>();
      attackAbilityList = new Dictionary<string, AttackAbilityData>();
      buffAbilityList = new Dictionary<string, BuffAbilityData>();

      loadBasicAbility(DirectoryType.BasicAbility);
      loadBasicAbility(DirectoryType.AttackAbility);
      loadBasicAbility(DirectoryType.BuffAbility);

      if (abilityScene != null) {
         abilityScene.updateWithAbilityData(abilityDataList, attackAbilityList, buffAbilityList);
      }
      if (monsterAbilityScene != null) {
         monsterAbilityScene.updateWithAbilityData(abilityDataList, attackAbilityList, buffAbilityList);
      }
   }

   private void loadBasicAbility(DirectoryType directoryType) {
      // Build the path to the folder containing the ability data XML files
      string directoryPath = "";
      directoryPath = Path.Combine(Application.dataPath, "Data/Ability", getDirectory(directoryType));

      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      } else {
         // Get the list of XML files in the folder
         string[] fileNames = ToolsUtil.getFileNamesInFolder(directoryPath, "*.xml");

         // Iterate over the files
         for (int i = 0; i < fileNames.Length; i++) {
            // Build the path to a single file
            string filePath = Path.Combine(directoryPath, fileNames[i]);

            // Read and deserialize the file
            switch (directoryType) {
               case DirectoryType.BasicAbility: {
                     BasicAbilityData abilityData = ToolsUtil.xmlLoad<BasicAbilityData>(filePath);
                     abilityDataList.Add(abilityData.itemName, abilityData);
                  }
                  break;
               case DirectoryType.AttackAbility: {
                     AttackAbilityData abilityData = ToolsUtil.xmlLoad<AttackAbilityData>(filePath);
                     attackAbilityList.Add(abilityData.itemName, abilityData);
                  }
                  break;
               case DirectoryType.BuffAbility: {
                     BuffAbilityData abilityData = ToolsUtil.xmlLoad<BuffAbilityData>(filePath);
                     buffAbilityList.Add(abilityData.itemName, abilityData);
                  }
                  break;
            }
         }
      }
   }

   public void deleteSkillDataFile (BasicAbilityData data, DirectoryType directoryType) {
      // Build the file name
      string fileName = data.itemName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data/Ability", getDirectory(directoryType), fileName + ".xml");

      // Delete the file
      ToolsUtil.deleteFile(path);
   }

   public bool ifExists (string nameID) {
      return abilityDataList.ContainsKey(nameID);
   }

   public void saveAbility (BasicAbilityData data, DirectoryType directoryType) {
      string directoryPath = "";
      directoryPath = Path.Combine(Application.dataPath, "Data/Ability", getDirectory(directoryType));

      if (!Directory.Exists(directoryPath)) {
         DirectoryInfo folder = Directory.CreateDirectory(directoryPath);
      }

      // Build the file name
      string fileName = data.itemName;

      // Build the path to the file
      string path = Path.Combine(Application.dataPath, "Data/Ability", getDirectory(directoryType), fileName + ".xml");

      // Save the file
      ToolsUtil.xmlSave(data, path);
   }

   private string getDirectory (DirectoryType directoryType) {
      string directoryPath = "";
      switch (directoryType) {
         case DirectoryType.BasicAbility:
            directoryPath = "BasicAbility";
            break;
         case DirectoryType.AttackAbility:
            directoryPath = "AttackAbility";
            break;
         case DirectoryType.BuffAbility:
            directoryPath = "BuffAbility";
            break;
      }
      return directoryPath;
   }

   #region Private Variables

   // Ability Cache
   private Dictionary<string, BasicAbilityData> abilityDataList = new Dictionary<string, BasicAbilityData>();
   private Dictionary<string, AttackAbilityData> attackAbilityList = new Dictionary<string, AttackAbilityData>();
   private Dictionary<string, BuffAbilityData> buffAbilityList = new Dictionary<string, BuffAbilityData>();

   #endregion
}
