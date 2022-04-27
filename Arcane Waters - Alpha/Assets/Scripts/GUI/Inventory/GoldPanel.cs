using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class GoldPanel : ClientMonoBehaviour
{
   #region Public Variables

   // The panels that display the user gold
   public static HashSet<Panel.Type> PANELS_DISPLAYING_GOLD = new HashSet<Panel.Type> {
      Panel.Type.Adventure, Panel.Type.Auction, Panel.Type.Merchant, Panel.Type.Shipyard, Panel.Type.Mail
   };

   // The user gold
   public Text goldText;

   // The canvas group component
   public CanvasGroup canvasGroup;

   // Reference to group rect transform
   public RectTransform rectTransform;

   // Self
   public static GoldPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      this.enabled = !Util.isBatchServer();
      self = this;
   }

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      // Update the displayed gold amount if it has changed
      if (Global.lastUserGold != _displayedGold) {
         _displayedGold = Global.lastUserGold;
         goldText.text = string.Format("{0:n0}", Global.lastUserGold);
      }

      // Show the panel when relevant
      Panel currentPanel = PanelManager.self.currentPanel();
      if (currentPanel != null && PANELS_DISPLAYING_GOLD.Contains(currentPanel.type)) {
         showGoldPanel();
      } else {
         canvasGroup.Hide();
      }
   }

   private void showGoldPanel () {
      // Only invoke show when panel is hidden
      if (!canvasGroup.IsShowing()) {
         // Updating the gold panel width so we can avoid manually updating the gold panel width in scene when button is added/removed in bottom hud
         float width = BottomBar.self.GetComponent<RectTransform>().rect.size.x;
         Vector2 size = rectTransform.rect.size;
         size.x = width;
         rectTransform.sizeDelta = size;
            
         canvasGroup.Show();
      }
   }

   #region Private Variables

   // The gold amount currently being displayed
   private int _displayedGold = -1;

   #endregion
}
