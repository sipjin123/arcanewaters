using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialManager3 : MonoBehaviour {

   #region Public Variables

   // The keys used to save the config and progress in PlayerPrefs
   public static string MODE = "Tutorial_Mode";
   public static string SELECTED = "Tutorial_Selected";
   public static string STEP = "Tutorial_Step";
   public static string COMPLETED = "Tutorial_Completed_";

   // A reference to the tutorial panel
   public TutorialPanel3 panel;

   // Self
   public static TutorialManager3 self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void Start () {
      // Set the default config
      TutorialPanel3.Mode panelMode = TutorialPanel3.Mode.NPCSpeech;
      string selectedTutorialKey = TutorialData3.tutorials[0].key;
      _currentStep = 0;

      // Read the config from the PlayerPrefs
      if (PlayerPrefs.HasKey(MODE) && PlayerPrefs.HasKey(SELECTED) && PlayerPrefs.HasKey(STEP)) {
         panelMode = (TutorialPanel3.Mode) PlayerPrefs.GetInt(MODE);
         selectedTutorialKey = PlayerPrefs.GetString(SELECTED);
         _currentStep = PlayerPrefs.GetInt(STEP);

         // Read the completion state of each tutorial
         foreach (Tutorial3 tutorial in TutorialData3.tutorials) {
            if (PlayerPrefs.HasKey(COMPLETED + tutorial.key)) {
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
      refreshPanel();
   }

   public void selectTutorial (Tutorial3 tutorial) {
      // If we were at the last step of the previous tutorial (usually the 'congratulations'), consider it completed
      if (_currentStep >= _currentTutorial.steps.Count) {
         completeTutorial();
      }

      _currentTutorial = tutorial;
      _currentStep = 0;
      refreshPanel();
   }

   public void previousStep () {
      _currentStep--;

      // Keep the navigation in the current tutorial
      if (_currentStep < 0) {
         _currentStep = 0;
      }

      refreshPanel();
   }

   public void nextStep () {
      _currentStep++;
      if (_currentStep >= _currentTutorial.steps.Count) {
         completeTutorial();
      }

      refreshPanel();
   }

   public void tryCompletingStep (TutorialTrigger key) {
      if (!isActive()) {
         return;
      }

      if (key == _currentTutorial.steps[_currentStep].completionTrigger) {
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
      saveConfigAndProgress();
   }

   private void refreshPanel () {
      string selectedTutorialKey = _currentTutorial.key;
      string npcSpeech = _currentTutorial.steps[_currentStep].npcSpeech;
      bool isNextStepManual = _currentTutorial.steps[_currentStep].completionTrigger == TutorialTrigger.Manual;
      panel.refreshTutorialStep(selectedTutorialKey, npcSpeech, isNextStepManual);
   }

   public void OnDestroy () {
      saveConfigAndProgress();
   }

   private void saveConfigAndProgress () {
      PlayerPrefs.SetInt(MODE, (int) panel.getMode());
      PlayerPrefs.SetString(SELECTED, _currentTutorial.key);
      PlayerPrefs.SetInt(STEP, _currentStep);

      foreach (Tutorial3 tutorial in TutorialData3.tutorials) {
         string prefsKey = COMPLETED + tutorial.key;
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

      // First look down in the list
      for (int i = currentIndex; i < TutorialData3.tutorials.Count; i++) {
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

   private bool isActive () {
      switch (panel.getMode()) {
         case TutorialPanel3.Mode.TutorialList:
         case TutorialPanel3.Mode.NPCSpeech:
            return true;
         case TutorialPanel3.Mode.QuestionMark:
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

   #endregion
}
