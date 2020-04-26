using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class TutorialStepAction {
   #region Public Variables

   // The db id
   public int stepActionId;

   // The unique code for this action
   public string code;

   // The display name for this action
   public string displayName;

   #endregion

   public TutorialStepAction () { }

#if IS_SERVER_BUILD
   public TutorialStepAction (MySqlDataReader reader) {
      stepActionId = reader.GetInt32("stepActionId");
      code = reader.GetString("code");
      displayName = reader.GetString("displayName");
   }
#endif

   #region Private Variables

   #endregion
}
