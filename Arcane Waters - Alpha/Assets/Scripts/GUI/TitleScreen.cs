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
   public Button loginButton, steamLoginButton;

   // The drop down menu to select the database server - debug only
   public Dropdown dbServerDropDown;

   // The Virtual Camera we use for the Title Screen
   public Cinemachine.CinemachineVirtualCamera virtualCamera;

   // The link we redirect the users to download a new version
   public string downloadNewVersionLink;

   // Self
   public static TitleScreen self;

   // The various login panels
   public GameObject steamLoginPanel, defaultLoginPanel;

   #endregion

   private void Awake () {
      self = this;
   }

   void Start () {
      _canvasGroup = GetComponent<CanvasGroup>();

      defaultLoginPanel.SetActive(!SteamManager.Initialized);
      steamLoginPanel.SetActive(SteamManager.Initialized);
   }

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Make the canvas disabled while the client is running
      bool disabled = !isActive();
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

   public void startUpNetworkClient () {
      // Start up the Network Client, which triggers the rest of the login process
      MyNetworkManager.self.StartClient();
   }

   public bool isShowing () {
      if (_canvasGroup == null) {
         return false;
      }

      return _canvasGroup.alpha != 0f;
   }

   public bool isActive () {
      return !NetworkClient.active && !NetworkServer.active && !Global.isRedirecting && !CharacterScreen.self.isShowing();
   }

   public void displayError (ErrorMessage.Type errorType) {
      // We didn't log in, so stop the client and restart the login process
      MyNetworkManager.self.StopHost();

      switch (errorType) {
         case ErrorMessage.Type.ClientOutdated:
            PanelManager.self.noticeScreen.show($"Please download the new version <link=\"{ downloadNewVersionLink }\"><u>here</u></link>");
            break;
         case ErrorMessage.Type.FailedUserOrPass:
            PanelManager.self.noticeScreen.show("Invalid account/password combination.");
            break;
      }
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
