using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class TutorialManager3 : MonoBehaviour {

   #region Public Variables

   // The keys used to save the config and progress in PlayerPrefs
   public static string MODE = "Tutorial_Mode_";
   public static string SELECTED = "Tutorial_Selected_";
   public static string STEP = "Tutorial_Step_";
   public static string COMPLETED = "Tutorial_Completed_";

   // A reference to the tutorial panel
   public TutorialPanel3 panel;

   // A reference to the tutorial arrow
   public TutorialArrow arrow;

   // Self
   public static TutorialManager3 self;

   // The list of tutorial data
   public List<Tutorial3> tutorialDataList = new List<Tutorial3>();

   #endregion

   public void Awake () {
      self = this;
   }

   public void onUserSpawns (int userId) {
      updateArrow();

      if (_userId == userId) {
         return;
      }

      // If a new user has logged in, initialize the tutorial config, progress and panel
      _userId = userId;

      // Set the default config
      TutorialPanel3.Mode panelMode = TutorialPanel3.Mode.NPCSpeech;
      string selectedTutorialKey = tutorialDataList[0].key;
      _currentStep = 0;
      foreach (Tutorial3 tutorial in tutorialDataList) {
         tutorial.isCompleted = false;
      }

      // Read the config from the PlayerPrefs
      if (PlayerPrefs.HasKey(MODE + _userId) && PlayerPrefs.HasKey(SELECTED + _userId) && PlayerPrefs.HasKey(STEP + _userId)) {
         panelMode = (TutorialPanel3.Mode) PlayerPrefs.GetInt(MODE + _userId);
         selectedTutorialKey = PlayerPrefs.GetString(SELECTED + _userId);
         _currentStep = PlayerPrefs.GetInt(STEP + _userId);

         // Read the completion state of each tutorial
         foreach (Tutorial3 tutorial in tutorialDataList) {
            if (PlayerPrefs.HasKey(COMPLETED + _userId + "_" + tutorial.key)) {
               tutorial.isCompleted = true;
            }
         }
      }

      // Create the tutorial rows in the panel
      panel.initialize(panelMode, tutorialDataList);

      // Get the selected tutorial
      foreach (Tutorial3 tutorial in tutorialDataList) {
         if (string.Equals(tutorial.key, selectedTutorialKey)) {
            _currentTutorial = tutorial;
         }
      }

      // If the selected tutorial was not found, find one that is not completed
      if (_currentTutorial == null) {
         _currentTutorial = tutorialDataList[0];
         _currentTutorial = getNextUncompletedTutorial();
         _currentStep = 0;
      } else {
         // Reset the current step if it is inconsistent
         if (_currentStep >= _currentTutorial.steps.Count) {
            _currentStep = 0;
         }
      }

      // If all the tutorials are completed, hide the tutorial panel (override the user config)
      if (_currentTutorial == null) {
         panel.onCloseButtonPressed();
      }

      saveConfigAndProgress();
      refreshUI();
   }

   public void onUserLogOut () {
      saveConfigAndProgress();
      _userId = -1;
   }

   public void updateArrow () {
      if (Global.player == null || !isActive()) {
         return;
      }

      // Since we changed area, check if the tutorial arrow can point to something relevant
      arrow.setTarget(_currentTutorial.steps[_currentStep].targetAreaKey);
   }

   public void selectTutorial (Tutorial3 tutorial) {
      // If we were at the last step of the previous tutorial (usually the 'congratulations'), consider it completed
      if (_currentStep >= _currentTutorial.steps.Count) {
         completeTutorial();
      }

      _currentTutorial = tutorial;
      _currentStep = 0;
      _triggerCount = 0;
      refreshUI();

      // Check if the completion conditions are already met - and skip the step if so
      tryCompletingStepByLocation();
      tryCompletingStepByWeaponEquipped();
   }

   public void previousStep () {
      _currentStep--;

      // Keep the navigation in the current tutorial
      if (_currentStep < 0) {
         _currentStep = 0;
      } else {
         _triggerCount = 0;
         refreshUI();
      }
   }

   public void nextStep () {
      _currentStep++;
      if (_currentStep >= _currentTutorial.steps.Count) {
         completeTutorial();
      }

      _triggerCount = 0;
      refreshUI();

      // Check if the completion conditions are already met - and skip the step if so
      tryCompletingStepByLocation();
      tryCompletingStepByWeaponEquipped();
   }

   public void tryCompletingStep (TutorialTrigger key) {
      if (!isActive()) {
         return;
      }

      if (key == _currentTutorial.steps[_currentStep].completionTrigger) {
         if (key == TutorialTrigger.EquipWeapon) {
            // Check if the required weapon is equipped
            tryCompletingStepByWeaponEquipped();
         } else {
            // Some steps require multiple repetitions of the action
            _triggerCount++;
            if (_triggerCount >= _currentTutorial.steps[_currentStep].countRequirement) {
               nextStep();
            }
         }
      }
   }

   public void tryCompletingStepByLocation () {
      if (!isActive()) {
         return;
      }

      // Complete the step if the user reached the target location
      if (isUserAtTargetLocation()) {
         nextStep();
      }
   }

   public void tryCompletingStepByWeaponEquipped () {
      if (!isActive()
         || Global.getUserObjects().weapon == null) {
         return;
      }

      // Either the equipWeapon trigger or a weapon action must be defined for the current tutorial step
      if (_currentTutorial.steps[_currentStep].completionTrigger != TutorialTrigger.EquipWeapon &&
         _currentTutorial.steps[_currentStep].weaponAction == Weapon.ActionType.None) {
         return;
      }

      // Temporary until new item system is implemented
      WeaponStatData weaponData;
      if (string.IsNullOrEmpty(Global.getUserObjects().weapon.data)) {
         weaponData = EquipmentXMLManager.self.getWeaponData(Global.getUserObjects().weapon.itemTypeId);
      } else {
         weaponData = WeaponStatData.getStatData(Global.getUserObjects().weapon.data, Global.getUserObjects().weapon.itemTypeId);
      }

      if (weaponData != null) {
         Weapon newWeapon = WeaponStatData.translateDataToWeapon(weaponData);

         // Check that the weapon action type is the one required
         if (newWeapon.getActionType() == _currentTutorial.steps[_currentStep].weaponAction) {
            nextStep();
         }
      }
   }

   public TutorialTrigger getCurrentTrigger () {
      if (isActive()) {
         return _currentTutorial.steps[_currentStep].completionTrigger;
      } else {
         return TutorialTrigger.None;
      }
   }

   private void completeTutorial () {
      // Complete the current tutorial and get the next
      _currentTutorial.isCompleted = true;
      _currentTutorial = getNextUncompletedTutorial();

      // If there are no more uncompleted tutorials, select the last one (the end notice)
      if (_currentTutorial == null) {
         _currentTutorial = tutorialDataList[tutorialDataList.Count - 1];
      }

      _currentStep = 0;
      _triggerCount = 0;
      saveConfigAndProgress();
   }

   private bool isUserAtTargetLocation () {
      string targetAreaKey = _currentTutorial.steps[_currentStep].targetAreaKey;

      if (string.IsNullOrEmpty(targetAreaKey)) {
         return false;
      }

      if (string.Equals(targetAreaKey, CustomFarmManager.GROUP_AREA_KEY)) {
         return AreaManager.self.isFarmOfUser(Global.player.areaKey, Global.player.userId);
      } else if (string.Equals(targetAreaKey, CustomHouseManager.GROUP_AREA_KEY)) {
         return AreaManager.self.isHouseOfUser(Global.player.areaKey, Global.player.userId);
      } else {
         if (string.Equals(Global.player.areaKey, targetAreaKey)) {
            return true;
         }
      }

      return false;
   }

   private void refreshUI () {
      string selectedTutorialKey = _currentTutorial.key;
      bool isNextStepManual = _currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.Manual;

      string npcSpeech = _currentTutorial.steps[_currentStep].npcSpeech;

      // Handle dynamic npc speechs
      if (_currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.MoveShip) {
         npcSpeech = npcSpeech.Replace("[northp]", InputManager.getBinding(KeyAction.MoveUp).primary.ToString());
         npcSpeech = npcSpeech.Replace("[norths]", InputManager.getBinding(KeyAction.MoveUp).secondary.ToString());
         npcSpeech = npcSpeech.Replace("[eastp]", InputManager.getBinding(KeyAction.MoveRight).primary.ToString());
         npcSpeech = npcSpeech.Replace("[easts]", InputManager.getBinding(KeyAction.MoveRight).secondary.ToString());
         npcSpeech = npcSpeech.Replace("[southp]", InputManager.getBinding(KeyAction.MoveDown).primary.ToString());
         npcSpeech = npcSpeech.Replace("[souths]", InputManager.getBinding(KeyAction.MoveDown).secondary.ToString());
         npcSpeech = npcSpeech.Replace("[westp]", InputManager.getBinding(KeyAction.MoveLeft).primary.ToString());
         npcSpeech = npcSpeech.Replace("[wests]", InputManager.getBinding(KeyAction.MoveLeft).secondary.ToString());
      }

      panel.refreshTutorialStep(selectedTutorialKey, npcSpeech, _currentStep + 1, _currentTutorial.steps.Count,
         isNextStepManual, _currentTutorial.isCompleted);
      arrow.setTarget(_currentTutorial.steps[_currentStep].targetAreaKey);
   }

   public void OnDestroy () {
      saveConfigAndProgress();
   }

   private void saveConfigAndProgress () {
      if (_userId < 0) {
         return;
      }

      PlayerPrefs.SetInt(MODE + _userId, (int) panel.getMode());
      PlayerPrefs.SetString(SELECTED + _userId, _currentTutorial.key);
      PlayerPrefs.SetInt(STEP + _userId, _currentStep);

      foreach (Tutorial3 tutorial in tutorialDataList) {
         string prefsKey = COMPLETED + _userId + "_" + tutorial.key;
         if (tutorial.isCompleted) {
            PlayerPrefs.SetInt(prefsKey, 1);
         } else {
            PlayerPrefs.DeleteKey(prefsKey);
         }
      }
   }

   private Tutorial3 getNextUncompletedTutorial () {
      // Get the index of the current tutorial
      int currentIndex = 0;
      for (int i = 0; i < tutorialDataList.Count; i++) {
         if (tutorialDataList[i] == _currentTutorial) {
            currentIndex = i;
            break;
         }
      }

      // First look down in the list - but leave out the last one, which is the end notice
      for (int i = currentIndex; i < tutorialDataList.Count - 1; i++) {
         if (!tutorialDataList[i].isCompleted) {
            return tutorialDataList[i];
         }
      }

      // Then start looking from the beginning
      for (int i = 0; i < currentIndex; i++) {
         if (!tutorialDataList[i].isCompleted) {
            return tutorialDataList[i];
         }
      }

      return null;
   }

   public void receiveDataFromZip (List<Tutorial3> newTutorialDataList) {
      // Sort the tutorials by their order index
      tutorialDataList = newTutorialDataList.Where(t => t.isActive).OrderBy(t => t.order).ToList();

      _currentTutorial = tutorialDataList[0];
   }

   public bool isActive () {
      switch (panel.getMode()) {
         case TutorialPanel3.Mode.TutorialList:
         case TutorialPanel3.Mode.NPCSpeech:
            return true;
         case TutorialPanel3.Mode.Closed:
         default:
            return false;
      }
   }

   #region Private Variables

   // The current selected tutorial
   private Tutorial3 _currentTutorial;

   // The index of the current step in the tutorial
   private int _currentStep = 0;

   // The number of times the completion trigger has been set off for the current step
   private int _triggerCount = 0;

   // The user whose tutorial config and progress is currently loaded
   private int _userId = -1;

   #endregion
}

