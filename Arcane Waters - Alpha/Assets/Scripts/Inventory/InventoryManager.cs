using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InventoryManager : MonoBehaviour
{
   #region Public Variables

   // The weapons new characters start with
   public static int[] STARTING_WEAPON_TYPE_IDS = new int[] { 34, 17, 16, 35, 30 };

   // ID of the hammer item
   public const int HAMMER_ID = 30;

   #endregion

   public static void tryEquipOrUseItem (Item castedItem) {
      // Equip the item if it is equippable
      if (castedItem.canBeEquipped()) {
         equipOrUnequipItem(castedItem);
         return;
      }

      // Use the item if it is usable
      if (castedItem.canBeUsed()) {
         useItem(castedItem);
         return;
      }
   }

   public static void equipOrUnequipItem (Item castedItem) {
      InventoryPanel inventoryPanel = (InventoryPanel)PanelManager.self.get(Panel.Type.Inventory);

      // Check which type of item we requested to equip/unequip
      if (castedItem.category == Item.Category.Weapon) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.enableLoadBlocker();
         }

         // Check if it's currently equipped or not
         int itemIdToSend = isEquipped(castedItem.id) ? 0 : castedItem.id;

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetWeaponId(itemIdToSend);

         // Trigger the tutorial
         if (Global.getUserObjects().weapon != null && Global.getUserObjects().weapon.itemTypeId == HAMMER_ID) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.UnequipHammer);
         }
      } else if (castedItem.category == Item.Category.Armor) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.enableLoadBlocker();
         }

         // Check if it's currently equipped or not
         int itemIdToSend = isEquipped(castedItem.id) ? 0 : castedItem.id;

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetArmorId(itemIdToSend);
      } else if (castedItem.category == Item.Category.Hats) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.enableLoadBlocker();
         }

         // Check if it's currently equipped or not
         int itemIdToSend = isEquipped(castedItem.id) ? 0 : castedItem.id;

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetHatId(itemIdToSend);
      }
   }

   public static void useItem (Item castedItem) {
      // Show an error panel if the item cannot be used
      if (!castedItem.canBeUsed()) {
         PanelManager.self.noticeScreen.show("This item can not be used.");
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmUseItem(castedItem));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to use your " + castedItem.getName() + "?");
   }

   protected static void confirmUseItem (Item item) {
      PanelManager.self.confirmScreen.hide();
      Global.player.rpc.Cmd_UseItem(item.id);
   }

   public static void trashItem (Item castedItem) {
      // Show an error panel if the item cannot be trashed
      if (!castedItem.canBeTrashed()) {
         PanelManager.self.noticeScreen.show("This item can not be trashed.");
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmTrashItem(castedItem));

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to trash " + castedItem.getName() + "?");
   }

   protected static void confirmTrashItem (Item item) {
      Global.player.rpc.Cmd_DeleteItem(item.id);
   }

   public static bool isEquipped (int itemId) {
      if (itemId <= 0 || Global.getUserObjects() == null) {
         return false;
      }

      if (itemId == Global.getUserObjects().weapon.id || itemId == Global.getUserObjects().armor.id || itemId == Global.getUserObjects().hat.id) {
         return true;
      }

      return false;
   }

   #region Private Variables

   #endregion
}
