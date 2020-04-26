using System;
using System.Collections.Generic;
using System.Linq;

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
   public List<TutorialStepViewModel> tutorialSteps;

   #endregion

   public TutorialViewModel () {
      tutorialSteps = new List<TutorialStepViewModel>();
   }

   public TutorialViewModel (NewTutorialData tutorialData, List<UserTutorialStep> completedUserSteps) {
      tutorialName = tutorialData.tutorialName;
      tutorialDescription = tutorialData.tutorialDescription;
      tutorialAreaKey = tutorialData.tutorialAreaKey;
      tutorialImgUrl = tutorialData.tutorialImageUrl;

      foreach (TutorialStepData step in tutorialData.tutorialStepList) {
         UserTutorialStep completedStep = completedUserSteps.SingleOrDefault(x => x.stepId == step.stepId);
         TutorialStepViewModel tutorialStepViewModel = new TutorialStepViewModel();
         tutorialStepViewModel.stepName = step.stepName;
         tutorialStepViewModel.stepDescription = step.stepDescription;
         tutorialStepViewModel.completedTimestamp = completedStep == null ? (DateTime?) null : completedStep.completedTimestamp;
         tutorialSteps.Add(tutorialStepViewModel);
      }
   }

   public TutorialViewModel (NewTutorialData tutorialData) {
      tutorialName = tutorialData.tutorialName;
      tutorialDescription = tutorialData.tutorialDescription;
      tutorialAreaKey = tutorialData.tutorialAreaKey;
      tutorialImgUrl = tutorialData.tutorialImageUrl;
   }

   #region Private Variables

   #endregion
}
