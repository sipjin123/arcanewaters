using MapCustomization;
using NubisDataHandling;
using UnityEngine;
using UnityEngine.UI;

public class BottomBar : MonoBehaviour {
   #region Public Variables

   // Self
   public static BottomBar self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Update () {
      if (Global.player == null) {
         return;
      }

      // Disable the bottom bar buttons when the area is being loaded
      if (!AreaManager.self.hasArea(Global.player.areaKey)) {
         if (_areButtonsActive) {
            foreach (Button button in GetComponentsInChildren<Button>()) {
               button.interactable = false;
            }
            _areButtonsActive = false;
         }
      } else {
         if (!_areButtonsActive) {
            foreach (Button button in GetComponentsInChildren<Button>()) {
               button.interactable = true;
            }
            _areButtonsActive = true;
         }
      }
   }

   public void toggleCharacterInfoPanel () {
      CharacterInfoPanel panel = (CharacterInfoPanel) PanelManager.self.get(Panel.Type.CharacterInfo);

      // If the panel is not showing, send a request to the server
      if (!panel.isShowing()) {
         PanelManager.self.pushPanel(Panel.Type.CharacterInfo);
         panel.loadCharacterCache();
         Global.player.rpc.Cmd_RequestCharacterInfoFromServer(Global.player.userId);
      } else {
         PanelManager.self.togglePanel(Panel.Type.CharacterInfo);
      }
   }

   public void toggleInventoryPanel () {
      PanelManager.self.selectedPanel = Panel.Type.Inventory;
      InventoryPanel panel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

      // If the panel is not showing, send a request to the server to get our items
      if (!panel.isShowing()) {
         panel.refreshPanel();
      } else {
         PanelManager.self.togglePanel(Panel.Type.Inventory);
      }
   }

   public void toggleMapPanel () {
      OverworldScreen panel = (OverworldScreen) PanelManager.self.get(Panel.Type.Overworld);

      // If the panel is not showing, send a request to the server to get our exploration data
      if (!panel.isShowing()) {
         PanelManager.self.togglePanel(Panel.Type.Overworld);
      } else {
         PanelManager.self.togglePanel(Panel.Type.Overworld);
      }
   }

   public void toggleGuildPanel () {
      GuildPanel panel = (GuildPanel) PanelManager.self.get(Panel.Type.Guild);

      // If the panel is not showing, send a request to the server to get the info
      if (!panel.isShowing()) {
         Global.player.rpc.Cmd_RequestGuildInfoFromServer();
      } else {
         PanelManager.self.togglePanel(Panel.Type.Guild);
      }
   }

   public void toggleShipsPanel () {
      FlagshipPanel panel = (FlagshipPanel) PanelManager.self.get(Panel.Type.Flagship);

      // If the panel is not showing, send a request to the server to get our ships
      if (!panel.isShowing()) {
         Global.player.rpc.Cmd_RequestShipsFromServer();
      } else {
         PanelManager.self.togglePanel(Panel.Type.Flagship);
      }
   }

   public void toggleOptionsPanel () {
      OptionsPanel panel = (OptionsPanel) PanelManager.self.get(Panel.Type.Options);

      if (!panel.isShowing()) {
         Global.player.rpc.Cmd_RequestOptionsInfoFromServer();
      } else {
         PanelManager.self.togglePanel(Panel.Type.Options);
      }
   }

   public void toggleStorePanel () {
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
         Global.player.rpc.Cmd_RequestTradeHistoryInfoFromServer(0, TradeHistoryPanel.ROWS_PER_PAGE);
      } else {
         PanelManager.self.togglePanel(Panel.Type.TradeHistory);
      }
   }

   public void toggleLeaderBoardsPanel () {
      LeaderBoardsPanel panel = (LeaderBoardsPanel) PanelManager.self.get(Panel.Type.LeaderBoards);

      if (!panel.isShowing()) {
         Global.player.rpc.Cmd_RequestLeaderBoardsFromServer(LeaderBoardsPanel.DEFAULT_PERIOD, Perk.Category.None);
      } else {
         PanelManager.self.togglePanel(Panel.Type.LeaderBoards);
      }
   }

   public void toggleFriendListPanel () {
      FriendListPanel panel = (FriendListPanel) PanelManager.self.get(Panel.Type.FriendList);

      if (!panel.isShowing()) {
         panel.refreshPanel();
      } else {
         PanelManager.self.togglePanel(Panel.Type.FriendList);
      }
   }

   public void toggleAbilityPanel () {
      AbilityPanel panel = (AbilityPanel) PanelManager.self.get(Panel.Type.Ability_Panel);

      if (!panel.isShowing()) {
         NubisDataFetcher.self.fetchUserAbilities();
      } else {
         PanelManager.self.togglePanel(Panel.Type.Ability_Panel);
      }
   }

   public void toggleMailPanel () {
      MailPanel panel = (MailPanel) PanelManager.self.get(Panel.Type.Mail);

      if (!panel.isShowing()) {
         panel.refreshMailList();
      } else {
         PanelManager.self.togglePanel(Panel.Type.Mail);
      }
   }

   #region Private Variables

   // Gets set to true when the bar buttons are interactable
   private bool _areButtonsActive = true;

   #endregion
}
