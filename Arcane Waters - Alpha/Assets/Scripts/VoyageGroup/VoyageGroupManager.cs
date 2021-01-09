using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MLAPI.Messaging;

public class VoyageGroupManager : MonoBehaviour
{
   #region Public Variables

   // The number of seconds before an offline user is removed from its voyage group
   public static int DELAY_BEFORE_GROUP_REMOVAL = 5 * 60;

   // The number of seconds a user must wait before inviting the same user to a group again
   public static int GROUP_INVITE_MIN_INTERVAL = 60;

   // The white flag the ships use when they are out of PvP and cannot be attacked
   public static string WHITE_FLAG_PALETTE = "ship_flag_white";

   // Self
   public static VoyageGroupManager self;

   #endregion

   void Awake () {
      self = this;
   }

   public void Start () {
      // Update the members of the voyage group, if any
      InvokeRepeating(nameof(updateVoyageGroupMembers), 0f, 2f);
   }

   public void inviteUserToGroup (NetEntity player, string inviteeName) {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Try to retrieve the invitee info
         UserInfo inviteeInfo = DB_Main.getUserInfo(inviteeName);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (inviteeInfo == null) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "The player " + inviteeName + " doesn't exists!");
               return;
            }

            // Prevent spamming invitations
            if (isGroupInvitationSpam(player.userId, inviteeInfo.username)) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "You must wait " + GROUP_INVITE_MIN_INTERVAL.ToString() + " seconds before inviting " + inviteeName + " again!");
               return;
            }

            StartCoroutine(CO_InviteUserToGroup(player, inviteeInfo));
         });
      });
   }

   private IEnumerator CO_InviteUserToGroup (NetEntity player, UserInfo inviteeInfo) {
      // Get the voyage group info
      VoyageGroupInfo voyageGroup = getGroupById(player.voyageGroupId);
      if (voyageGroup == null) {
         // If the player is not in a group, create one
         yield return CO_CreateGroup(player, -1, true);
         voyageGroup = getGroupById(player.voyageGroupId);
      }

      // Check the validity of the request
      if (!voyageGroup.isPrivate) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "Only private groups allow inviting players!");
         yield break;
      }

      VoyageGroupInfo inviteeGroup = getGroupByUser(inviteeInfo.userId);
      if (inviteeGroup != null && voyageGroup.groupId == inviteeGroup.groupId) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, inviteeInfo.username + " already belongs to the group!");
         yield break;
      }

      if (isGroupFull(voyageGroup)) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "Your group is full!");
         yield break;
      }

      // Make sure the invitee is online
      NetworkedServer inviteeServer = ServerNetworkingManager.self.getServerContainingUser(inviteeInfo.userId);
      if (inviteeServer == null) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "Could not find the player " + inviteeInfo.username);
         yield break;
      }

      // Send the invitation
      ServerNetworkingManager.self.sendGroupInvitationNotification(voyageGroup.groupId, player.userId, player.entityName, inviteeInfo.userId);
      ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "The voyage invitation has been sent to " + inviteeInfo.username);

      // Log the invitation to prevent spamming
      logGroupInvitation(player.userId, inviteeInfo.username);
   }

   public void createGroup (NetEntity player, int voyageId, bool isPrivate) {
      StartCoroutine(CO_CreateGroup(player, voyageId, isPrivate));
   }

   private IEnumerator CO_CreateGroup (NetEntity player, int voyageId, bool isPrivate) {
      int userId = player.userId;

      // To avoid duplicate group ids, only the master server generates new ones
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageGroupId();

      while (!response.IsDone) {
         yield return null;
      }
      
      VoyageGroupInfo voyageGroup = new VoyageGroupInfo(response.Value, voyageId, DateTime.UtcNow, !isPrivate, isPrivate);
      voyageGroup.members.Add(userId);

      // Store the group in our server
      ServerNetworkingManager.self.server.voyageGroups.Add(voyageGroup.groupId, voyageGroup);

      // Wait until the data is synchronized
      while(!ServerNetworkingManager.self.server.voyageGroups.ContainsKey(voyageGroup.groupId)) {
         yield return null;
      }

      // If the player is still available (not warping), set its group id
      if (player != null) {
         player.voyageGroupId = voyageGroup.groupId;
      }
   }

   public void addUserToGroup (VoyageGroupInfo voyageGroup, NetEntity player) {
      if (isGroupFull(voyageGroup)) {
         return;
      }

      if (voyageGroup.members.Contains(player.userId)) {
         return;
      }

      voyageGroup.members.Add(player.userId);
      player.voyageGroupId = voyageGroup.groupId;

      // If the group is now complete, disable its quickmatch
      if (voyageGroup.isQuickmatchEnabled) {
         Voyage voyage = VoyageManager.self.getVoyage(voyageGroup.voyageId);
         if (voyage != null && voyageGroup.members.Count >= Voyage.getMaxGroupSize(voyage.difficulty)) {
            voyageGroup.isQuickmatchEnabled = false;
         }
      }

      // Update the data in the server network
      updateGroup(voyageGroup);

      // Send the group composition update to all group members
      StartCoroutine(CO_SendGroupMembersToUser(voyageGroup.groupId));
   }

   public void removeUserFromGroup (VoyageGroupInfo voyageGroup, NetEntity player) {
      player.voyageGroupId = -1;
      removeUserFromGroup(voyageGroup, player.userId);
   }

   public void removeUserFromGroup (VoyageGroupInfo voyageGroup, int userId) {
      voyageGroup.members.Remove(userId);

      // Update the data in the server network
      if (voyageGroup.members.Count <= 0) {
         // If the group is now empty, delete it
         deleteGroup(voyageGroup);
      } else {
         // Reenable the quickmatching if the conditions are met
         if (!voyageGroup.isPrivate && !voyageGroup.isQuickmatchEnabled) {
            Voyage voyage = VoyageManager.self.getVoyage(voyageGroup.voyageId);
            if (voyage != null && voyageGroup.members.Count < Voyage.getMaxGroupSize(voyage.difficulty)) {
               voyageGroup.isQuickmatchEnabled = true;
            }
         }

         updateGroup(voyageGroup);

         // Send the group composition update to all group members
         StartCoroutine(CO_SendGroupMembersToUser(voyageGroup.groupId));
      }
   }

   public void updateGroup (VoyageGroupInfo voyageGroup) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.voyageGroups.ContainsKey(voyageGroup.groupId)) {
            // This will tag the dictionary entry as modified and the changes will be sent to other servers
            server.voyageGroups[voyageGroup.groupId] = voyageGroup;
         }
      }
   }

   public void deleteGroup (VoyageGroupInfo voyageGroup) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.voyageGroups.ContainsKey(voyageGroup.groupId)) {
            server.voyageGroups.Remove(voyageGroup.groupId);
         }
      }
   }

   private IEnumerator CO_SendGroupMembersToUser(int groupId) {
      // Wait a few frames for the changes to synchronize on other servers
      yield return new WaitForSeconds(0.1f);

      VoyageGroupInfo voyageGroup = getGroupById(groupId);
      if (voyageGroup == null) {
         yield break;
      }

      // Send the updated group composition to each group member
      foreach (int userId in voyageGroup.members) {
         ServerNetworkingManager.self.sendVoyageGroupMembersToUser(userId);
      }
   }

   public VoyageGroupInfo getBestGroupForQuickmatch (int voyageId) {
      // Find the oldest incomplete quickmatch group in the given voyage instance
      DateTime bestDate = DateTime.MaxValue;
      VoyageGroupInfo bestGroup = null;
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (VoyageGroupInfo voyageGroup in server.voyageGroups.Values) {
            DateTime creationDate = DateTime.FromBinary(voyageGroup.creationDate);
            if (voyageGroup.voyageId == voyageId && voyageGroup.isQuickmatchEnabled && creationDate < bestDate) {
               bestDate = creationDate;
               bestGroup = voyageGroup;
            }
         }
      }
      return bestGroup;
   }

   public int getGroupCountInVoyage (int voyageId) {
      int count = 0;

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (VoyageGroupInfo voyageGroup in server.voyageGroups.Values) {
            if (voyageGroup.voyageId == voyageId) {
               count++;
            }
         }
      }

      return count;
   }

   public Dictionary<int, int> getGroupCountInAllVoyages () {
      Dictionary<int, int> groupCount = new Dictionary<int, int>();

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (VoyageGroupInfo voyageGroup in server.voyageGroups.Values) {
            if (groupCount.ContainsKey(voyageGroup.voyageId)) {
               groupCount[voyageGroup.voyageId]++;
            } else {
               groupCount.Add(voyageGroup.voyageId, 0);
            }
         }
      }

      return groupCount;
   }

   public static bool isInGroup (NetEntity entity) {
      return entity != null && entity.voyageGroupId != -1;
   }

   public static bool isGroupFull (VoyageGroupInfo voyageGroup) {
      if (voyageGroup.voyageId <= 0) {
         // If the group has not joined a voyage, the limit is the maximum
         if (voyageGroup.members.Count >= Voyage.MAX_PLAYERS_PER_GROUP_HARD) {
            return true;
         }
      } else {
         // If the group has joined a voyage, we enforce the limit set by the voyage difficulty
         Voyage voyage = VoyageManager.self.getVoyage(voyageGroup.voyageId);
         if (voyage != null && voyageGroup.members.Count >= Voyage.getMaxGroupSize(voyage.difficulty)) {
            return true;
         }
      }
      return false;
   }

   public VoyageGroupInfo getGroupById (int groupId) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.voyageGroups.ContainsKey(groupId)) {
            return server.voyageGroups[groupId];
         }
      }
      return null;
   }

   public VoyageGroupInfo getGroupByUser (int userId) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (VoyageGroupInfo voyageGroup in server.voyageGroups.Values) {
            if (voyageGroup.members.Contains(userId)) {
               return voyageGroup;
            }
         }
      }
      return null;
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

   public void requestPrivateGroupCreation () {
      Global.player.rpc.Cmd_CreatePrivateVoyageGroup();
   }

   public void handleInviteCommand (string inputString) {
      // Get the user name
      string[] sections = inputString.Split(' ');
      string userName = sections[0];

      inviteUserToVoyageGroup(userName);
   }

   public void inviteUserToVoyageGroup (string userName) {
      Global.player.rpc.Cmd_SendVoyageGroupInvitationToUser(userName);
   }

   public void receiveGroupInvitation (int voyageGroupId, string inviterName) {
      // Test if the player is already being invited to a voyage
      if (_invitationGroupId != -1) {
         return;
      }

      // Store the voyage group id
      _invitationGroupId = voyageGroupId;
      _inviterName = inviterName;

      // Associate a new function with the accept button
      PanelManager.self.voyageInviteScreen.acceptButton.onClick.RemoveAllListeners();
      PanelManager.self.voyageInviteScreen.acceptButton.onClick.AddListener(() => acceptGroupInvitation());

      // Associate a new function with the refuse button
      PanelManager.self.voyageInviteScreen.refuseButton.onClick.RemoveAllListeners();
      PanelManager.self.voyageInviteScreen.refuseButton.onClick.AddListener(() => refuseGroupInvitation());

      // Show the voyage invite screen
      PanelManager.self.voyageInviteScreen.activate(inviterName);
   }

   public void acceptGroupInvitation () {
      if (Global.player == null) {
         return;
      }

      if (Global.player.isInBattle()) {
         PanelManager.self.noticeScreen.show("You must exit battle before joining a voyage group");
         return;
      }

      if (isInGroup(Global.player)) {
         PanelManager.self.noticeScreen.show("You must leave your current group before joining another");
         return;
      }

      // Send the join request to the server
      Global.player.rpc.Cmd_AcceptGroupInvitation(_invitationGroupId);

      // Deactivate the invite panel
      hideVoyageGroupInvitation();
   }

   public void refuseGroupInvitation () {
      if (_invitationGroupId != -1) {
         hideVoyageGroupInvitation();
      }
   }

   public void hideVoyageGroupInvitation() {
      // Deactivate the invite panel
      PanelManager.self.voyageInviteScreen.deactivate();

      // Clear the invitation group id so that we can receive more invitations
      _invitationGroupId = -1;
   }

   private void updateVoyageGroupMembers () {
      if (Global.player == null || !Global.player.isLocalPlayer) {
         return;
      }

      // Check if the player is in a group
      if (!isInGroup(Global.player)) {
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

   public void sendVoyageGroupMembersToClient (NetEntity player) {
      // If the player is not in a group, send an empty group
      if (!isInGroup(player)) {
         player.rpc.Target_ReceiveVoyageGroupMembers(player.connectionToClient, new int[0]);
         return;
      }

      // Send the data to the client
      VoyageGroupInfo voyageGroup = getGroupById(player.voyageGroupId);
      player.rpc.Target_ReceiveVoyageGroupMembers(player.connectionToClient, voyageGroup.members.ToArray());
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

   private IEnumerator CO_RemoveGroupInvitationLog (int inviterUserId, string inviteeName) {
      yield return new WaitForSeconds(GROUP_INVITE_MIN_INTERVAL);

      if (_groupInvitationsLog.TryGetValue(inviterUserId, out HashSet<string> inviteeLog)) {
         inviteeLog.Remove(inviteeName);
      }
   }

   public void onUserDisconnectsFromServer (int userId) {
      // Stop any existing group removal coroutine
      stopExistingGroupRemovalCoroutine(userId);

      // If the player is not in a group, do nothing
      VoyageGroupInfo voyageGroup = getGroupByUser(userId);
      if (voyageGroup == null) {
         return;
      }

      // Remove the disconnected user from its voyage group after a delay
      Coroutine removalCO = StartCoroutine(CO_RemoveDisconnectedUserFromGroup(userId));

      // Keep a reference to the coroutine
      _pendingRemovalFromGroups.Add(userId, removalCO);
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

   private IEnumerator CO_RemoveDisconnectedUserFromGroup (int userId) {
      // Wait a few minutes in case the user reconnects
      yield return new WaitForSeconds(DELAY_BEFORE_GROUP_REMOVAL);

      // If the player is not in a group, do nothing
      VoyageGroupInfo voyageGroup = getGroupByUser(userId);
      if (voyageGroup == null) {
         yield break;
      }

      // Remove the player from its group
      removeUserFromGroup(voyageGroup, userId);
   }

   public int getNewGroupId () {
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         D.error("New group ids can only be generated by the Master server!");
         return 0;
      }

      return ++_lastVoyageGroupId;
   }

   public static string getShipFlagPalette (int groupId) {
      List<PaletteToolManager.PaletteRepresentation> flagPalettes = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Ship, PaletteDef.Ship.flag.name, PaletteDef.Tags.PvP);
      if (flagPalettes.Count == 0) {
         D.error("Could not find any ship flag palettes for voyage groups!");
         return "";
      }

      int index = groupId % flagPalettes.Count;
      return flagPalettes[index].name;
   }

   #region Private Variables

   // Keeps track of the group members visible by this client
   private List<int> _visibleGroupMembers = new List<int>();

   // Keeps a log of the group invitations to prevent spamming
   private Dictionary<int, HashSet<string>> _groupInvitationsLog = new Dictionary<int, HashSet<string>>();

   // The pending coroutines that remove users from groups after disconnecting
   private Dictionary<int, Coroutine> _pendingRemovalFromGroups = new Dictionary<int, Coroutine>();

   // The id of the group the player is being invited to, if any
   private int _invitationGroupId = -1;

   // The name of the voyage inviter
   private string _inviterName;

   // The last id used to create a voyage group - This can only be incremented by the Master server!
   private int _lastVoyageGroupId = 0;

   #endregion
}
