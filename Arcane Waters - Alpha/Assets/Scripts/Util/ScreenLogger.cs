using UnityEngine;
using UnityEngine.UI;

// This class is uses for reviewing logs when running multiple clients in one machine, instead of reviewing text file this feature allows on screen log review
// This class cannot be accessed if it is cloud build, it can be overridden by admin manager if the user is admin which is checked by the server
public class ScreenLogger : MonoBehaviour {
   #region Public Variables

   // If logger is enabled
   public static bool isEnabled;

   // Self
   public static ScreenLogger self;

   // The text where the logs will show
   public Text textUI;

   // The object where the logs will show
   public GameObject canvasObj;

   #endregion

   private void Awake () {
      self = this;
      isEnabled = false;
      canvasObj.SetActive(false);
   }

   public void adminActivateLogger () {
      isEnabled = true;
      canvasObj.SetActive(true);
   }

   public void displayLogMsg (string message) {
      if (!isEnabled) {
         return;
      }

      try {
         textUI.text += "\n" + message;
      } catch { 
         // Only process text write if possible
      }
   }

   private void Update () {
      if (!isEnabled) {
         return;
      }

      if (Input.GetKey(KeyCode.LeftAlt)) {
         // Displays log screen when holding left and Q button
         if (Input.GetKeyDown(KeyCode.Q)) {
            canvasObj.SetActive(!canvasObj.activeSelf);
         }

         // Clears log screen when holding left and T button
         if (Input.GetKeyDown(KeyCode.T) && canvasObj.activeSelf) {
            clearLog();
         }
      }
   }

   public void clearLog () {
      if (!isEnabled) {
         return;
      }

      textUI.text = "";
   }

   #region Private Variables
      
   #endregion
}
