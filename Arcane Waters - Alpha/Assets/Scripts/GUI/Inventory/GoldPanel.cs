using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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
         canvasGroup.Show();
      } else {
         canvasGroup.Hide();
      }
   }

   #region Private Variables

   // The gold amount currently being displayed
   private int _displayedGold = -1;

   #endregion
}
