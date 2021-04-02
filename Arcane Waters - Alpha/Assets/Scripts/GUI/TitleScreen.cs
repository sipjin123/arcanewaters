﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using NubisDataHandling;
using UnityEngine.InputSystem;

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

   // The canvas reference
   public CanvasGroup titleScreenCanvas;

   // Debug UI for resolution testing of MAC
   public Button setToMaxScreenWindows;
   public Button setToMaxScreenExclusive;
   public Text windowResolutionText, monitorResolutionText;

   // The gameobject referencing the title screen map
   public GameObject titleScreenReference;

   // Reference to the battleboard script of the title screen
   public BattleBoard battleBoardReference;

   #endregion

   private void Awake () {
      self = this;
   }

   void Start () {
      _canvasGroup = GetComponent<CanvasGroup>();

      defaultLoginPanel.SetActive(!SteamManager.Initialized);
      steamLoginPanel.SetActive(SteamManager.Initialized);

      setToMaxScreenWindows.onClick.AddListener(() => {
         ScreenSettingsManager.setToResolutionFullscreenWindowed();
      });
      setToMaxScreenExclusive.onClick.AddListener(() => {
         ScreenSettingsManager.setToResolutionFullscreenExclusive();
      });

      CameraManager.self.resolutionChanged += onResolutionChanged;
      battleBoardReference.setWeather(WeatherEffectType.Cloud, battleBoardReference.biomeType);
   }

   private void OnDestroy () {
      CameraManager.self.resolutionChanged -= onResolutionChanged;
   }

   private void onResolutionChanged () {
      windowResolutionText.text = "Your Window is: W: " + Screen.width + " H: " + Screen.height;
      monitorResolutionText.text = "Your Screen is: W: " + Screen.currentResolution.width + " H: " + Screen.currentResolution.height;
   }

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Make the canvas disabled while the client is running
      bool isActive = this.isActive();

      if (_canvasGroup.interactable != isActive || _canvasGroup.blocksRaycasts != isActive) {
         _canvasGroup.interactable = isActive;
         _canvasGroup.blocksRaycasts = isActive;
      }

      // Save the current alpha
      float currentAlpha = _canvasGroup.alpha;

      if (isActive) {
         // If they press Enter in the password field, activate the Play button
         if (KeyUtils.GetKeyDown(Key.Enter) && Util.isSelected(passwordInputField) && passwordInputField.text != "" && passwordInputField.text.Length > 0 && accountInputField.text.Length > 0) {
            Util.clickButton(loginButton);
         }

         // Check for an assortment of keys
         bool moveToNextField = KeyUtils.GetKeyDown(Key.Tab) || KeyUtils.GetKeyDown(Key.Enter) || KeyUtils.GetKeyDown(Key.DownArrow);

         // If we're in the account field, let us move to the password field
         if (moveToNextField && Util.isSelected(accountInputField)) {
            Util.select(passwordInputField);
         }

         // Make sure the canvas group is visible if the screen is active
         if (_canvasGroup.alpha < 1) {
            _canvasGroup.alpha = 1;
         }

         if (!titleScreenReference.activeInHierarchy) {
            titleScreenReference.SetActive(true);
         }

         if (!virtualCamera.gameObject.activeInHierarchy) {
            Util.activateVirtualCamera(virtualCamera);
         }
      } else {
         // Slowly fade out the canvas group if the screen isn't active
         if (_canvasGroup.alpha > 0) {
            _canvasGroup.alpha = currentAlpha - Time.smoothDeltaTime;
         }

         if (titleScreenReference.activeInHierarchy) {
            titleScreenReference.SetActive(false);
         }
      }
   }

   public void onLoginButtonPressed (bool isSteam) {
      if (ServerHistoryManager.self.isServerHistoryActive()) {
         // Check if the server is online by looking at the boot history using Nubis
         NubisDataFetcher.self.checkServerOnlineForClientLogin(isSteam);
      } else {
         startUpNetworkClient(isSteam);
      }
   }

   public void startUpNetworkClient (bool isSteam) {
      // Stop the client in case we're already connected to the server
      MyNetworkManager.self.StopClient();

      if (isSteam || (!isSteam && passwordInputField.text.Length > 0 && accountInputField.text.Length > 0)) {
         PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.Login);

         LoadingUtil.executeAfterFade(() => {
            // Start up the Network Client, which triggers the rest of the login process
            MyNetworkManager.self.StartClient();
         });
      } else {
         displayError(ErrorMessage.Type.FailedUserOrPass);
      }
   }

   public void openOptionsPanel () {
      PanelManager.self.linkIfNotShowing(Panel.Type.Options);
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

   public void displayError (ErrorMessage.Type errorType, ErrorMessage message = null) {
      // We didn't log in, so stop the client and restart the login process
      MyNetworkManager.self.StopHost();

      switch (errorType) {
         case ErrorMessage.Type.ClientOutdated:
            if (SteamManager.Initialized) {
               PanelManager.self.noticeScreen.show($"Please update your client to log in!");
            } else {
               PanelManager.self.noticeScreen.show($"Please download the new version <link=\"{ downloadNewVersionLink }\"><u>here</u></link>");
            }
            break;
         case ErrorMessage.Type.FailedUserOrPass:
            PanelManager.self.noticeScreen.show("Invalid account/password combination.");
            break;
         case ErrorMessage.Type.Banned:
            string panelMessage = message != null ? message.customMessage : "Your account has been suspended.";
            PanelManager.self.noticeScreen.show(panelMessage);
            break;
         case ErrorMessage.Type.AlreadyOnline:
            PanelManager.self.noticeScreen.show("Your account has been disconnected because you logged in somewhere else.");
            break;
         case ErrorMessage.Type.ServerOffline:
            PanelManager.self.noticeScreen.show("The server is offline.");
            break;
         case ErrorMessage.Type.SteamWebOffline:
            PanelManager.self.noticeScreen.show("Unable to connect to server, please try again later.");
            break;
         default:
            if (message != null) {
               PanelManager.self.noticeScreen.show(message.customMessage);
            }
            break;
      }
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
