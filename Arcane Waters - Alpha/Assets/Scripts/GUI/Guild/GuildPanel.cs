﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;

public class GuildPanel : Panel {
   #region Public Variables

   // The button for editting ranks in guild
   public Button ranksButton;

   // The button for creating a guild
   public Button createButton;

   // The button for leaving a guild
   public Button leaveButton;

   // The container for our guild member list
   public GameObject memberContainer;

   // The prefab we use for creating a guild member row
   public GuildMemberRow guildMemberPrefab;

   // Our various texts
   public Text nameText;
   public Text dateText;
   public Text levelText;
   public Text countText;

   // Our various images
   public Image flagImage;

   // The guild icon
   public GuildIcon guildIcon;

   // The guild creation panel
   public GuildCreatePanel guildCreatePanel;

   // The guild ranks panel
   public GuildRanksPanel guildRanksPanel;

   [Header("Guild actions")]
   // Additional spacer before action buttons
   public GameObject spacerActionContainer;

   // Contains all action buttons
   public GameObject actionButtonsContainer;

   // The button for promoting guild member to higher rank
   public Button promoteButton;

   // The button for demoting guild member to lower rank
   public Button demoteButton;

   // The button for kicking guild member from guild
   public Button kickButton;

   // Self
   public static GuildPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public void receiveDataFromServer (GuildInfo info, GuildRankInfo[] guildRanks) {
      bool inGuild = Global.player.guildId != 0;

      // Disable and enable buttons and images
      ranksButton.interactable = inGuild;
      createButton.interactable = !inGuild;
      leaveButton.interactable = inGuild;
      guildIcon.gameObject.SetActive(inGuild);
      flagImage.enabled = inGuild;

      // Activates "Create Guild" button or action buttons depending on user being in guild
      createButton.gameObject.SetActive(!inGuild);
      spacerActionContainer.gameObject.SetActive(inGuild);
      actionButtonsContainer.gameObject.SetActive(inGuild);

      // Fill in the texts
      nameText.text = inGuild ? info.guildName : "";
      dateText.text = inGuild ? DateTime.FromBinary(info.creationTime).ToString("MMMM yyyy") : "";
      levelText.text = "Level " + (inGuild ? "1" : "");
      countText.text = inGuild ? ("Members: " + info.guildMembers.Length + " / " + GuildManager.MAX_MEMBERS) : "";

      // Set the guild icon
      if (inGuild) {
         guildIcon.initialize(info);
      }

      // Clear out any old member info
      memberContainer.DestroyChildren();

      _guildMemberRowsReference.Clear();
      if (info.guildMembers != null) {
         foreach (UserInfo member in info.guildMembers) {
            GuildMemberRow memberRow = Instantiate(guildMemberPrefab, memberContainer.transform);
            memberRow.setRowForGuildMember(member, guildRanks);
            _guildMemberRowsReference.Add(memberRow);
         }
      }

      // Fill guild ranks data
      if (guildRanks != null) {
         guildRanksPanel.initialize(guildRanks);
      }

      // Cache local player permissions for GUI purposes
      if (guildRanks != null && Global.player != null) {
         int rankId = -1;
         foreach (UserInfo member in info.guildMembers) {
            if (Global.player.userId == member.userId) {
               rankId = member.guildRankId;
               break;
            }
         }

         // Guild leader has all permissions
         if (rankId == 0) {
            Global.player.guildPermissions = int.MaxValue;
         } else {
            foreach (GuildRankInfo rank in guildRanks) {
               if (rank.id == rankId) {
                  Global.player.guildPermissions = rank.permissions;
                  break;
               }
            }
         }
      }

      // Update buttons interactivity
      checkButtonPermissions();
   }

   public void createGuildPressed () {
      guildCreatePanel.show();
   }

   public void ranksGuildPressed () {
      guildRanksPanel.show();
   }

   public void leaveGuildPressed () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmedLeaveGuild());

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to leave your guild?");
   }

   protected void confirmedLeaveGuild () {
      Global.player.rpc.Cmd_LeaveGuild();

      // Exit all panels
      PanelManager.self.confirmScreen.hide();
      PanelManager.self.unlinkPanel();
   }

   public void checkButtonPermissions () {
      bool isActive = (_guildMemberRowsReference.Find(row => row.highlightRow.activeSelf) != null);
      promoteButton.interactable = isActive && Global.player.canPerformAction(GuildRankInfo.GuildPermission.Promote);
      demoteButton.interactable = isActive && Global.player.canPerformAction(GuildRankInfo.GuildPermission.Demote);
      kickButton.interactable = isActive && Global.player.canPerformAction(GuildRankInfo.GuildPermission.Kick);

      ranksButton.interactable = Global.player.canPerformAction(GuildRankInfo.GuildPermission.EditRanks);
   }

   public List<GuildMemberRow> getGuildMemeberRows () {
      return _guildMemberRowsReference;
   }

   public void promoteButtonClicked () {
      if (Global.player == null) {
         return;
      }

      GuildMemberRow row = _guildMemberRowsReference.Find(x => x.highlightRow.activeSelf);
      if (row != null && !checkIfActionOnSelf(row)) {
         Global.player.rpc.Cmd_PromoteGuildMember(Global.player.userId, row.getUserId(), Global.player.guildId);
      }
   }

   public void demoteButtonClicked () {
      if (Global.player == null) {
         return;
      }

      GuildMemberRow row = _guildMemberRowsReference.Find(x => x.highlightRow.activeSelf);
      if (row != null && !checkIfActionOnSelf(row)) {
         Global.player.rpc.Cmd_DemoteGuildMember(Global.player.userId, row.getUserId(), Global.player.guildId);
      }
   }

   public void kickButtonClicked () {
      if (Global.player == null) {
         return;
      }

      GuildMemberRow row = _guildMemberRowsReference.Find(x => x.highlightRow.activeSelf);
      if (row != null && !checkIfActionOnSelf(row)) {
         Global.player.rpc.Cmd_KickGuildMember(Global.player.userId, row.getUserId(), Global.player.guildId);
      }
   }

   private bool checkIfActionOnSelf (GuildMemberRow row) {
      if (Global.player.userId == row.getUserId()) {
         PanelManager.self.noticeScreen.show("You cannot perform action on yourself!");
         return true;
      }
      return false;
   }

   #region Private Variables

   // References to the objects representing guild members
   private List<GuildMemberRow> _guildMemberRowsReference = new List<GuildMemberRow>();

   #endregion
}
