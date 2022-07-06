using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MLAPI.Messaging;

public class GroupManager : MonoBehaviour
{
   #region Public Variables

   // The number of seconds before an offline user is removed from its group
   public static int DELAY_BEFORE_GROUP_REMOVAL = 5 * 60;

   // The number of seconds a user must wait before inviting the same user to a group again
   public static int GROUP_INVITE_MIN_INTERVAL = 60;

   // The white flag the ships use when they are out of PvP and cannot be attacked
   public static string WHITE_FLAG_PALETTE = "ship_flag_white";

   // The container for the group member arrows
   public GameObject groupMemberArrowContainer;

   // Self
   public static GroupManager self;

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
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, player, "The player " + inviteeName + " doesn't exist!");
               return;
            }

            // Prevent spamming invitations
            if (isGroupInvitationSpam(player.userId, inviteeInfo.username)) {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, player, "You must wait " + GROUP_INVITE_MIN_INTERVAL.ToString() + " seconds before inviting " + inviteeName + " again!");
               return;
            }

            // Check if the invitee is already in a group
            if (tryGetGroupByUser(inviteeInfo.userId, out Group inviteeGroup)) {
               ServerMessageManager.sendError(ErrorMessage.Type.Misc, player, "The player " + inviteeName + " is already in a group!");
               return;
            }

            StartCoroutine(CO_InviteUserToGroup(player, inviteeInfo));
         });
      });
   }

   [Server]
   private IEnumerator CO_InviteUserToGroup (NetEntity player, UserInfo inviteeInfo) {
      // Get the group info
      if (!player.tryGetGroup(out Group groupInfo)) {
         // If the player is not in a group, create one
         yield return CO_CreateGroup(player.userId, -1, true, null);
         player.tryGetGroup(out groupInfo);
      }

      // Check the validity of the request
      if (!groupInfo.isPrivate) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "Only private groups allow inviting players!");
         yield break;
      }

      if (tryGetGroupByUser(inviteeInfo.userId, out Group inviteeGroup) && groupInfo.groupId == inviteeGroup.groupId) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, inviteeInfo.username + " already belongs to the group!");
         yield break;
      }

      if (isGroupFull(groupInfo, out string groupFullErrorMessage)) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, groupFullErrorMessage);
         yield break;
      }

      if (GroupInstanceManager.self.tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance gI) && GroupInstanceManager.isPOIArea(gI.areaKey)) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "All group members must leave the land area before inviting more!");
         yield break;
      }

      // Make sure the invitee is online
      NetworkedServer inviteeServer = ServerNetworkingManager.self.getServerContainingUser(inviteeInfo.userId);
      if (inviteeServer == null) {
         ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, player, "Could not find the player " + inviteeInfo.username);
         yield break;
      }

      // Send the invitation
      ServerNetworkingManager.self.sendGroupInvitationNotification(groupInfo.groupId, player.userId, player.entityName, inviteeInfo.userId);

      // Send the confirmation to all online group members
      ServerNetworkingManager.self.sendConfirmationMessageToGroup(ConfirmMessage.Type.General, player.groupId, player.entityName + " has sent group invitation to " + inviteeInfo.username + "!");

      // Log the invitation to prevent spamming
      logGroupInvitation(player.userId, inviteeInfo.username);
   }

   [Server]
   public void createGroup (int userId, int groupInstanceId, bool isPrivate, bool isGhost = false) {
      StartCoroutine(CO_CreateGroup(userId, groupInstanceId, isPrivate, null, isGhost));
   }

   [Server]
   public IEnumerator CO_CreateGroup (int userId, int groupInstanceId, bool isPrivate, Action<Group> newGroupAction, bool isGhost = false) {
      // To avoid duplicate group ids, only the master server generates new ones
      RpcResponse<int> response = ServerNetworkingManager.self.getNewGroupId();

      while (!response.IsDone) {
         yield return null;
      }
      
      Group groupInfo = new Group(response.Value, userId, groupInstanceId, DateTime.UtcNow, !isPrivate, isPrivate, isGhost);
      groupInfo.members.Add(userId);

      // Store the group in our server
      ServerNetworkingManager.self.server.groups.Add(groupInfo.groupId, groupInfo);

      // Wait until the data is synchronized
      while(!ServerNetworkingManager.self.server.groups.ContainsKey(groupInfo.groupId)) {
         yield return null;
      }

      // If the player is available in this server, set its group id
      NetEntity playerEntity = EntityManager.self.getEntity(userId);
      if (playerEntity != null) {
         playerEntity.groupId = groupInfo.groupId;
      }

      // Send the group composition update to all group members
      StartCoroutine(CO_SendGroupCompositionToAllMembers(groupInfo.groupId));

      // Execute the custom action with the new group
      newGroupAction?.Invoke(groupInfo);
   }

   [Server]
   public void addUserToGroup (Group groupInfo, int userId, string userName) {
      if (isGroupFull(groupInfo)) {
         return;
      }

      if (groupInfo.members.Contains(userId)) {
         return;
      }

      groupInfo.members.Add(userId);

      // If the player is available in this server, set its group id
      NetEntity playerEntity = EntityManager.self.getEntity(userId);
      if (playerEntity != null) {
         playerEntity.groupId = groupInfo.groupId;
      }

      // If the group is now complete, disable its quickmatch
      if (groupInfo.isQuickmatchEnabled) {
         if (GroupInstanceManager.self.tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance groupInstance) && groupInfo.members.Count >= GroupInstance.getMaxGroupSize(groupInstance.difficulty)) {
            groupInfo.isQuickmatchEnabled = false;
         }
      }

      // Update the data in the server network
      updateGroup(groupInfo);

      // Send the confirmation to all online group members
      ServerNetworkingManager.self.sendConfirmationMessageToGroup(ConfirmMessage.Type.General, groupInfo.groupId, userName + " has joined group!");

      // Send the group composition update to all group members
      StartCoroutine(CO_SendGroupCompositionToAllMembers(groupInfo.groupId));

      // If the group is linked to an instance, the new member will have their powerups cleared
      if (groupInfo.groupInstanceId > 0) {
         PowerupManager.self.clearPowerupsForUser(userId);
      }      
   }

   [Server]
   public void removeUserFromGroup (Group groupInfo, NetEntity player) {
      player.groupId = -1;
      removeUserFromGroup(groupInfo, player.userId);
      player.rpc.Target_ReceiveGroupMembers(player.connectionToClient, new GroupMemberCellInfo[0], groupInfo.groupCreator);
      
      // Clear user powerups on the server
      PowerupManager.self.clearPowerupsForUser(player.userId);
   }

   [Server]
   public void removeUserFromGroup (Group groupInfo, int userId) {
      groupInfo.members.Remove(userId);

      // Update the data in the server network
      if (groupInfo.members.Count <= 0) {
         // If the group is now empty, delete it
         deleteGroup(groupInfo);
      } else {
         // Reenable the quickmatching if the conditions are met
         if (!groupInfo.isPrivate && !groupInfo.isQuickmatchEnabled) {
            if (GroupInstanceManager.self.tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance groupInstance) && groupInfo.members.Count < GroupInstance.getMaxGroupSize(groupInstance.difficulty)) {
               groupInfo.isQuickmatchEnabled = true;
            }
         }

         updateGroup(groupInfo);

         // Send the group composition update to all group members
         StartCoroutine(CO_SendGroupCompositionToAllMembers(groupInfo.groupId));

         // Clear user powerups on the server
         PowerupManager.self.clearPowerupsForUser(userId);
      }
   }

   [Server]
   public void updateGroup (Group groupInfo) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.groups.ContainsKey(groupInfo.groupId)) {
            // This will tag the dictionary entry as modified and the changes will be sent to other servers
            server.groups[groupInfo.groupId] = groupInfo;
         }
      }
   }

   /// <summary>
   /// Note that this function will not update the group composition in clients.
   /// Use it only when the group is empty or when the player will receive the update by other means (for example, by warping)
   /// </summary>
   [Server]
   public void deleteGroup (Group groupInfo) {
      List<int> userList = new List<int>(groupInfo.members);

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.groups.ContainsKey(groupInfo.groupId)) {
            server.groups.Remove(groupInfo.groupId);
         }
      }

      // Clear all the members powerups
      foreach (int userId in userList) {
         PowerupManager.self.clearPowerupsForUser(userId);
      }
   }

   [Server]
   private IEnumerator CO_SendGroupCompositionToAllMembers (int groupId) {
      // Wait a few frames for the changes to synchronize on other servers
      yield return new WaitForSeconds(0.1f);

      ServerNetworkingManager.self.sendGroupCompositionToMembers(groupId);
   }

   [Server]
   public Group getBestGroupForQuickmatch (int groupInstanceId) {
      // Find the oldest incomplete quickmatch group in the given group instance
      DateTime bestDate = DateTime.MaxValue;
      Group bestGroup = null;
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Group groupInfo in server.groups.Values) {
            DateTime creationDate = DateTime.FromBinary(groupInfo.creationDate);
            if (groupInfo.groupInstanceId == groupInstanceId && groupInfo.isQuickmatchEnabled && creationDate < bestDate) {
               bestDate = creationDate;
               bestGroup = groupInfo;
            }
         }
      }
      return bestGroup;
   }

   [Server]
   public void forceAdminJoinGroupInstance (NetEntity admin, int groupInstanceId) {
      // Check if the admin is already in a group
      if (isInGroup(admin)) {
         if (!admin.tryGetGroup(out Group groupInfo)) {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, admin, "Error when retrieving the group!");
            return;
         }

         // If the admin group is already linked to this group instance, do nothing
         if (groupInfo.groupInstanceId == groupInstanceId) {
            return;
         }

         if (groupInfo.groupInstanceId > 0) {
            // If the group is already linked to a group instance, make the admin leave and join a new one
            removeUserFromGroup(groupInfo, admin);
            createGroup(admin.userId, groupInstanceId, true);
         } else {
            // If the group has not joined a group instance, make it join this one
            groupInfo.groupInstanceId = groupInstanceId;
            updateGroup(groupInfo);
         }
      } else {
         // If the admin is not in a group, create a new one
         createGroup(admin.userId, groupInstanceId, true);
      }
   }

   [Server]
   public int getGroupCountInGroupInstance (int groupInstanceId) {
      int count = 0;

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Group groupInfo in server.groups.Values) {
            if (groupInfo.groupInstanceId == groupInstanceId) {
               count++;
            }
         }
      }

      return count;
   }

   [Server]
   public bool isAtLeastOneGroupInGroupInstance (int groupInstanceId) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Group groupInfo in server.groups.Values) {
            if (groupInfo.groupInstanceId == groupInstanceId) {
               return true;
            }
         }
      }

      return false;
   }

   [Server]
   public Dictionary<int, int> getGroupCountInAllGroupInstances () {
      Dictionary<int, int> groupCount = new Dictionary<int, int>();

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Group groupInfo in server.groups.Values) {
            if (groupCount.ContainsKey(groupInfo.groupInstanceId)) {
               groupCount[groupInfo.groupInstanceId]++;
            } else {
               groupCount.Add(groupInfo.groupInstanceId, 0);
            }
         }
      }

      return groupCount;
   }

   public static bool isInGroup (NetEntity entity) {
      return entity != null && entity.groupId != -1;
   }

   [Server]
   public static bool isGroupFull (Group groupInfo) {
      return isGroupFull(groupInfo, out string errorMessage);
   }

   [Server]
   public static bool isGroupFull (Group groupInfo, out string errorMessage) {
      errorMessage = "Your group is full!";

      // Ghost groups have a single member
      if (groupInfo.isGhost) {
         errorMessage = "You cannot invite more members while in ghost mode!";
         return true;
      }

      if (groupInfo.groupInstanceId <= 0) {
         // If the group is not linked to a group instance, the limit is the maximum
         if (groupInfo.members.Count >= GroupInstance.MAX_PLAYERS_PER_GROUP_HARD) {
            return true;
         }
      } else {
         // If the group is linked to a group instance, we enforce the limit set by its parameters
         if (GroupInstanceManager.self.tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance groupInstance)) {
            if (groupInstance.isLeague) {
               if (groupInfo.members.Count >= GroupInstance.getMaxGroupSize(GroupInstance.getMaxDifficulty())) {
                  return true;
               }

               int aliveNPCEnemyCount = groupInstance.aliveNPCEnemyCount;
               int playerCount = groupInstance.playerCount;
               bool hasTreasureSite = false;

               // Find the treasure site (if any) and add the npc enemies and players it contains
               if (GroupInstanceManager.self.tryGetGroupInstance(groupInstance.groupInstanceId, out GroupInstance treasureSite, true)) {
                  if (GroupInstanceManager.isTreasureSiteArea(treasureSite.areaKey)) {
                     hasTreasureSite = true;
                  }

                  aliveNPCEnemyCount += treasureSite.aliveNPCEnemyCount;
                  playerCount += treasureSite.playerCount;
               }

               // More members can be invited if the league instance is cleared of enemies
               if (aliveNPCEnemyCount == 0 && !hasTreasureSite) {
                  return false;
               }

               // More members can be invited if the group members are not in the instance
               if (playerCount <= 0) {
                  return false;
               }                

               errorMessage = "You cannot invite more players while group members are close to danger!";
               return true;
            } else if (GroupInstanceManager.isPOIArea(groupInstance.areaKey)) {
               return groupInfo.members.Count >= GroupInstance.MAX_PLAYERS_PER_GROUP_HARD;
            } else if (!groupInstance.isPvP && groupInfo.members.Count >= GroupInstance.getMaxGroupSize(groupInstance.difficulty)) {
               return true;
            } else if (groupInstance.isPvP && groupInfo.members.Count >= GroupInstance.MAX_PLAYERS_PER_GROUP_PVP) {
               return true;
            }
         }
      }
      return false;
   }

   [Server]
   public bool tryGetGroupById (int groupId, out Group groupInfo) {
      groupInfo = default;

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.groups.ContainsKey(groupId)) {
            groupInfo = server.groups[groupId];
            return true;
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetGroupByUser (int userId, out Group groupInfo) {
      groupInfo = default;

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Group g in server.groups.Values) {
            if (g.members.Contains(userId)) {
               groupInfo = g;
               return true;
            }
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetGroupByGroupInstanceId (int groupInstanceId, out Group groupInfo) {
      groupInfo = default;

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Group g in server.groups.Values) {
            if (g.groupInstanceId == groupInstanceId) {
               groupInfo = g;
               return true;
            }
         }
      }
      
      return false;
   }

   [Server]
   public bool isGuildAllianceInvitationSpam (int guildId, int allyId) {
      // Get the invitee log for this user
      if (_guildAllianceInvitationsLog.TryGetValue(guildId, out HashSet<int> inviteeLog)) {
         if (inviteeLog.Contains(allyId)) {
            return true;
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
      Global.player.rpc.Cmd_CreatePrivateGroup();
   }

   public void handleInviteCommand (string inputString) {
      // Get the user name
      string[] sections = inputString.Split(' ');
      string userName = sections[0];

      inviteUserToGroup(userName);
   }

   public void inviteUserToGroup (string userName) {
      Global.player.rpc.Cmd_SendGroupInvitationToUser(userName);
   }

   public void receiveGroupInvitation (int groupId, string inviterName) {
      // Ignore invite if do not disturb flag is enabled
      if (Global.doNotDisturbEnabled) {
         return;
      }

      // Test if the player is already being invited to a group instance
      if (_invitationGroupId != -1) {
         return;
      }

      // Store the group id
      _invitationGroupId = groupId;
      _inviterName = inviterName;

      // Associate a new function with the accept button
      PanelManager.self.groupInviteScreen.acceptButton.onClick.RemoveAllListeners();
      PanelManager.self.groupInviteScreen.acceptButton.onClick.AddListener(() => acceptGroupInvitation());

      // Associate a new function with the refuse button
      PanelManager.self.groupInviteScreen.refuseButton.onClick.RemoveAllListeners();
      PanelManager.self.groupInviteScreen.refuseButton.onClick.AddListener(() => refuseGroupInvitation());

      // Show the group invite screen
      PanelManager.self.groupInviteScreen.activate(inviterName);
   }

   public void acceptGroupInvitation () {
      if (Global.player == null) {
         return;
      }

      if (Global.player.isInBattle()) {
         PanelManager.self.noticeScreen.show("You must exit battle before joining a group");
         return;
      }

      if (isInGroup(Global.player)) {
         PanelManager.self.noticeScreen.show("You must leave your current group before joining another");
         return;
      }

      // Send the join request to the server
      Global.player.rpc.Cmd_AcceptGroupInvitation(_invitationGroupId);

      // Deactivate the invite panel
      hideGroupInvitation();
   }

   public void refuseGroupInvitation () {
      if (_invitationGroupId != -1) {
         hideGroupInvitation();
      }
   }

   public void hideGroupInvitation() {
      // Deactivate the invite panel
      PanelManager.self.groupInviteScreen.deactivate();

      // Clear the invitation group id so that we can receive more invitations
      _invitationGroupId = -1;
   }

   [Server]
   public void sendGroupCompositionToMembers (int groupId) {
      if (!tryGetGroupById(groupId, out Group groupInfo)) {
         return;
      }

      // Copy the group members to avoid concurrent modifications errors in the background thread
      List<int> groupMemberList = new List<int>(groupInfo.members);

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Read in DB the group members info needed to display their portrait
         List<GroupMemberCellInfo> groupMembersInfo = getGroupMembersInfo(groupMemberList);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Send the updated group composition to each group member
            foreach (int userId in groupInfo.members) {
               NetEntity player = EntityManager.self.getEntity(userId);
               if (player != null) {
                  // Make sure the player entity has the correct group id set
                  player.groupId = groupId;
                  player.rpc.Target_ReceiveGroupMembers(player.connectionToClient, groupMembersInfo.ToArray(), groupInfo.groupCreator);
               }
            }
         });
      });
   }

   // Must be called in the background thread!
   [Server]
   public List<GroupMemberCellInfo> getGroupMembersInfo (List<int> groupMemberList) {
      // Read in DB the group members info needed to display their portrait
      List<GroupMemberCellInfo> groupMembersInfo = new List<GroupMemberCellInfo>();
      foreach (int userId in groupMemberList) {
         UserObjects userObjects = DB_Main.getUserObjects(userId);
         GroupMemberCellInfo memberInfo = new GroupMemberCellInfo(userObjects);
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
   public void logGuildAllianceInvitation (int guildId, int allyId) {
      // Check if the log exists for this inviter
      if (_guildAllianceInvitationsLog.TryGetValue(guildId, out HashSet<int> inviteeLog)) {
         if (!inviteeLog.Contains(allyId)) {
            inviteeLog.Add(allyId);

            // Remove the invitee from the log after the defined interval
            StartCoroutine(CO_RemoveGuildAllianceInvitationLog(guildId, allyId));
         }
      } else {
         // Create the log
         HashSet<int> newInviteeLog = new HashSet<int>();
         newInviteeLog.Add(allyId);
         _guildAllianceInvitationsLog.Add(guildId, newInviteeLog);

         // Remove the invitee from the log after the defined interval
         StartCoroutine(CO_RemoveGuildAllianceInvitationLog(guildId, allyId));
      }
   }
   
   [Server]
   private IEnumerator CO_RemoveGuildAllianceInvitationLog (int guildId, int allyId) {
      yield return new WaitForSeconds(GROUP_INVITE_MIN_INTERVAL);

      if (_guildAllianceInvitationsLog.TryGetValue(guildId, out HashSet<int> inviteeLog)) {
         inviteeLog.Remove(allyId);
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
      if (!tryGetGroupByUser(userId, out Group groupInfo)) {
         return;
      }

      // Remove the disconnected user from its group after a delay
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
      if (!tryGetGroupByUser(userId, out Group groupInfo)) {
         yield break;
      }

      // Remove the player from its group
      removeUserFromGroup(groupInfo, userId);
   }

   [Server]
   public int getNewGroupId () {
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         D.error("New group ids can only be generated by the Master server!");
         return 0;
      }

      return ++_lastGroupId;
   }

   public static string getShipFlagPalette (int groupId) {
      List<PaletteToolManager.PaletteRepresentation> flagPalettes = PaletteToolManager.getColors(PaletteToolManager.PaletteImageType.Ship, PaletteDef.Ship.flag.name, PaletteDef.Tags.PvP);
      if (flagPalettes.Count == 0) {
         D.error("Could not find any ship flag palettes for groups!");
         return "";
      }

      int index = groupId % flagPalettes.Count;
      return flagPalettes[index].name;
   }

   [Server]
   public void setPvpSpawn(int groupId, string spawnName) {
      if (tryGetGroupById(groupId, out Group groupInfo)) {
         groupInfo.pvpSpawn = spawnName;
      }
   }

   #region Private Variables
   
   // Keeps a log of the guild alliance invitations to prevent spamming
   private Dictionary<int, HashSet<int>> _guildAllianceInvitationsLog = new Dictionary<int, HashSet<int>>();

   // Keeps a log of the group invitations to prevent spamming
   private Dictionary<int, HashSet<string>> _groupInvitationsLog = new Dictionary<int, HashSet<string>>();

   // The pending coroutines that remove users from groups after disconnecting
   private Dictionary<int, Coroutine> _pendingRemovalFromGroups = new Dictionary<int, Coroutine>();

   // The id of the group the player is being invited to, if any
   private int _invitationGroupId = -1;

   // The name of the group inviter
   private string _inviterName;

   // The last id used to create a group - This can only be incremented by the Master server!
   private int _lastGroupId = 0;

   #endregion
}
