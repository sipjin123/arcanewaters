using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class TutorialStepData {
   #region Public Variables

   // The step id
   public int stepId;

   // The tutorial id 
   public int tutorialId;

   // The step name
   public string stepName;

   // The step description
   public string stepDescription;

   #endregion

   public TutorialStepData () { }

   public TutorialStepData (int stepId, int tutorialId, string stepName, string stepDescription) {
      this.tutorialId = tutorialId;
      this.stepId = stepId;
      this.stepName = stepName;
      this.stepDescription = stepDescription;
   }

#if IS_SERVER_BUILD

   public TutorialStepData (MySqlDataReader dataReader) {
      this.tutorialId = dataReader.GetInt32("tutorialId");
      this.stepId = dataReader.GetInt32("stepId");
      this.stepDescription = dataReader.GetString("stepDescription");
      this.stepName = dataReader.GetString("stepName");
   }

#endif

   #region Private Variables

   #endregion
}
