using System;

public class TutorialStepViewModel {
   #region Public Variables

   // The name of the step
   public string stepName;

   // The description of the step
   public string stepDescription;

   // The action required to complete this step
   public string actionDescription;

   // The completed timestamp of this step for a given user
   public DateTime completedTimestamp;

   // Wether the step was completed by user
   public bool isCompleted;

   #endregion

   #region Private Variables

   #endregion
}
