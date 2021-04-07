using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class QuickLaunchPanel : MonoBehaviour {
   #region Public Variables

   // Title screen reference
   public TitleScreen titleScreen;

   // The Account to use for our quick launch
   public InputField accountInputField;

   // The Password to use for our quick launch
   public InputField passwordInputField;

   // The drop down menu to select the database server - debug only
   public Dropdown dbServerDropDown;

   // The currently selected port - debug only
   public Text portText;

   // Our Host Mode toggle
   public Toggle hostToggle;

   // Our Client Mode toggle
   public Toggle clientToggle;

   // Our Server Mode toggle
   public Toggle serverToggle;

   // Our Game is in single player mode
   public Toggle singlePlayerToggle;

   // Some keys we use to store login credentials
   public static string ACCOUNT_KEY = "quick_launch_account";
   public static string PASSWORD_KEY = "quick_launch_password";
   public static string STEAM_ID_KEY = "quick_launch_steam_id";

   // Self
   public static QuickLaunchPanel self;

   #endregion

   private void Awake () {
      self = this;

      Debug.Log("Server build: " + Util.isServerBuild());
      // Debug.Log("Test class result: " + TestClass.someIntFunction());

      // We only show this panel if it's a server build, never in the production client
      this.gameObject.SetActive(Util.isServerBuild());

      singlePlayerToggle.onValueChanged.AddListener(_ => {
         Global.isSinglePlayer = _;
      });
   }

   private void Start () {
      // Default setting
      hostToggle.isOn = true;

      // Check if we've saved any quick launch settings
      string savedKey = PlayerPrefs.GetString(ACCOUNT_KEY);
      string savedPassword = PlayerPrefs.GetString(PASSWORD_KEY);

      if (!string.IsNullOrEmpty(savedKey) && !string.IsNullOrEmpty(savedPassword)) {
         this.accountInputField.text = PlayerPrefs.GetString(ACCOUNT_KEY);
         this.passwordInputField.text = PlayerPrefs.GetString(PASSWORD_KEY);
      }
   }

   private void Update () {
      // Show the current port
      portText.text = MyNetworkManager.getCurrentPort() + "";
   }

   public void launch () {
      if (passwordInputField.text.Length > 0 && accountInputField.text.Length > 0) {
         // Store the values we've specified
         PlayerPrefs.SetString(ACCOUNT_KEY, this.accountInputField.text);
         PlayerPrefs.SetString(PASSWORD_KEY, this.passwordInputField.text);
         if (SteamManager.Initialized) {
            Steamworks.CSteamID steamId = Steamworks.SteamUser.GetSteamID();
            Global.isSteamLogin = true;
            Global.lastSteamId = steamId.ToString();
            PlayerPrefs.SetString(STEAM_ID_KEY, steamId.ToString());
         }

         // Fill in the fields in the actual login panel
         TitleScreen.self.accountInputField.text = this.accountInputField.text;
         TitleScreen.self.passwordInputField.text = this.passwordInputField.text;

         // Launch into the appropriate mode, depending on which toggle was selected
         if (hostToggle.isOn) {
            MyNetworkManager.self.StartHost();
         } else if (clientToggle.isOn) {
            PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.Login);

            LoadingUtil.executeAfterFade(() => {
               MyNetworkManager.self.StartClient();
            });
         } else if (serverToggle.isOn) {
            MyNetworkManager.self.StartServer();
         }
         
      } else {
         titleScreen.displayError(ErrorMessage.Type.FailedUserOrPass);
      }
   }

   public void changePort (int modifier) {
      MyNetworkManager.self.telepathy.port += (ushort) modifier;
   }

   public void startWithFastLogin () {
      TitleScreen.self.accountInputField.text = Global.fastLoginAccountName;
      TitleScreen.self.passwordInputField.text = Global.fastLoginAccountPassword;

      if (Global.isFastLoginHostMode) {
         MyNetworkManager.self.StartHost();
      } else {
         MyNetworkManager.self.StartClient();
      }
   }

   public void refreshDatabaseServer () {
      #if IS_SERVER_BUILD
      switch (dbServerDropDown.value) {
         case 0:
            DB_Main.setServer(DB_Main.RemoteServer);
            break;

         case 1:
            DB_Main.setServer("127.0.0.1");
            break;

         case 2:
            DB_Main.setServerFromConfig();
            break;
      }
      #endif
   }

   #region Private Variables

   #endregion
}
