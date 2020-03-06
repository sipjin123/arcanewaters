﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class VoyageManager : MonoBehaviour {

   #region Public Variables

   // The area and spawn the user is warped to when leaving a voyage group
   public static string RETURN_AREA_KEY = "Starting Town New Houses";
   public static string RETURN_SPAWN_KEY = "new dock";

   // Self
   public static VoyageManager self;

   #endregion

   void Awake () {
      self = this;

      // Hardcoded data for testing purposes
      //store(new Voyage("Starting Sea Map", Voyage.Difficulty.Easy, true));
      //store(new Voyage("Starting Sea Map", Voyage.Difficulty.Medium, false));
      store(new Voyage("Starting Sea Map", Voyage.Difficulty.Hard, true));
   }

   public void Start () {
      InvokeRepeating("updateVoyageGroupMembers", 0f, 2f);
   }

   public void store (Voyage voyage) {
      _allVoyages.Add(voyage.areaKey, voyage);
   }

   public Voyage getVoyage (string areaKey) {
      if (_allVoyages.ContainsKey(areaKey)) {
         return _allVoyages[areaKey];
      } else {
         D.error("No voyage is defined for area " + areaKey);
         return null;
      }
   }

   public List<Voyage> getAllVoyages () {
      List<Voyage> voyageList = new List<Voyage>(_allVoyages.Values);
      return voyageList;
   }

   public bool isVoyageArea (string areaKey) {
      foreach(Voyage voyage in _allVoyages.Values) {
         if (string.Equals(voyage.areaKey, areaKey)) {
            return true;
         }
      }
      return false;
   }

   public static bool isInVoyage (NetEntity entity) {
      return entity != null && entity.voyageGroupId != -1;
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

   #region Private Variables

   // Keeps track of the voyages, accessible by areaKey
   private Dictionary<string, Voyage> _allVoyages = new Dictionary<string, Voyage>();

   // Keeps track of the group members visible by this client
   private List<int> _visibleGroupMembers = new List<int>();

   // The id of the group the player is being invited to, if any
   private int _invitationVoyageGroupId = -1;

   #endregion
}
