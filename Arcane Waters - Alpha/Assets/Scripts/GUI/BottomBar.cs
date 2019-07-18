using UnityEngine;

public class BottomBar : MonoBehaviour {
   #region Public Variables

   // Self
   public static BottomBar self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void toggleCharacterInfoPanel () {
      CharacterInfoPanel panel = (CharacterInfoPanel) PanelManager.self.get(Panel.Type.CharacterInfo);

      // If the panel is not showing, send a request to the server
      if (!panel.isShowing()) {
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
         panel.requestInventoryFromServer(-1);
      } else {
         PanelManager.self.togglePanel(Panel.Type.Inventory);
      }
   }

   public void toggleCraftingPanel () {
      PanelManager.self.selectedPanel = Panel.Type.Craft;
      CraftingPanel panel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

      // If the panel is not showing, send a request to the server to get our items
      if (!panel.isShowing()) {
         panel.requestInventoryFromServer(1);
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

   #region Private Variables

   #endregion
}
