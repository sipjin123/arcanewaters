using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// This class is uses for reviewing logs when running multiple clients in one machine, instead of reviewing text file this feature allows on screen log review
// This class cannot be accessed if it is cloud build, it can be overridden by admin manager if the user is admin which is checked by the server
public class ScreenLogger : GenericGameManager {
   #region Public Variables

   // If logger is enabled
   public static bool isEnabled;

   // Self
   public static ScreenLogger self;

   // The text where the logs will show
   public Text textUI;

   // The object where the logs will show
   public GameObject canvasObj;

   // The maximum log count in characters before the text UI is deducted
   public const int MAX_LOG_COUNT = 5000;

   // The deduct count when text reaches max cap
   public const int TEXT_DEDUCT_COUNT = 50;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      isEnabled = false;
      canvasObj.SetActive(false);

      Global.logTypesToShow.Add(D.ADMIN_LOG_TYPE.AnimationFreeze);
      //Global.logTypesToShow.Add(D.ADMIN_LOG_TYPE.CombatEnd);
      D.debug("Screen Logger v4");

      if (!Util.isCloudBuild()) {
         isEnabled = true;
      }
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

      if (textUI.text.Length > MAX_LOG_COUNT) {
         try {
            textUI.text = textUI.text.Remove(0, TEXT_DEDUCT_COUNT);
         } catch {
         }
      }
   }

   private void Update () {
      if (!isEnabled) {
         return;
      }

      if (KeyUtils.GetKey(Key.LeftAlt)) {
         // Displays log screen when holding left and Q button
         if (KeyUtils.GetKeyDown(Key.Q)) {
            canvasObj.SetActive(!canvasObj.activeSelf);
         }

         // Clears log screen when holding left and R button
         if (KeyUtils.GetKeyDown(Key.R)) {
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
