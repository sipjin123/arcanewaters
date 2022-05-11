using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class OutpostPanel : Panel
{
   #region Public Variables

   // Text with guild name
   public Text guildNameText;

   // Our guild icon
   public GuildIcon guildIcon;

   // Image for showing food amount
   public Image foodBarFill;

   // Text for showing food amount
   public Text foodText;

   #endregion

   public void open (Outpost outpost) {
      _outpost = outpost;
      PanelManager.self.linkIfNotShowing(Type.Outpost);

      // Guild name
      guildNameText.text = outpost.guildName;

      // Guild icon
      if (!string.IsNullOrEmpty(outpost.guildIconBackground)) {
         guildIcon.setBackground(outpost.guildIconBackground, outpost.guildIconBackPalettes);
      }
      if (!string.IsNullOrEmpty(outpost.guildIconBorder)) {
         guildIcon.setBorder(outpost.guildIconBorder);
      }
      if (!string.IsNullOrEmpty(outpost.guildIconSigil)) {
         guildIcon.setSigil(outpost.guildIconSigil, outpost.guildIconSigilPalettes);
      }
   }

   public override void Update () {
      base.Update();

      if (NetworkClient.active) {
         if (_outpost != null) {
            foodText.text = "Food: " + Mathf.RoundToInt(_outpost.currentFood) + "/" + Mathf.RoundToInt(_outpost.maxFood);
            foodBarFill.fillAmount = _outpost.maxFood == 0 ? 0 : Mathf.Clamp01(_outpost.currentFood / _outpost.maxFood);
         } else {
            close();
         }
      }
   }

   public void onSupplyButtonClick () {
      // Associate a new function with the select button
      PanelManager.self.itemSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.selectButton.onClick.AddListener(() => supplyItemSelected());

      // Associate a new function with the cancel button
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.AddListener(() => PanelManager.self.itemSelectionScreen.hide());

      // Show the item selection screen
      PanelManager.self.itemSelectionScreen.show(new List<int>(), new List<Item.Category> { Item.Category.Crop }, (i) => {
         if (i == null) {
            return "Food: 0";
         } else {
            return "Food: " + OutpostUtil.getFoodAmountFromItems(i);
         }
      });
   }

   private void supplyItemSelected () {
      // Hide item selection screen
      PanelManager.self.itemSelectionScreen.hide();

      // Get the selected item
      Item selectedItem = ItemSelectionScreen.selectedItem;
      selectedItem.count = ItemSelectionScreen.selectedItemCount;

      // Have the player confirm his choise
      PanelManager.self.confirmScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.cancelButton.onClick.AddListener(() => {
         PanelManager.self.itemSelectionScreen.show();
      });

      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
         confirmSupplyItem(ItemSelectionScreen.selectedItem.id, ItemSelectionScreen.selectedItemCount);
         PanelManager.self.confirmScreen.hide();
      });

      PanelManager.self.confirmScreen.showYesNo(
         "Are you sure you want to supply outpost with " +
         selectedItem.count + " " + selectedItem.getCastItem().getName() + "?");
   }

   private void confirmSupplyItem (int itemId, int count) {
      if (_outpost == null) {
         return;
      }

      Global.player.rpc.Cmd_SupplyOutpost(_outpost.netId, itemId, count);
   }

   #region Private Variables

   // The outpost we represent
   private Outpost _outpost;

   #endregion
}
