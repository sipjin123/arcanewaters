using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;

public class VoyageManager : MonoBehaviour {

   #region Public Variables

   // Self
   public static VoyageManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public void Start () {
      // Update the members of the voyage group, if any
      InvokeRepeating("updateVoyageGroupMembers", 0f, 2f);
   }

   public void regenerateVoyageInstances () {
      // At server startup, for dev builds, make all the sea maps accessible by creating one voyage for each
#if !CLOUD_BUILD
      StartCoroutine(CO_CreateInitialVoyages());
#endif

      // Regularly check that there are enough voyage instances open and create more
      InvokeRepeating("createVoyageInstanceIfNeeded", 20f, 10f);
   }

   public void createVoyageInstance () {
      // Get the list of sea maps area keys
      List<string> seaMaps = AreaManager.self.getSeaAreaKeys();

      // If there are no available areas, do nothing
      if (seaMaps.Count == 0) {
         return;
      }

      // Randomly choose an area
      string areaKey = seaMaps[UnityEngine.Random.Range(0, seaMaps.Count)];

      // Create the instance
      createVoyageInstance(areaKey);
   }

   public void createVoyageInstance (string areaKey) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get a new voyage id
         int voyageId = DB_Main.getNewVoyageId();

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Randomize the voyage parameters
            Voyage.Difficulty difficulty = Util.randomEnumStartAt<Voyage.Difficulty>(1);
            bool isPvP = UnityEngine.Random.Range(0, 2) == 0;

