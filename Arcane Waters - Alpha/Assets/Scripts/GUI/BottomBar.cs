﻿using MapCustomization;
using NubisDataHandling;
using UnityEngine;
using UnityEngine.UI;

public class BottomBar : MonoBehaviour {
   #region Public Variables

   // Self
   public static BottomBar self;

   // The friend list panel button when there is no pending request
   public GameObject normalFriendListPanelButton;

   // The friend list panel button when there is a pending request
   public GameObject pendingFriendListPanelButton;

   // The mail panel button when there are no unread mails
   public GameObject normalMailPanelButton;

   // The mail panel button where there are unread mails
   public GameObject pendingMailPanelButton;

   #endregion

   private void Awake () {
      self = this;
      setFriendshipRequestNotificationStatus(false);
      setUnreadMailNotificationStatus(false);
   }

   private void Update () {
      if (Global.player == null) {
         return;
      }

      // Disable the bottom bar buttons when the area is being loaded
      if (!AreaManager.self.hasArea(Global.player.areaKey)) {
         if (_areButtonsActive) {
            foreach (Button button in GetComponentsInChildren<Button>(true)) {
               button.interactable = false;
            }
            _areButtonsActive = false;
         }
      } else {
         if (!_areButtonsActive) {
            foreach (Button button in GetComponentsInChildren<Button>(true)) {
               button.interactable = true;
            }
            _areButtonsActive = true;
         }
      }
   }

   public void setFriendshipRequestNotificationStatus (bool active) {
      pendingFriendListPanelButton.SetActive(active);
      normalFriendListPanelButton.SetActive(!active);
   }

   public void setUnreadMailNotificationStatus (bool active) {
      pendingMailPanelButton.SetActive(active);
      normalMailPanelButton.SetActive(!active);
   }

   public void toggleInventoryPanel () {
      PanelManager.self.selectedPanel = Panel.Type.Inventory;
      InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

      // If the panel is not showing, send a request to the server to get our items
      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();
            panel.onTabButtonPress();
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.Inventory);
      }
   }

   public void toggleMapPanel () {
      WorldMapPanel panel = (WorldMapPanel) PanelManager.self.get(Panel.Type.WorldMap);

      // If the panel is not showing, send a request to the server to get our exploration data
      if (!panel.isShowing()) {
         if (Global.player != null) {
            // Open map sfx
            SoundEffectManager.self.playFmodSfx(SoundEffectManager.MAP_OPEN);

            panel.displayMap();
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.WorldMap);
      }
   }

   public void toggleGuildPanel () {
      GuildPanel panel = (GuildPanel) PanelManager.self.get(Panel.Type.Guild);

      // If the panel is not showing, send a request to the server to get the info
      if (!panel.isShowing()) {

         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            Global.player.rpc.Cmd_RequestGuildInfoFromServer(true);
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.Guild);
      }
   }

   public void enablePvpStatPanel () {
      if (Global.player == null) {
         return;
      }

      // Get the panel
      PvpStatPanel panel = (PvpStatPanel) PanelManager.self.get(Panel.Type.PvpScoreBoard);
      panel.toggleShowWindowValid(true);
      
      if (!panel.isShowing()) {
         Global.player.rpc.Cmd_RequestPvpStatPanel();
      }
   }

   public void disablePvpStatPanel () {
      // Get the panel
      PvpStatPanel panel = (PvpStatPanel) PanelManager.self.get(Panel.Type.PvpScoreBoard);
      
      // Toggle show validity to false to ensure response from server wont show panel after button is released
      panel.toggleShowWindowValid(false);
      
      if (panel.isShowing() && !panel.isGameEnded) {
         PanelManager.self.togglePanel(Panel.Type.PvpScoreBoard);
      }
   }

   public void toggleShipsPanel () {
      FlagshipPanel panel = (FlagshipPanel) PanelManager.self.get(Panel.Type.Flagship);

      // If the panel is not showing, send a request to the server to get our ships
      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            if (Global.player) {
               Global.player.rpc.Cmd_RequestShipsFromServer();
            }
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.Flagship);
      }
   }

   public void toggleOptionsPanel () {
      OptionsPanel panel = (OptionsPanel) PanelManager.self.get(Panel.Type.Options);

      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            Global.player.rpc.Cmd_RequestOptionsInfoFromServer();
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.Options);
      }
   }

   public void toggleStorePanel () {
      if (Global.player == null) {
         return;
      }

      // Makes the Store accessible only to admin players
      if (!Global.player.isAdmin()) {
         return;
      }

      StoreScreen panel = (StoreScreen) PanelManager.self.get(Panel.Type.Store);

      if (!panel.isShowing()) {
         Global.player.rpc.Cmd_RequestStoreFromServer();
      } else {
         PanelManager.self.togglePanel(Panel.Type.Store);
      }
   }

   public void toggleTradeHistoryPanel () {
      TradeHistoryPanel panel = (TradeHistoryPanel) PanelManager.self.get(Panel.Type.TradeHistory);

      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            Global.player.rpc.Cmd_RequestTradeHistoryInfoFromServer(0, TradeHistoryPanel.ROWS_PER_PAGE);
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.TradeHistory);
      }
   }

   public void toggleLeaderBoardsPanel () {
      LeaderBoardsPanel panel = (LeaderBoardsPanel) PanelManager.self.get(Panel.Type.LeaderBoards);

      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(LeaderBoardsPanel.DEFAULT_PERIOD);
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.LeaderBoards);
      }
   }

   public void toggleFriendVisitPanel () {
      VisitListPanel panel = (VisitListPanel) PanelManager.self.get(Panel.Type.VisitPanel);

      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            panel.refreshPanel();
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.VisitPanel);
      }
   }

   public void toggleFriendListPanel () {
      toggleFriendListPanelAtTab(FriendListPanel.FriendshipPanelTabs.Friends);
   }

   public void toggleFriendListPanelAtTab (FriendListPanel.FriendshipPanelTabs desiredTab) {
      FriendListPanel panel = (FriendListPanel) PanelManager.self.get(Panel.Type.FriendList);

      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();
            PanelManager.self.linkPanel(panel.type);
            panel.refreshPanel(true, desiredTab);
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.FriendList);
      }
   }

   public void toggleAbilityPanel () {
      AbilityPanel panel = (AbilityPanel) PanelManager.self.get(Panel.Type.Ability_Panel);

      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            NubisDataFetcher.self.fetchUserAbilities();
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.Ability_Panel);
      }
   }

   public void toggleMailPanel () {
      MailPanel panel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

      if (!panel.isShowing()) {
         if (Global.player != null) {
            SoundEffectManager.self.playGuiMenuOpenSfx();

            panel.refreshMailList();
         }
      } else {
         PanelManager.self.togglePanel(Panel.Type.Mail);
      }
   }

   #region Private Variables

   // Gets set to true when the bar buttons are interactable
   private bool _areButtonsActive = true;

   #endregion
}
