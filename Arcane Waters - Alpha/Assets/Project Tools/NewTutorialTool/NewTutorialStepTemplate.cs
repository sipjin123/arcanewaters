using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NewTutorialStepTemplate : MonoBehaviour {
   #region Public Variables

   // The step Id
   public int id;

   // The step Name
   public InputField stepName;

   // The step description
   public InputField stepDescription;

   // The action selection dropdown
   public TMPro.TMP_Dropdown stepAction;

   // Delete button
   public Button deleteButton;

   #endregion

   #region Private Variables

   #endregion
}
