using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewTutorialStepRow : MonoBehaviour {

   #region Public Variables

   // The title text reference
   public TextMeshProUGUI titleText;

   // The description text reference
   public TextMeshProUGUI descriptionText;

   // The toggle to display if the step was completed by the user
   public Toggle isCompleteToggle;

   // The text to display the "Completed timestamp" label
   public TextMeshProUGUI completedTimestampLabel;

   // The text to display the completed timestamp if any
   public TextMeshProUGUI completedTimestampValue;

   // The action required for this step
   public TextMeshProUGUI actionText;

   #endregion

   #region Private Variables

   #endregion
}
