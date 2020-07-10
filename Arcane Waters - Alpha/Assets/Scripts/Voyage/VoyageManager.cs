using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using ServerCommunicationHandlerv2;

public class VoyageManager : MonoBehaviour {

   #region Public Variables

   // The number of seconds before an offline user is removed from its voyage group
   public static int DELAY_BEFORE_GROUP_REMOVAL = 5 * 60;

   // The number of seconds a user must wait before inviting the same user to a group again
   public static int GROUP_INVITE_MIN_INTERVAL = 60;

   // Self
   public static VoyageManager self;

   #endregion

   void Awake () {
      self = this;

      // Randomize the starting value of the PvP parameter
      _isNewVoyagePvP = UnityEngine.Random.Range(0, 2) == 0;
   }

   public void Start () {
      // Update the members of the voyage group, if any
      InvokeRepeating(nameof(updateVoyageGroupMembers), 0f, 2f);
   }

   public void startVoyageManagement () {
      // At server startup, for dev builds, make all the sea maps accessible by creating one voyage for each
#if !CLOUD_BUILD
      StartCoroutine(CO_CreateInitialVoyages());
#endif

      // At server startup, delete all voyage groups
      StartCoroutine(CO_ClearVoyageGroups());

      // Regularly check that there are enough voyage instances open and create more
      InvokeRepeating(nameof(createVoyageInstanceIfNeeded), 20f, 10f);

      // Regularly update the list of voyage instances hosted by this server
      InvokeRepeating(nameof(updateVoyageInstancesList), 5.5f, 5f);
   }

