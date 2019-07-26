using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TitleScreen : MonoBehaviour {
   #region Public Variables

   // The account name field
   public InputField accountInputField;

   // The password field
   public InputField passwordInputField;

   // The login button
   public Button loginButton;

   // The container for the debug button group
   public GameObject debugButtonGroup;

   // The drop down menu to select the database server - debug only
   public Dropdown dbServerDropDown;

   // The Virtual Camera we use for the Title Screen
   public Cinemachine.CinemachineVirtualCamera virtualCamera;

   // Self
   public static TitleScreen self;

   #endregion

   private void Awake () {
      self = this;

      Debug.Log("Server build: " + Util.isServerBuild());
      Debug.Log("Test class result: " + TestClass.someIntFunction());

      // We don't show the debug button group in the normal build
      debugButtonGroup.SetActive(Util.isServerBuild());
   }

   void Start () {
      _canvasGroup = GetComponent<CanvasGroup>();
   }

   private void Update () {
      // Make the canvas disabled while the client is running
      bool disabled = NetworkClient.active || NetworkServer.active || Global.isRedirecting || CharacterScreen.self.isShowing();
      _canvasGroup.interactable = !disabled;
      _canvasGroup.blocksRaycasts = !disabled;

      // Slowly fade out
      float currentAlpha = _canvasGroup.alpha;
      _canvasGroup.alpha = disabled ? currentAlpha - Time.smoothDeltaTime : 1f;

      // If they press Enter in the password field, activate the Play button
      if (Input.GetKeyDown(KeyCode.Return) && Util.isSelected(passwordInputField) && passwordInputField.text != "") {
         Util.clickButton(loginButton);
      }

      // Check for an assortment of keys
      bool moveToNextField = Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.DownArrow);

      // If we're in the account field, let us move to the password field
      if (moveToNextField && Util.isSelected(accountInputField)) {
         Util.select(passwordInputField);
      }
   }

   public void startHost (int num=0) {
      // Auto-fill the fields in the server build for faster testing
      if (num != 0) {
         accountInputField.text = "Tester" + num;
         passwordInputField.text = "test";
      }

      MyNetworkManager.self.StartHost();
   }

   public void startClient (int clientNumber=1) {
      // Auto-fill the fields in the server build for faster testing
      if (accountInputField.text == "" && Util.isServerBuild()) {
         accountInputField.text = "Tester" + clientNumber;
         passwordInputField.text = "test";
      }

      MyNetworkManager.self.StartClient();
   }

   public void startServer () {
      MyNetworkManager.self.StartServer();
   }

   public void startUpNetworkClient() {
      // Start up the Network Client, which triggers the rest of the login process
      MyNetworkManager.self.StartClient();
   }

   public void startWithFastLogin () {
      accountInputField.text = Global.fastLoginAccountName;
      passwordInputField.text = Global.fastLoginAccountPassword;

      if (Global.isFastLoginHostMode) {
         MyNetworkManager.self.StartHost();
      } else {
         MyNetworkManager.self.StartClient();
      }
   }

   public bool isShowing () {
      if (_canvasGroup == null) {
         return false;
      }

      return _canvasGroup.alpha != 0f;
   }

   public void displayError (ErrorMessage.Type errorType) {
      // We didn't log in, so stop the client and restart the login process
      MyNetworkManager.self.StopHost();

      switch (errorType) {
         case ErrorMessage.Type.ClientOutdated:
            PanelManager.self.noticeScreen.show("Please download the new version!");
            break;
         case ErrorMessage.Type.FailedUserOrPass:
            PanelManager.self.noticeScreen.show("Invalid account/password combination.");
            break;
      }
   }

   public void refreshDatabaseServer() {
      if (dbServerDropDown.value == 0) {
         DB_Main.setServer(DB_Main.RemoteServer);
      } else {
         DB_Main.setServer("127.0.0.1");
      }
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
