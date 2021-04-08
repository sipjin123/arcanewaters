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

   // The container for the group member arrows
   public GameObject groupMemberArrowContainer;

   // Self
   public static VoyageGroupManager self;

   #endregion

   void Awake () {
      self = this;
   }

   [Server]
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

            // Check if the invitee is already in a group
            if (tryGetGroupByUser(inviteeInfo.userId, out VoyageGroupInfo inviteeVoyageGroup)) {
               ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "The player " + inviteeName + " is already in a group!");
               return;
            }

            StartCoroutine(CO_InviteUserToGroup(player, inviteeInfo));
         });
      });
   }

   [Server]
   private IEnumerator CO_InviteUserToGroup (NetEntity player, UserInfo inviteeInfo) {
      // Get the voyage group info
      if (!player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         // If the player is not in a group, create one
         yield return CO_CreateGroup(player, -1, true);
         player.tryGetGroup(out voyageGroup);
      }

      // Check the validity of the request
      if (!voyageGroup.isPrivate) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "Only private groups allow inviting players!");
         yield break;
      }

      if (tryGetGroupByUser(inviteeInfo.userId, out VoyageGroupInfo inviteeGroup) && voyageGroup.groupId == inviteeGroup.groupId) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, inviteeInfo.username + " already belongs to the group!");
         yield break;
      }

      if (isGroupFull(voyageGroup, out string groupFullErrorMessage)) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, groupFullErrorMessage);
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

      // Send the confirmation to all online group members
      ServerNetworkingManager.self.sendConfirmationMessageToGroup(ConfirmMessage.Type.General, player.voyageGroupId, player.entityName + " has sent group invitation to " + inviteeInfo.username + "!");

      // Log the invitation to prevent spamming
      logGroupInvitation(player.userId, inviteeInfo.username);
   }

   [Server]
   public void createGroup (NetEntity player, int voyageId, bool isPrivate, bool isGhost = false) {
      StartCoroutine(CO_CreateGroup(player, voyageId, isPrivate, isGhost));
   }

   [Server]
   private IEnumerator CO_CreateGroup (NetEntity player, int voyageId, bool isPrivate, bool isGhost = false) {
      int userId = player.userId;

      // To avoid duplicate group ids, only the master server generates new ones
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageGroupId();

      while (!response.IsDone) {
         yield return null;
      }
      
      VoyageGroupInfo voyageGroup = new VoyageGroupInfo(response.Value, voyageId, DateTime.UtcNow, !isPrivate, isPrivate, isGhost);
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

      // Send the group composition update to all group members
      StartCoroutine(CO_SendGroupCompositionToAllMembers(voyageGroup.groupId));
   }

   [Server]
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
         if (VoyageManager.self.tryGetVoyage(voyageGroup.voyageId, out Voyage voyage) && voyageGroup.members.Count >= Voyage.getMaxGroupSize(voyage.difficulty)) {
            voyageGroup.isQuickmatchEnabled = false;
         }
      }

      // Update the data in the server network
      updateGroup(voyageGroup);

      // Send the confirmation to all online group members
      ServerNetworkingManager.self.sendConfirmationMessageToGroup(ConfirmMessage.Type.General, voyageGroup.groupId, player.entityName + " has joined group!");

      // Send the group composition update to all group members
      StartCoroutine(CO_SendGroupCompositionToAllMembers(voyageGroup.groupId));
   }

   [Server]
   public void removeUserFromGroup (VoyageGroupInfo voyageGroup, NetEntity player) {
      player.voyageGroupId = -1;
      removeUserFromGroup(voyageGroup, player.userId);
      player.rpc.Target_ReceiveVoyageGroupMembers(player.connectionToClient, new VoyageGroupMemberCellInfo[0]);
   }

   [Server]
   public void removeUserFromGroup (VoyageGroupInfo voyageGroup, int userId) {
      voyageGroup.members.Remove(userId);

      // Update the data in the server network
      if (voyageGroup.members.Count <= 0) {
         // If the group is now empty, delete it
         deleteGroup(voyageGroup);
      } else {
         // Reenable the quickmatching if the conditions are met
         if (!voyageGroup.isPrivate && !voyageGroup.isQuickmatchEnabled) {
            if (VoyageManager.self.tryGetVoyage(voyageGroup.voyageId, out Voyage voyage) && voyageGroup.members.Count < Voyage.getMaxGroupSize(voyage.difficulty)) {
               voyageGroup.isQuickmatchEnabled = true;
            }
         }

         updateGroup(voyageGroup);

         // Send the group composition update to all group members
         StartCoroutine(CO_SendGroupCompositionToAllMembers(voyageGroup.groupId));
      }
   }

   [Server]
   public void updateGroup (VoyageGroupInfo voyageGroup) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.voyageGroups.ContainsKey(voyageGroup.groupId)) {
            // This will tag the dictionary entry as modified and the changes will be sent to other servers
            server.voyageGroups[voyageGroup.groupId] = voyageGroup;
         }
      }
   }

   [Server]
   public void deleteGroup (VoyageGroupInfo voyageGroup) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.voyageGroups.ContainsKey(voyageGroup.groupId)) {
            server.voyageGroups.Remove(voyageGroup.groupId);
         }
      }
   }

   [Server]
   private IEnumerator CO_SendGroupCompositionToAllMembers (int groupId) {
      // Wait a few frames for the changes to synchronize on other servers
      yield return new WaitForSeconds(0.1f);

      ServerNetworkingManager.self.sendVoyageGroupCompositionToMembers(groupId);
   }

   [Server]
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

   [Server]
   public void forceAdminJoinVoyage (NetEntity admin, int voyageId) {
      // Check if the admin is already in a voyage group
      if (isInGroup(admin)) {
         if (!admin.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, admin, "Error when retrieving the voyage group!");
            return;
         }

         // If the admin group is already linked to this voyage, do nothing
         if (voyageGroup.voyageId == voyageId) {
            return;
         }

         if (voyageGroup.voyageId > 0) {
            // If the group is already linked to a voyage, make the admin leave and join a new one
            removeUserFromGroup(voyageGroup, admin);
            createGroup(admin, voyageId, true);
         } else {
            // If the group has not joined a voyage, make it join this one
            voyageGroup.voyageId = voyageId;
            updateGroup(voyageGroup);
         }
      } else {
         // If the admin is not in a group, create a new one
         createGroup(admin, voyageId, true);
      }
   }

   [Server]
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

   [Server]
   public bool isAtLeastOneGroupInVoyage (int voyageId) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (VoyageGroupInfo voyageGroup in server.voyageGroups.Values) {
            if (voyageGroup.voyageId == voyageId) {
               return true;
            }
         }
      }

      return false;
   }

   [Server]
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

   [Server]
   public static bool isGroupFull (VoyageGroupInfo voyageGroup) {
      return isGroupFull(voyageGroup, out string errorMessage);
   }

   [Server]
   public static bool isGroupFull (VoyageGroupInfo voyageGroup, out string errorMessage) {
      errorMessage = "Your group is full!";

      // Ghost groups have a single member
      if (voyageGroup.isGhost) {
         errorMessage = "You cannot invite more members while in ghost mode!";
         return true;
      }

      if (voyageGroup.voyageId <= 0) {
         // If the group has not joined a voyage, the limit is the maximum
         if (voyageGroup.members.Count >= Voyage.MAX_PLAYERS_PER_GROUP_HARD) {
            return true;
         }
      } else {
         // If the group has joined a voyage, we enforce the limit set by its parameters
         if (VoyageManager.self.tryGetVoyage(voyageGroup.voyageId, out Voyage voyage)) {
            if (voyage.isLeague) {
               // Once a league is started (after the lobby), the group cannot accept more players
               if (voyage.leagueIndex > 0 || voyageGroup.members.Count >= Voyage.getMaxGroupSize(Voyage.getMaxDifficulty())) {
                  errorMessage = "You cannot invite more members after starting a league!";
                  return true;
               } else {
                  return false;
               }
            } else if (voyageGroup.members.Count >= Voyage.getMaxGroupSize(voyage.difficulty)) {
               return true;
            }
         }
      }
      return false;
   }

   [Server]
   public bool tryGetGroupById (int groupId, out VoyageGroupInfo voyageGroup) {
      voyageGroup = default;

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.voyageGroups.ContainsKey(groupId)) {
            voyageGroup = server.voyageGroups[groupId];
            return true;
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetGroupByUser (int userId, out VoyageGroupInfo voyageGroup) {
      voyageGroup = default;

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (VoyageGroupInfo vGroup in server.voyageGroups.Values) {
            if (vGroup.members.Contains(userId)) {
               voyageGroup = vGroup;
               return true;
            }
         }
      }
      
      return false;
   }

   [Server]
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

   [Server]
   public void sendGroupCompositionToMembers (int groupId) {
      if (!tryGetGroupById(groupId, out VoyageGroupInfo voyageGroup)) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Read in DB the group members info needed to display their portrait
         List<VoyageGroupMemberCellInfo> groupMembersInfo = getGroupMembersInfo(voyageGroup);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the updated group composition to each group member
            foreach (int userId in voyageGroup.members) {
               NetEntity player = EntityManager.self.getEntity(userId);
               if (player != null) {
                  player.rpc.Target_ReceiveVoyageGroupMembers(player.connectionToClient, groupMembersInfo.ToArray());
               }
            }
         });
      });
   }

   // Must be called in the background thread!
   [Server]
   public List<VoyageGroupMemberCellInfo> getGroupMembersInfo (VoyageGroupInfo voyageGroup) {
      // Read in DB the group members info needed to display their portrait
      List<VoyageGroupMemberCellInfo> groupMembersInfo = new List<VoyageGroupMemberCellInfo>();
      foreach (int userId in voyageGroup.members) {
         UserObjects userObjects = DB_Main.getUserObjects(userId);
         VoyageGroupMemberCellInfo memberInfo = new VoyageGroupMemberCellInfo(userObjects);
         groupMembersInfo.Add(memberInfo);
      }

      return groupMembersInfo;
   }

   [Server]
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

   [Server]
   private IEnumerator CO_RemoveGroupInvitationLog (int inviterUserId, string inviteeName) {
      yield return new WaitForSeconds(GROUP_INVITE_MIN_INTERVAL);

      if (_groupInvitationsLog.TryGetValue(inviterUserId, out HashSet<string> inviteeLog)) {
         inviteeLog.Remove(inviteeName);
      }
   }

   [Server]
   public void onUserDisconnectsFromServer (int userId) {
      // Stop any existing group removal coroutine
      stopExistingGroupRemovalCoroutine(userId);

      // If the player is not in a group, do nothing
      if (!tryGetGroupByUser(userId, out VoyageGroupInfo voyageGroup)) {
         return;
      }

      // Remove the disconnected user from its voyage group after a delay
      Coroutine removalCO = StartCoroutine(CO_RemoveDisconnectedUserFromGroup(userId));

      // Keep a reference to the coroutine
      _pendingRemovalFromGroups.Add(userId, removalCO);
   }

   [Server]
   public void onUserConnectsToServer (int userId) {
      // Stop any existing group removal coroutine
      stopExistingGroupRemovalCoroutine(userId);
   }

   [Server]
   private void stopExistingGroupRemovalCoroutine (int userId) {
      if (_pendingRemovalFromGroups.TryGetValue(userId, out Coroutine existingCO)) {
         if (existingCO != null) {
            StopCoroutine(existingCO);
         }
         _pendingRemovalFromGroups.Remove(userId);
      }
   }

   [Server]
   private IEnumerator CO_RemoveDisconnectedUserFromGroup (int userId) {
      // Wait a few minutes in case the user reconnects
      yield return new WaitForSeconds(DELAY_BEFORE_GROUP_REMOVAL);

      // If the player is not in a group, do nothing
      if (!tryGetGroupByUser(userId, out VoyageGroupInfo voyageGroup)) {
         yield break;
      }

      // Remove the player from its group
      removeUserFromGroup(voyageGroup, userId);
   }

   [Server]
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
