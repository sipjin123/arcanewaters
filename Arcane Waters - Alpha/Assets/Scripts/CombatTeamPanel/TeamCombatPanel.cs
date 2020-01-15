using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class TeamCombatPanel : Panel
{
   #region Public Variables

   // Prefab for template data
   public BattlerTemplate battlerTempPrefab;

   // Prefab holders
   public Transform leftBattlerParent, rightBattlerParent;

   // Selection for drop down
   public Dropdown[] battlerTypeSelection;

   // Add battler button
   public Button addLeftBattler, addRightBattler;

   // Cached battler type
   public Enemy.Type[] battlerType;

   // Type of enemies that are existing in xml
   public List<Enemy.Type> availableEnemyTypes;

   // Type of enemies attacking and defending
   public List<Enemy.Type> leftBattlers, rightBattlers;

   // Launches the team battler system
   public Button launchBattler;

   #endregion

   public override void Awake () {
      base.Awake();
      addLeftBattler.onClick.AddListener(() => {
         int index = 0;
         int currValueSelected = battlerTypeSelection[index].value;
         string currTypeName = battlerTypeSelection[index].options[currValueSelected].text;

         Enemy.Type currType = (Enemy.Type) Enum.Parse(typeof(Enemy.Type), currTypeName);
         battlerType[index] = currType;

         createTemplate(currType, leftBattlerParent);
      });
      addRightBattler.onClick.AddListener(() => {
         int index = 1;
         int currValueSelected = battlerTypeSelection[index].value;
         string currTypeName = battlerTypeSelection[index].options[currValueSelected].text;

         Enemy.Type currType = (Enemy.Type) Enum.Parse(typeof(Enemy.Type), currTypeName);
         battlerType[index] = currType;

         createTemplate(currType, rightBattlerParent);
      });

      launchBattler.onClick.AddListener(() => {
         leftBattlers = new List<Enemy.Type>();
         foreach (Transform template in leftBattlerParent) {
            leftBattlers.Add(template.GetComponent<BattlerTemplate>().battlerDataCache.enemyType);
         }

         rightBattlers = new List<Enemy.Type>();
         foreach (Transform template in rightBattlerParent) {
            rightBattlers.Add(template.GetComponent<BattlerTemplate>().battlerDataCache.enemyType);
         }

         PanelManager.self.popPanel();

         Global.player.rpc.Cmd_StartNewTeamBattle(leftBattlers.ToArray(), rightBattlers.ToArray());
      });
   }

   private void createTemplate (Enemy.Type enemyType, Transform parent) {
      BattlerData battlerData = MonsterManager.self.getMonster(enemyType);
      BattlerTemplate template = Instantiate(battlerTempPrefab.gameObject, parent).GetComponent<BattlerTemplate>();
      template.battlerName.text = battlerData.enemyName;
      template.battlerIcon.sprite = ImageManager.getSprite(battlerData.imagePath);
      template.battlerType.text = battlerData.enemyType.ToString();
      template.battlerDataCache = battlerData;
   }

   private void setupDropDown () {
      int dropdownIndex = 0;
      foreach (Dropdown dropDown in battlerTypeSelection) {
         List<Dropdown.OptionData> optionDataList = new List<Dropdown.OptionData>();

         foreach (Enemy.Type enemyType in Enum.GetValues(typeof(Enemy.Type))) {
            if (availableEnemyTypes.Contains(enemyType)) {
               Dropdown.OptionData optionData = new Dropdown.OptionData {
                  image = null,
                  text = enemyType.ToString()
               };
               optionDataList.Add(optionData);
            }
         }

         dropDown.options.Clear();
         dropDown.AddOptions(optionDataList);

         dropdownIndex++;
      }
   }

   public void receiveDataFromServer (int[] enemyTypes) {
      availableEnemyTypes = new List<Enemy.Type>();
      foreach (int index in enemyTypes) {
         availableEnemyTypes.Add((Enemy.Type)index);
      }
      setupDropDown();
   }

   public void fetchSQLData () {
      Global.player.rpc.Cmd_RequestTeamCombatData();
   }

   #region Private Variables

   #endregion
}