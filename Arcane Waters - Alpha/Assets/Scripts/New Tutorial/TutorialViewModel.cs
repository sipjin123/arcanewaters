using System;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class TutorialViewModel {
   #region Public Variables

   //The name of the tutorial
   public string tutorialName;

   // The description of the tutorial
   public string tutorialDescription;

   // The url of the image of the tutorial
   public string tutorialImgUrl;

   // The area key of the map for this tutorial
   public string tutorialAreaKey;

   // The list of steps for this tutorial
   public TutorialStepViewModel[] tutorialSteps;

   #endregion

   public TutorialViewModel () { }

   public TutorialViewModel (NewTutorialData tutorialData, List<UserTutorialStep> completedUserSteps) {
      tutorialName = tutorialData.tutorialName;
      tutorialDescription = tutorialData.tutorialDescription;
      tutorialAreaKey = tutorialData.tutorialAreaKey;
      tutorialImgUrl = tutorialData.tutorialImageUrl;
      tutorialSteps = new TutorialStepViewModel[tutorialData.tutorialStepList.Count];
      int i = 0;

      foreach (TutorialStepData step in tutorialData.tutorialStepList) {
         UserTutorialStep completedStep = completedUserSteps.SingleOrDefault(x => x.stepId == step.stepId);
         TutorialStepViewModel tutorialStepViewModel = new TutorialStepViewModel();
         tutorialStepViewModel.stepName = step.stepName;
         tutorialStepViewModel.stepDescription = step.stepDescription;
         tutorialStepViewModel.actionDescription = step.stepAction.displayName;
         tutorialStepViewModel.isCompleted = completedStep != null;
         tutorialStepViewModel.completedTimestamp = completedStep == null ? new DateTime() : completedStep.completedTimestamp.Value;
         tutorialSteps[i] = tutorialStepViewModel;
         i++;
      }
   }

   public TutorialViewModel (NewTutorialData tutorialData) {
      tutorialName = tutorialData.tutorialName;
      tutorialDescription = tutorialData.tutorialDescription;
      tutorialAreaKey = tutorialData.tutorialAreaKey;
      tutorialImgUrl = tutorialData.tutorialImageUrl;

      tutorialSteps = new TutorialStepViewModel[tutorialData.tutorialStepList.Count];
      int i = 0;

      foreach (TutorialStepData step in tutorialData.tutorialStepList) {
         TutorialStepViewModel tutorialStepViewModel = new TutorialStepViewModel();
         tutorialStepViewModel.stepName = step.stepName;
         tutorialStepViewModel.stepDescription = step.stepDescription;
         tutorialStepViewModel.actionDescription = step.stepAction.displayName;
         tutorialSteps[i] = tutorialStepViewModel;
         i++;
      }
   }

   #region Private Variables

   #endregion
}