   public void createVoyageInstance (string areaKey, bool isPvP) {
      // Check if the area is defined
      if (string.IsNullOrEmpty(areaKey)) {
         // Get the list of sea maps area keys
         List<string> seaMaps = getVoyageAreaKeys();

         // If there are no available areas, do nothing
         if (seaMaps.Count == 0) {
            return;
         }

         // Randomly choose an area
         areaKey = seaMaps[UnityEngine.Random.Range(0, seaMaps.Count)];
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get a new voyage id
         int voyageId = DB_Main.getNewVoyageId();

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Randomize the voyage parameters
            Voyage.Difficulty difficulty = Util.randomEnumStartAt<Voyage.Difficulty>(1);

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

   public bool doesVoyageExists(int voyageId) {
      return getVoyage(voyageId) != null;
   }

   public bool isVoyageArea (string areaKey) {
      return getVoyageAreaKeys().Contains(areaKey);
   }

   public List<string> getVoyageAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.Voyage).ToList();
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
         displayWarpToVoyageConfirmScreen();
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

   public void displayWarpToVoyageConfirmScreen () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmWarpToVoyageMap());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Do you want to warp to your current voyage?");
   }

   public void confirmWarpToVoyageMap () {
      // Hide the confirm panel
      PanelManager.self.confirmScreen.hide();

      // Warp the player to its voyage
      Global.player.rpc.Cmd_WarpToCurrentVoyageMap();
   }

   public void handleInviteCommand (string inputString) {
      // Get the user name
      string[] sections = inputString.Split(' ');
      string userName = sections[0];

      invitePlayerToVoyageGroup(userName);
   }

   public void invitePlayerToVoyageGroup (string userName) {
      Global.player.rpc.Cmd_SendVoyageGroupInvitationToUser(userName);
   }

   public void receiveVoyageInvitation (int voyageGroupId, string inviterName) {
      // Test if the player is already being invited to a voyage
      if (_invitationVoyageGroupId != -1) {
         return;
      }

      // Store the voyage group id
      _invitationVoyageGroupId = voyageGroupId;
      _inviterName = inviterName;

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
      Global.player.rpc.Cmd_AddUserToVoyageGroup(_invitationVoyageGroupId, _inviterName);

      // Clear the invitation group id so that we can receive more invitations
      _invitationVoyageGroupId = -1;
   }

   public void refuseVoyageInvitation () {
      if (_invitationVoyageGroupId != -1) {
         // Deactivate the invite panel
         PanelManager.self.voyageInviteScreen.deactivate();

         // Send the decline to the server
         Global.player.rpc.Cmd_DeclineVoyageInvite(_invitationVoyageGroupId, _inviterName, Global.player.userId);

         // Clear the invitation group id so that we can receive more invitations
         _invitationVoyageGroupId = -1;
      }
   }

   public bool isGroupInvitationSpam (int inviterUserId, string inviteeName) {
      // Get the invitee log for this user
      if (_groupInvitationsLog.TryGetValue(inviterUserId, out HashSet<string> inviteeLog)) {
         if (inviteeLog.Contains(inviteeName)) {
            return true;
         }
      }

      return false;
   }

   public void logGroupInvitation (int inviterUserId, string inviteeName) {
      // Check if the log exists for this inviter
      if (_groupInvitationsLog.TryGetValue(inviterUserId, out HashSet<string> inviteeLog)) {
         if (!inviteeLog.Contains(inviteeName)) {
            inviteeLog.Add(inviteeName);

            // Remove the invitee from the log after the defined interval
            StartCoroutine(CO_RemoveGroupInvitationLog(inviterUserId, inviteeName));
         }
      } else {
         // Create the log
         HashSet<string> newInviteeLog = new HashSet<string>();
         newInviteeLog.Add(inviteeName);
         _groupInvitationsLog.Add(inviterUserId, newInviteeLog);

         // Remove the invitee from the log after the defined interval
         StartCoroutine(CO_RemoveGroupInvitationLog(inviterUserId, inviteeName));
      }
   }

   public void onUserDisconnectsFromServer (int userId) {
      // Stop any existing group removal coroutine
      stopExistingGroupRemovalCoroutine(userId);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the current voyage group the user belongs to, if any
         VoyageGroupInfo voyageGroup = DB_Main.getVoyageGroupForMember(userId);

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // If the player is not in a group, do nothing
            if (voyageGroup == null) {
               return;
            }

            // Remove the disconnected user from its voyage group after a delay
            Coroutine removalCO = StartCoroutine(CO_RemoveDisconnectedUserFromVoyageGroup(userId));

            // Keep a reference to the coroutine
            _pendingRemovalFromGroups.Add(userId, removalCO);
         });
      });
   }

   public void onUserConnectsToServer (int userId) {
      // Stop any existing group removal coroutine
      stopExistingGroupRemovalCoroutine(userId);
   }

   private void stopExistingGroupRemovalCoroutine (int userId) {
      if (_pendingRemovalFromGroups.TryGetValue(userId, out Coroutine existingCO)) {
         if (existingCO != null) {
            StopCoroutine(existingCO);
         }
         _pendingRemovalFromGroups.Remove(userId);
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

   private void updateVoyageInstancesList () {
      // Rebuilds the list of voyage instances and their status held in this server
      List<Voyage> allVoyages = new List<Voyage>();

      foreach (Instance instance in InstanceManager.self.getVoyageInstances()) {
         Voyage voyage = new Voyage(instance.voyageId, instance.areaKey, instance.difficulty, instance.isPvP,
            instance.creationDate, instance.treasureSiteCount, instance.capturedTreasureSiteCount);
         allVoyages.Add(voyage);
      }

      ServerCommunicationHandler.self.refreshVoyageInstances(allVoyages);
   }

   protected void createVoyageInstanceIfNeeded () {
      // Get our server
      Server server = ServerNetwork.self.server;

      // Check that our server is the main server
      if (server == null || !server.isMainServer()) {
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

               // Toggle the PvP variable
               _isNewVoyagePvP = !_isNewVoyagePvP;

               // Create a new voyage instance on the chosen server
               List<PendingVoyageCreation> voyageList = new List<PendingVoyageCreation>();
               PendingVoyageCreation newVoyage = new PendingVoyageCreation {
                  serverName = bestServer.deviceName,
                  serverIp = bestServer.ipAddress,
                  id = -1,
                  isPending = true,
                  serverPort = bestServer.port,
                  updateTime = DateTime.UtcNow,
                  areaKey = "",
                  isPvP = _isNewVoyagePvP
               };
               voyageList.Add(newVoyage);
               SharedServerDataHandler.self.createVoyage(voyageList);
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
      List<PendingVoyageCreation> pendingVoyageList = new List<PendingVoyageCreation>();

      // Check that our server is the main server
      if (server.isMainServer()) {
         // Get the list of sea maps area keys
         List<string> seaMaps = getVoyageAreaKeys();

         // Create a voyage instance for each available sea map
         foreach (string areaKey in seaMaps) {
            // Find the server with the least people
            Server bestServer = ServerNetwork.self.getServerWithLeastPlayers();

            // Toggle the PvP variable
            _isNewVoyagePvP = !_isNewVoyagePvP;

            if (bestServer != null) {
               // Create a new voyage instance on the chosen server
               pendingVoyageList.Add(new PendingVoyageCreation {
                  id = -1,
                  areaKey = areaKey,
                  isPvP = _isNewVoyagePvP,
                  isPending = true,
                  serverIp = bestServer.ipAddress,
                  serverName = bestServer.deviceName,
                  serverPort = bestServer.port,
                  updateTime = DateTime.UtcNow
               });
            } else {
               D.editorLog("Could not find best server!", Color.red);
            }
         }

         if (pendingVoyageList.Count > 0) {
            SharedServerDataHandler.self.createVoyage(pendingVoyageList);
         }
      }
   }

   private IEnumerator CO_ClearVoyageGroups () {
      // Wait until our server is defined
      while (ServerNetwork.self.server == null) {
         yield return null;
      }

      // Get our server
      Server server = ServerNetwork.self.server;

      // Wait until our server port is initialized
      while (server.port == 0) {
         yield return null;
      }

      // Check that our server is the main server
      if (!server.isMainServer()) {
         yield break;
      }

      // Get the location of the starting spawn
      Vector2 startingSpawnLocalPos = SpawnManager.self.getLocalPosition(Area.STARTING_TOWN, Spawn.STARTING_SPAWN);

      // Get the name of this computer
      string deviceName = SystemInfo.deviceName;

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get all users, members of groups created by this computer
         List<int> groupMembers = DB_Main.getAllVoyageGroupMembersForDevice(deviceName);

         // Delete the groups and group members created by this computer
         DB_Main.deleteAllVoyageGroups(deviceName);

         // Do additional updates on the users after being removed from the group
         foreach (int userId in groupMembers) {
            checkUserConsistencyAfterRemovalFromGroup(userId, startingSpawnLocalPos);
         }
      });
   }

   private IEnumerator CO_RemoveGroupInvitationLog (int inviterUserId, string inviteeName) {
      yield return new WaitForSeconds(GROUP_INVITE_MIN_INTERVAL);

      if (_groupInvitationsLog.TryGetValue(inviterUserId, out HashSet<string> inviteeLog)) {
         inviteeLog.Remove(inviteeName);
      }
   }

   private IEnumerator CO_RemoveDisconnectedUserFromVoyageGroup (int userId) {
      // Wait a few minutes in case the user reconnects
      yield return new WaitForSeconds(DELAY_BEFORE_GROUP_REMOVAL);

      // Get the location of the starting spawn
      Vector2 startingSpawnLocalPos = SpawnManager.self.getLocalPosition(Area.STARTING_TOWN, Spawn.STARTING_SPAWN);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the current voyage group the user belongs to, if any
         VoyageGroupInfo voyageGroup = DB_Main.getVoyageGroupForMember(userId);

         // If the player is not in a group, do nothing
         if (voyageGroup == null) {
            return;
         }

         // Remove the player from its group
         DB_Main.deleteMemberFromVoyageGroup(userId);

         // Update the group member count
         voyageGroup.memberCount--;

         // If the group is now empty, delete it
         if (voyageGroup.memberCount <= 0) {
            DB_Main.deleteVoyageGroup(voyageGroup.groupId);
         }

         // Do additional updates on the user after being removed from the group
         checkUserConsistencyAfterRemovalFromGroup(userId, startingSpawnLocalPos);
      });
   }

   // Must be called in the background thread!
   private void checkUserConsistencyAfterRemovalFromGroup (int userId, Vector2 startingSpawnLocalPos) {
      // Get the user objects
      UserObjects userObjects = DB_Main.getUserObjects(userId);

      // If the user is in a voyage area, move him to the starting town
      if (isVoyageArea(userObjects.userInfo.areaKey)) {
         DB_Main.setNewLocalPosition(userId, startingSpawnLocalPos, Direction.South, Area.STARTING_TOWN);
      }

      // If the user's ship was killed, restore its hp
      if (userObjects.shipInfo.health <= 0) {
         DB_Main.storeShipHealth(userObjects.shipInfo.shipId, userObjects.shipInfo.maxHealth);
      }
   }

   #region Private Variables

   // Keeps track of the group members visible by this client
   private List<int> _visibleGroupMembers = new List<int>();

   // The pending coroutines that remove users from groups after disconnecting
   private Dictionary<int, Coroutine> _pendingRemovalFromGroups = new Dictionary<int, Coroutine>();

   // The id of the group the player is being invited to, if any
   private int _invitationVoyageGroupId = -1;

   // The name of the voyage inviter
   private string _inviterName;

   // Gets toggled every time a new voyage is created, to ensure an equal number of PvP and PvE instances
   private bool _isNewVoyagePvP = false;

   // Keeps a log of the group invitations to prevent spamming
   private Dictionary<int, HashSet<string>> _groupInvitationsLog = new Dictionary<int, HashSet<string>>();

   #endregion
}