public enum TutorialTrigger
{
   None = 0,
   Manual = 1,
   TalkShopOwner = 2,
   BuyWeapon = 3,
   ExpandTutorialPanel = 5,
   OpenInventory = 6,
   EquipWeapon = 7,
   ShipSpeedUp = 8,
   FireShipCannon = 9,
   DefeatPirateShip = 10,
   PlantCrop = 12,
   CropGrewToMaxLevel = 13,
   HarvestCrop = 14,
   OpenFarmLayoutSelectionPanel = 15,
   OpenHouseLayoutSelectionPanel = 17,
   OpenVoyagePanel = 19,
   SpawnInVoyage = 20,
   EnterBattle = 22,
   AttackBattleTarget = 24,
   EndBattle = 25,
   EnterTreasureSiteRange = 26,
   PlaceObject = 28,
   DeleteObject = 29,
   UnequipHammer = 30,
   MoveShip = 33,
   LeaveVoyageGroup = 35,
   SelectPirateShip = 36,
   MoveObject = 37,
   SelectObject = 38,
   OpenMerchantScreen = 39,
   OpenTradeConfirmScreen = 40,
   SellCrops = 41,
   CloseMerchantScreen = 42,
   SpawnInLeagueNotLobby = 43,
   SwitchToOffensiveStance = 44,
   SpawnInLobby = 45,
};
