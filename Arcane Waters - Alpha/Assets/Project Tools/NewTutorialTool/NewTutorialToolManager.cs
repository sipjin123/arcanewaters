using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NewTutorialToolManager : XmlDataToolManager {
   #region Public Variables

   // The tutorial tool scene
   public NewTutorialToolScene scene;

   #endregion

   public void loadNewTutorialList () {
      XmlLoadingPanel.self.startLoading();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         _tutorialDataList = DB_Main.getNewTutorialList();
         dispatchLoadNewTutorialListOnScene();
      });
      XmlLoadingPanel.self.finishLoading();
   }

   public void saveNewTutorial (NewTutorialData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.upsertNewTutorial(data);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadNewTutorialList();
         });
      });
   }

   public void saveTutorialStep (TutorialStepData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.upsertTutorialStep(data);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadNewTutorialList();
         });
      });
   }

   public void duplicateNewTutorial (NewTutorialData data) {
      data.tutorialId = 0;

      foreach (TutorialStepData step in data.tutorialStepList) {
         step.stepId = 0;
      }

      saveNewTutorial(data);
   }

   public void deleteNewTutorial (NewTutorialData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteNewTutorialById(data.tutorialId);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadNewTutorialList();
         });
      });
   }

   public void deleteTutorialStep (TutorialStepData data) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteTutorialStepById(data.stepId);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadNewTutorialList();
         });
      });
   }

   public void loadAreaKeys () {
      List<string> areaKeys = new List<string>();
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         areaKeys = DB_Main.getAreaKeysForTutorial();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            scene.loadAreaKeys(areaKeys);
            XmlLoadingPanel.self.finishLoading();
         });
      });

   }

   private void dispatchLoadNewTutorialListOnScene () {
      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         scene.loadData(_tutorialDataList);
      });
   }

   #region Private Variables

   // The list of tutorial data
   private List<NewTutorialData> _tutorialDataList;

   #endregion
}
