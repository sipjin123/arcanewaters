using UnityEngine;
using UnityEngine.UI;

// This class is uses for reviewing logs when running multiple clients in one machine, instead of reviewing text file this feature allows on screen log review
// This class cannot be accessed if it is cloud build, it can be overridden by admin manager if the user is admin which is checked by the server
public class ScreenLogger : MonoBehaviour {
   #region Public Variables

   // If logger is enabled
   private bool isEnabled;

   // Self
   public static ScreenLogger self;

   // The text where the logs will show
   public Text textUI;

   // The canvas where the logs will show
   public Canvas canvas;

   #endregion

   private void Awake () {
      self = this;
      isEnabled = true;
      canvas.enabled = false;

      #if CLOUD_BUILD
      isEnabled = false;
      canvas.enabled = false;
      #endif
   }

   public void adminActivateLogger () {
      isEnabled = true;
      canvas.enabled = true;
   }

   public void displayLogMsg (string message) {
      if (!isEnabled || !canvas.enabled) {
         return;
      }

      textUI.text += "\n" + message;
   }

   private void Update () {
      if (!isEnabled) {
         return;
      }

      if (Input.GetKey(KeyCode.LeftAlt)) {
         // Displays log screen when holding left and Q button
         if (Input.GetKeyDown(KeyCode.Q)) {
            canvas.enabled = !canvas.enabled;
         }

         // Clears log screen when holding left and P button
         if (Input.GetKeyDown(KeyCode.P) && canvas.enabled) {
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
