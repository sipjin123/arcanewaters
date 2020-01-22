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

   // Button for login adn logout trigger
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
   public GameObject loadingPaenl;

   // Current Account ID being used
   public int currentAccoundID;

   // Current permission level of the account
   public static PermissionLevel PERMISSION_LEVEL;

   #endregion

   private void Awake () {
      DontDestroyOnLoad(this);
      self = this;

      userNameField.text = PlayerPrefs.GetString(USERNAME_PREF);
      passwordField.text = "test";

      loginButton.onClick.AddListener(() => {
         loadingPaenl.SetActive(true);
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
                  loadingPaenl.SetActive(false);
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

               currentAccoundID = accID;
               PERMISSION_LEVEL = (PermissionLevel) permissionLevel;

               userNameText.text = userNameField.text;
               accountIDText.text = accID.ToString();
               permissionText.text = PERMISSION_LEVEL.ToString();
               loadingPaenl.SetActive(false);
            }
         });
      });
   }

   public static bool canAlterData () {
      if (PERMISSION_LEVEL == PermissionLevel.Admin || PERMISSION_LEVEL == PermissionLevel.ContentWriter) {
         return true;
      }

      return false;
   }

   private void Update () {
      if (Input.GetKeyDown(KeyCode.Alpha1)) {
         PERMISSION_LEVEL = PermissionLevel.Admin;
         permissionText.text = PERMISSION_LEVEL.ToString();
      }
      if (Input.GetKeyDown(KeyCode.Alpha2)) {
         PERMISSION_LEVEL = PermissionLevel.QA;
         permissionText.text = PERMISSION_LEVEL.ToString();
      }
      if (Input.GetKeyDown(KeyCode.Alpha3)) {
         PERMISSION_LEVEL = PermissionLevel.ContentWriter;
         permissionText.text = PERMISSION_LEVEL.ToString();
      }
   }

   #region Private Variables

   #endregion
}
public enum PermissionLevel
{
   User = 0,
   Admin = 1,
   QA = 2,
   ContentWriter = 3
}