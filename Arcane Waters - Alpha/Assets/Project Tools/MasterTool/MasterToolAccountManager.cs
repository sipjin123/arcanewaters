using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;
using MapCreationTool.Serialization;

public class MasterToolAccountManager : MonoBehaviour {
   #region Public Variables

   // Reference to self
   public static MasterToolAccountManager self;

   // Inputfields
   public InputField userNameField;
   public InputField passwordField;

   // Main canvas of the login panel and the passive data
   public Canvas loginCanvas, passiveCanvas;

   // Main canvas of the info of the user
   public CanvasGroup passiveCanvasGroup;

   // Button for login and logout trigger
   public Button loginButton;
   public Button logOutButton;

   // Login error UI indicator
   public GameObject errorPanel;
   public Text errorText;
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
   public static PrivilegeType PERMISSION_LEVEL = PrivilegeType.None;

   // Button for exiting the application
   public Button exitButton;

   // User details found in the logout panel
   public GameObject[] userDetails;

   #endregion

   private void Awake () {
      DontDestroyOnLoad(this);
      self = this; 
      revealPassivePanel(false);
      passiveCanvas.enabled = false;

      userNameField.text = PlayerPrefs.GetString(USERNAME_PREF);

      // Get the program version from the cloud manifest
      try {
         _programVersion = Util.getGameVersion();
      } catch {
         D.debug("Failed to get linux game version");
      }

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

            // Look up the minimum version of the program required to login
            int minimumToolsVersion;
            if (Application.platform == RuntimePlatform.OSXPlayer) {
               minimumToolsVersion = DB_Main.getMinimumToolsVersionForMac();
            } else if (Application.platform == RuntimePlatform.LinuxPlayer) {
               minimumToolsVersion = DB_Main.getMinimumToolsVersionForLinux();
            } else {
               minimumToolsVersion = DB_Main.getMinimumToolsVersionForWindows();
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {

               // If this build is too old, ask the player to download a newer version
               if (_programVersion < minimumToolsVersion) {
                  errorText.text = "Please download the new version!";
                  errorPanel.SetActive(true);
                  loadingPanel.SetActive(false);
               } else if (accID > 0) {
                  proceedToLogin(accID);
               } else {
                  errorPanel.SetActive(true);
                  loadingPanel.SetActive(false);
               }
            });
         });
      });

      logOutButton.onClick.AddListener(() => {
         loginCanvas.enabled = true;
         passiveCanvas.enabled = false;

         passwordField.text = "";
         userNameText.text = "";
         permissionText.text = "";
         accountIDText.text = "";

         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      closeErrorPanel.onClick.AddListener(() => { errorPanel.SetActive(false); });
   }

   private void proceedToLogin (int accID) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         int permissionLevel = DB_Main.getAccountPermissionLevel(accID);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (permissionLevel > 0) {
               loginCanvas.enabled = false;
               passiveCanvas.enabled = true;

               PlayerPrefs.SetString(USERNAME_PREF, userNameField.text);
               PlayerPrefs.SetInt(ACCOUNT_ID_PREF, accID);

               currentAccountID = accID;
               PERMISSION_LEVEL = (PrivilegeType) permissionLevel;

               userNameText.text = userNameField.text;
               accountIDText.text = accID.ToString();
               permissionText.text = PERMISSION_LEVEL.ToString();
               loadingPanel.SetActive(false);
            }
         });
      });
   }

   public void revealPassivePanel (bool isActive) {
      passiveCanvasGroup.alpha = isActive ? 1 : .3f;
      foreach (GameObject obj in userDetails) {
         obj.SetActive(isActive);
      }
   }

   public static bool canAlterData () {
      if (PERMISSION_LEVEL == PrivilegeType.Admin || PERMISSION_LEVEL == PrivilegeType.ContentWriter) {
         return true;
      }

      return false;
   }

   public static bool canAlterResource (int resourceCreatorID, out string errorMessage) {
      if (!canAlterData()) {
         errorMessage = "Your account type has no permissions to alter data";
         return false;
      }

      if (PERMISSION_LEVEL != PrivilegeType.Admin && resourceCreatorID != self.currentAccountID) {
         errorMessage = "You are not the creator of this resource";
         return false;
      }

      errorMessage = null;
      return true;
   }

   #region Private Variables

   // The version of this build
   private int _programVersion = 0;

   #endregion
}