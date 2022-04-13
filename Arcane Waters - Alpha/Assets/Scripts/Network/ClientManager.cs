using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Steamworks;
using SteamLoginSystem;
using System.Text;

public class ClientManager : GenericGameManager {
   #region Public Variables

   // Gets set to true when the Application is shutting down
   public static bool isApplicationQuitting = false;

   // A reference to the Message Manager
   public MessageManager messageManager;

   // Version number gameObject
   public GameObject versionGameObject;

   // Version Number text field
   public Text versionNumberText;

   // Self
   public static ClientManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      // Standalone players will eat up all possible CPU unless we set these
      QualitySettings.vSyncCount = 0;
      Application.targetFrameRate = 60;

      // Routinely clear out any unused assets to reduce memory usage
      InvokeRepeating("unloadUnusedAssets", 0f, 300);
   }

   void Start () {
      // Allow setting the auto host mode on the commmand line
      if (CommandCodes.get(CommandCodes.Type.AUTO_HOST) && Util.isServerBuild()) {
         Global.startAutoHost = true;
      }

      if (CommandCodes.get(CommandCodes.Type.AUTO_CLIENT)) {
         MyNetworkManager.self.StartClient();
      }

      if (CommandCodes.get(CommandCodes.Type.AUTO_DBCONFIG) || Util.isForceServerLocalWithAutoDbconfig()) {
         DB_Main.setServerFromConfig();
      }

      if (CommandCodes.get(CommandCodes.Type.AUTO_SERVER) || Util.isForceServerLocalWithAutoDbconfig()) {
         MyNetworkManager.self.StartServer();
      }

      if (Global.startAutoHost) {
         QuickLaunchPanel.self.hostToggle.isOn = true;
         QuickLaunchPanel.self.launch();
      }

      if (Util.isAutoTest()) {
         QuickLaunchPanel.self.accountInputField.text = "tester" + Util.getAutoTesterNumber();
         QuickLaunchPanel.self.passwordInputField.text = "test";
         QuickLaunchPanel.self.clientToggle.isOn = true;
         QuickLaunchPanel.self.serverToggle.isOn = false;
         QuickLaunchPanel.self.hostToggle.isOn = false;
         QuickLaunchPanel.self.launch();
      }

      // Try to start with fast login only if no command codes were used
      if (!MyNetworkManager.self.isNetworkActive) {
         Util.readFastLoginFile();
         if (Global.isFastLogin) {
            QuickLaunchPanel.self.startWithFastLogin();
         }
      }

      // Display client version info on screen
      displayClientVersionInfo();
   }

   void OnApplicationQuit () {
      isApplicationQuitting = true;
   }

   public void displayClientVersionInfo () {
      // Get the client version number from the cloud build manifest
      versionNumberText.text = Util.getJenkinsBuildTitle();
      versionGameObject.SetActive(true);
   }

   public void setDemoSuffixInVersionText (bool enabled) {
      versionNumberText.text = versionNumberText.text.Replace("-demo", "");

      if (enabled) {
         versionNumberText.text += "-demo";
      }
   }

   public static void sendAccountNameAndUserId () {
      string machineIdentifier = SystemInfo.deviceName;
      int deploymentId = Util.getDeploymentId();

      if (SteamManager.Initialized) {
         SteamLoginManager.self.getAuthTicketEvent.RemoveAllListeners();
         SteamLoginManager.self.getAuthTicketEvent = new GetAuthTicketEvent();

         // Wait for the php request response
         SteamLoginManager.self.getAuthTicketEvent.AddListener(_ => {
            CSteamID steamId = SteamUser.GetSteamID();
            Global.lastSteamId = steamId.ToString();

            // Extract the credentials
            LogInUserMessage msg = new LogInUserMessage( "", "", true, 
               Global.clientGameVersion, Global.currentlySelectedUserId, Application.platform, Global.isSinglePlayer, _.m_Ticket, _.m_pcbTicket, machineIdentifier, Global.isFirstLogin, SteamUtils.GetAppID().ToString(), Global.lastSteamId, Global.isRedirecting, deploymentId);

            // Send a message to the Server letting them know which of our Users we want to log in to
            NetworkClient.Send(msg);
         });

         // Trigger the fetching of the ownership info
         SteamLoginManager.self.getAuthenticationTicket();
      } else {
         Global.lastSteamId = "";
         LogInUserMessage msg = new LogInUserMessage(Global.lastUsedAccountName, Global.lastUserAccountPassword, false, 
            Global.clientGameVersion, Global.currentlySelectedUserId, Application.platform, Global.isSinglePlayer, new byte[0], 0, machineIdentifier, Global.isFirstLogin, "0", "", Global.isRedirecting, deploymentId);

         // Send a message to the Server letting them know which of our Users we want to log in to
         NetworkClient.Send(msg);
      }
   }

   protected void unloadUnusedAssets () {
      Resources.UnloadUnusedAssets();
   }

   #region Private Variables

   #endregion
}
