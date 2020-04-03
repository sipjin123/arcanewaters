using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ClientManager : MonoBehaviour {
   #region Public Variables

   // Gets set to true when the Application is shutting down
   public static bool isApplicationQuitting = false;

   // A reference to the Message Manager
   public MessageManager messageManager;

   // Self
   public static ClientManager self;

   #endregion

   void Awake () {
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

      if (CommandCodes.get(CommandCodes.Type.AUTO_SERVER)) {
         MyNetworkManager.self.StartServer();
      }

      if (CommandCodes.get(CommandCodes.Type.AUTO_DBCONFIG) || Util.isForceServerLocalWithAutoDbconfig()) {
         DB_Main.setServerFromConfig();
         MyNetworkManager.self.StartServer();
      }

      if (Global.startAutoHost) {
         QuickLaunchPanel.self.hostToggle.isOn = true;
         QuickLaunchPanel.self.launch();
      }

      if (CommandCodes.get(CommandCodes.Type.AUTO_TEST)) {
         int testerNumber = Util.getCommandLineInt(CommandCodes.Type.AUTO_TEST+"");
         QuickLaunchPanel.self.accountInputField.text = "tester" + testerNumber;
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
   }

   void OnApplicationQuit () {
      isApplicationQuitting = true;
   }

   public static void sendAccountNameAndUserId () {
      LogInUserMessage msg = new LogInUserMessage(Global.netId,
         Global.lastUsedAccountName, Global.lastUserAccountPassword, Global.clientGameVersion, Global.currentlySelectedUserId, Application.platform, QuickLaunchPanel.self.singlePlayerToggle.isOn);

      // Send a message to the Server letting them know which of our Users we want to log in to
      NetworkClient.Send(msg);
   }

   protected void unloadUnusedAssets () {
      Resources.UnloadUnusedAssets();
   }

   #region Private Variables

   #endregion
}
