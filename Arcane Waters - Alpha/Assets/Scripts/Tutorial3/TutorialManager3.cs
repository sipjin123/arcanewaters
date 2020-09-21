using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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
      string selectedTutorialKey = TutorialData3.tutorials[0].key;
      _currentStep = 0;
      foreach (Tutorial3 tutorial in TutorialData3.tutorials) {
         tutorial.isCompleted = false;
      }

      // Read the config from the PlayerPrefs
      if (PlayerPrefs.HasKey(MODE + _userId) && PlayerPrefs.HasKey(SELECTED + _userId) && PlayerPrefs.HasKey(STEP + _userId)) {
         panelMode = (TutorialPanel3.Mode) PlayerPrefs.GetInt(MODE + _userId);
         selectedTutorialKey = PlayerPrefs.GetString(SELECTED + _userId);
         _currentStep = PlayerPrefs.GetInt(STEP + _userId);

         // Read the completion state of each tutorial
         foreach (Tutorial3 tutorial in TutorialData3.tutorials) {
            if (PlayerPrefs.HasKey(COMPLETED + _userId + "_" + tutorial.key)) {
               tutorial.isCompleted = true;
            }
         }
      }

      // Create the tutorial rows in the panel
      panel.initialize(panelMode, TutorialData3.tutorials);

      // Get the selected tutorial
      foreach (Tutorial3 tutorial in TutorialData3.tutorials) {
         if (string.Equals(tutorial.key, selectedTutorialKey)) {
            _currentTutorial = tutorial;
         }
      }

      // If the selected tutorial was not found, find one that is not completed
      if (_currentTutorial == null) {
         _currentTutorial = TutorialData3.tutorials[0];
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
      arrow.setTarget(_currentTutorial.steps[_currentStep].arrowTarget);
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
   }

   public void tryCompletingStep (TutorialTrigger key) {
      if (!isActive()) {
         return;
      }

      if (key == _currentTutorial.steps[_currentStep].completionTrigger) {
         // Some steps require multiple repetitions of the action
         _triggerCount++;
         if (_triggerCount >= _currentTutorial.steps[_currentStep].countRequirement) {
            nextStep();
         }
      }

      // Special cases
      if (key == TutorialTrigger.SpawnInFarm 
         && _currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.OpenFarmLayoutSelectionPanel) {
         // If the farm layout has already been chosen, we skip this step
         nextStep();
         nextStep();
      }

      if (key == TutorialTrigger.SpawnInHouse
         && _currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.OpenHouseLayoutSelectionPanel) {
         // If the house layout has already been chosen, we skip this step
         nextStep();
         nextStep();
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
         _currentTutorial = TutorialData3.tutorials[TutorialData3.tutorials.Count - 1];
      }

      _currentStep = 0;
      _triggerCount = 0;
      saveConfigAndProgress();
   }

   private void refreshUI () {
      string selectedTutorialKey = _currentTutorial.key;
      bool isNextStepManual = _currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.Manual;

      string npcSpeech = _currentTutorial.steps[_currentStep].npcSpeech;

      // Handle dynamic npc speechs
      if (_currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.TurnShipLeft) {
         npcSpeech = npcSpeech.Replace("[primary]", InputManager.getBinding(KeyAction.MoveLeft).primary.ToString());
         npcSpeech = npcSpeech.Replace("[secondary]", InputManager.getBinding(KeyAction.MoveLeft).secondary.ToString());
      }

      if (_currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.TurnShipRight) {
         npcSpeech = npcSpeech.Replace("[primary]", InputManager.getBinding(KeyAction.MoveRight).primary.ToString());
         npcSpeech = npcSpeech.Replace("[secondary]", InputManager.getBinding(KeyAction.MoveRight).secondary.ToString());
      }

      if (_currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.MoveShipForward) {
         npcSpeech = npcSpeech.Replace("[primary]", InputManager.getBinding(KeyAction.MoveUp).primary.ToString());
         npcSpeech = npcSpeech.Replace("[secondary]", InputManager.getBinding(KeyAction.MoveUp).secondary.ToString());
      }

      panel.refreshTutorialStep(selectedTutorialKey, npcSpeech, _currentStep + 1, _currentTutorial.steps.Count, isNextStepManual);
      arrow.setTarget(_currentTutorial.steps[_currentStep].arrowTarget);
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

      foreach (Tutorial3 tutorial in TutorialData3.tutorials) {
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
      for (int i = 0; i < TutorialData3.tutorials.Count; i++) {
         if (TutorialData3.tutorials[i] == _currentTutorial) {
            currentIndex = i;
            break;
         }
      }

      // First look down in the list - but leave out the last one, which is the end notice
      for (int i = currentIndex; i < TutorialData3.tutorials.Count - 1; i++) {
         if (!TutorialData3.tutorials[i].isCompleted) {
            return TutorialData3.tutorials[i];
         }
      }

      // Then start looking from the beginning
      for (int i = 0; i < currentIndex; i++) {
         if (!TutorialData3.tutorials[i].isCompleted) {
            return TutorialData3.tutorials[i];
         }
      }

      return null;
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
   private Tutorial3 _currentTutorial = TutorialData3.tutorials[0];

   // The index of the current step in the tutorial
   private int _currentStep = 0;

   // The number of times the completion trigger has been set off for the current step
   private int _triggerCount = 0;

   // The user whose tutorial config and progress is currently loaded
   private int _userId = -1;

   #endregion
}
