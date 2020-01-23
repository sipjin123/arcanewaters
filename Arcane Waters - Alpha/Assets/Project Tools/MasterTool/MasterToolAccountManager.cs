using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MasterToolAccountManager : MonoBehaviour {
   #region Public Variables

   // Reference to self
   public static MasterToolAccountManager self;

   // Inputfields
   public InputField userNameField;
   public InputField passwordField;

   // Main canvas of the login panel
   public Canvas loginCanvas;

   // Main canvas of the info of the user
   public CanvasGroup passiveCanvas;

   // Button for login and logout trigger
   public Button loginButton;
   public Button logOutButton;

   // Login error UI indicator
   public GameObject errorPanel;
   public Button closeErrorPanel;

   public static string USERNAME_PREF = "UserName";
   public static string ACCOUNT_ID_PREF = "UserID";

   // Text Display
   public Text userNameText, accountIDText, permissionText;

   // Blocker for account fetching
   public GameObject loadingPanel;

   // Current Account ID being used
   public int currentAccountID;

   // Current permission level of the account
   public static AdminManager.Type PERMISSION_LEVEL;

   // Button for exiting the application
   public Button exitButton;

   #endregion

   private void Awake () {
      DontDestroyOnLoad(this);
      self = this; 
      revealPassivePanel(false);

      userNameField.text = PlayerPrefs.GetString(USERNAME_PREF);

      exitButton.onClick.AddListener(() => {
         Application.Quit();
      });

      loginButton.onClick.AddListener(() => {
         loadingPanel.SetActive(true);
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Look up the account ID corresponding to the provided account name and password
            string salt = Util.createSalt("arcane");
            string hashedPassword = Util.hashPassword(salt, passwordField.text);
            int accID = DB_Main.getAccountId(userNameField.text, hashedPassword);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               if (accID > 0) {
                  proceedToLogin(accID);
               } else {
                  errorPanel.SetActive(true);
                  loadingPanel.SetActive(false);
               }
            });
         });
      });

      closeErrorPanel.onClick.AddListener(() => { errorPanel.SetActive(false); });
   }

   private void proceedToLogin (int accID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int permissionLevel = DB_Main.getAccountPermissionLevel(accID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (permissionLevel > 0) {
               loginCanvas.enabled = false;
               PlayerPrefs.SetString(USERNAME_PREF, userNameField.text);
               PlayerPrefs.SetInt(ACCOUNT_ID_PREF, accID);

               currentAccountID = accID;
               PERMISSION_LEVEL = (AdminManager.Type) permissionLevel;

               userNameText.text = userNameField.text;
               accountIDText.text = accID.ToString();
               permissionText.text = PERMISSION_LEVEL.ToString();
               loadingPanel.SetActive(false); 
            }
         });
      });
   }

   public void revealPassivePanel (bool isActive) {
      passiveCanvas.alpha = isActive ? 1 : .3f;
   }

   public static bool canAlterData () {
      if (PERMISSION_LEVEL == AdminManager.Type.Admin || PERMISSION_LEVEL == AdminManager.Type.ContentWriter) {
         return true;
      }

      return false;
   }

   #region Private Variables

   #endregion
}