            // Create the area instance
            InstanceManager.self.createNewInstance(areaKey, false, true, voyageId, isPvP, difficulty);
         });
      });
   }

   public Voyage getVoyage (int voyageId) {
      // Search the voyage in all the servers we know about
      foreach (Server server in ServerNetwork.self.servers) {
         foreach (Voyage voyage in server.voyages) {
            if (voyage.voyageId == voyageId) {
               return voyage;
            }
         }
      }

      return null;
   }

   public List<Voyage> getAllVoyages () {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (Server server in ServerNetwork.self.servers) {
         foreach (Voyage voyage in server.voyages) {
            voyages.Add(voyage);
         }
      }

      return voyages;
   }

   public bool isVoyageArea (string areaKey) {
      return AreaManager.self.getSeaAreaKeys().Contains(areaKey);
   }

   public static bool isInVoyage (NetEntity entity) {
      return entity != null && entity.voyageGroupId != -1;
   }

   public static bool isVoyageOpenToNewGroups (Voyage voyage, int groupCount) {
      // Calculate the time left until the voyage closes to new groups
      DateTime voyageCreationDate = DateTime.FromBinary(voyage.creationDate);
      TimeSpan timeLeft = voyageCreationDate.AddSeconds(Voyage.INSTANCE_OPEN_DURATION).Subtract(DateTime.UtcNow);

      // Check that the voyage is still in the open phase
      if (timeLeft.TotalSeconds <= 0) {
         return false;
      } else {
         // Check if the maximum number of groups has been reached
         if (groupCount >= Voyage.getMaxGroupsPerInstance(voyage.difficulty)) {
            return false;
         } else {
            return true;
         }
      }
   }

   public void showVoyagePanel (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      // Check if the player is in a voyage group
      if (isInVoyage(entity)) {
         // Associate a new function with the confirmation button
         PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
         PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmWarpToVoyageMap());

         // Show a confirmation panel
         PanelManager.self.confirmScreen.show("Do you want to warp to your current voyage?");
      } else {
         // Get the voyages panel
         PanelManager.self.selectedPanel = Panel.Type.Voyage;
         VoyagePanel panel = (VoyagePanel) PanelManager.self.get(Panel.Type.Voyage);

         // If the panel is not showing, send a request to the server to get the panel data
         if (!panel.isShowing()) {
            panel.displayVoyagesSelection();
         }
      }
   }

   public void confirmWarpToVoyageMap () {
      // Hide the confirm panel
      PanelManager.self.confirmScreen.hide();

      // Warp the player to its voyage
      Global.player.rpc.Cmd_WarpToCurrentVoyageMap();
   }

   public void handleInviteCommand (string inputString) {
      // Split things apart based on spaces
      string[] sections = inputString.Split(' ');

      // Get the user name
      string userName = sections[0];

      // Send the invite request to the server
      Global.player.rpc.Cmd_SendVoyageGroupInvitationToUser(userName);
   }

   public void receiveVoyageInvitation (int voyageGroupId, string inviterName) {
      // Test if the player is already being invited to a voyage
      if (_invitationVoyageGroupId != -1) {
         return;
      }

      // Store the voyage group id
      _invitationVoyageGroupId = voyageGroupId;

      // Associate a new function with the accept button
      PanelManager.self.voyageInviteScreen.acceptButton.onClick.RemoveAllListeners();
      PanelManager.self.voyageInviteScreen.acceptButton.onClick.AddListener(() => acceptVoyageInvitation());

      // Associate a new function with the refuse button
      PanelManager.self.voyageInviteScreen.refuseButton.onClick.RemoveAllListeners();
      PanelManager.self.voyageInviteScreen.refuseButton.onClick.AddListener(() => refuseVoyageInvitation());

      // Show the voyage invite screen
      PanelManager.self.voyageInviteScreen.activate(inviterName);
   }

   public void acceptVoyageInvitation () {
      if (Global.player == null) {
         return;
      }

      if (Global.player.isInBattle()) {
         PanelManager.self.noticeScreen.show("You must exit battle before joining a voyage group");
         return;
      }

      if (isInVoyage(Global.player)) {
         PanelManager.self.noticeScreen.show("You must leave your current group before joining another");
         return;
      }

      // Deactivate the invite panel
      PanelManager.self.voyageInviteScreen.deactivate();

      // Send the join request to the server
      Global.player.rpc.Cmd_AddUserToVoyageGroup(_invitationVoyageGroupId);

      // Clear the invitation group id so that we can receive more invitations
      _invitationVoyageGroupId = -1;
   }

   public void refuseVoyageInvitation () {
      if (_invitationVoyageGroupId != -1) {
         // Deactivate the invite panel
         PanelManager.self.voyageInviteScreen.deactivate();

         // Clear the invitation group id so that we can receive more invitations
         _invitationVoyageGroupId = -1;
      }
   }

   private void updateVoyageGroupMembers () {
      if (Global.player == null || !Global.player.isLocalPlayer) {
         return;
      }

      // Check if the player is not in a group
      if (!isInVoyage(Global.player)) {
         // If the player just left his group, request an update from the server
         if (_visibleGroupMembers.Count > 0) {
            _visibleGroupMembers.Clear();
            Global.player.rpc.Cmd_RequestVoyageGroupMembersFromServer();
         }
         return;
      }

      // Get the list of all entities (visible by this client)
      List<NetEntity> allEntities = EntityManager.self.getAllEntities();

      // Retrieve the group members that we can see
      List<int> visibleGroupMembers = new List<int>();
      foreach (NetEntity entity in allEntities) {
         if (entity != null && entity.voyageGroupId == Global.player.voyageGroupId) {
            visibleGroupMembers.Add(entity.userId);
         }
      }

      // Compare this new list with the latest
      bool hasNewGroupMembers = visibleGroupMembers.Except(_visibleGroupMembers).Any();
      bool hasMissingGroupMembers = _visibleGroupMembers.Except(visibleGroupMembers).Any();

      // If there are differences, request a full group members update from the server
      if (hasNewGroupMembers || hasMissingGroupMembers) {
         Global.player.rpc.Cmd_RequestVoyageGroupMembersFromServer();
      }

      // Save this list for future comparisons
      _visibleGroupMembers = visibleGroupMembers;
   }

   protected void createVoyageInstanceIfNeeded () {
      // Get our server
      Server server = ServerNetwork.self.server;

      // Check that our server is the main server
      if (server == null || server.port != 7777) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the group count in each active voyage
         Dictionary<int, int> voyageToGroupCount = DB_Main.getGroupCountInAllVoyages();

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Get the voyage instances in all known servers
            List<Voyage> allVoyages = getAllVoyages();

            // Count the number of voyages that are still open to new groups
            int openVoyagesCount = 0;
            foreach (Voyage voyage in allVoyages) {
               // Get the number of groups in the voyage instance
               int groupCount;
               if (!voyageToGroupCount.TryGetValue(voyage.voyageId, out groupCount)) {
                  // If the number of groups could not be found, set it to zero
                  groupCount = 0;
               }

               if (isVoyageOpenToNewGroups(voyage, groupCount)) {
                  openVoyagesCount++;
               }
            }

            // If there are missing voyages, create a new one
            if (openVoyagesCount < Voyage.OPEN_VOYAGE_INSTANCES) {
               // Find the server with the least people
               Server bestServer = ServerNetwork.self.getServerWithLeastPlayers();
               D.editorLog("Best server must create voyage intance", Color.green);
               // Create a new voyage instance on the chosen server
               //bestServer.photonView.RPC("CreateVoyageInstance", bestServer.view.owner);
            }
         });
      });
   }

   private IEnumerator CO_CreateInitialVoyages () {
      // Wait until our server is defined
      while (ServerNetwork.self.server == null) {
         yield return null;
      }

      // Wait until our server port is initialized
      while (ServerNetwork.self.server.port == 0) {
         yield return null;
      }

      // Get our server
      Server server = ServerNetwork.self.server;

      // Check that our server is the main server
      if (server.port == 7777) {
         // Get the list of sea maps area keys
         List<string> seaMaps = AreaManager.self.getSeaAreaKeys();

         // Create a voyage instance for each available sea map
         foreach (string areaKey in seaMaps) {
            // Find the server with the least people
            Server bestServer = ServerNetwork.self.getServerWithLeastPlayers();

            D.editorLog("Best server must create voyage intance", Color.green);
            // Create a new voyage instance on the chosen server
            //bestServer.photonView.RPC("CreateVoyageInstance", bestServer.view.owner, areaKey);
         }
      }
   }

   #region Private Variables

   // Keeps track of the group members visible by this client
   private List<int> _visibleGroupMembers = new List<int>();

   // The id of the group the player is being invited to, if any
   private int _invitationVoyageGroupId = -1;

   #endregion
}
