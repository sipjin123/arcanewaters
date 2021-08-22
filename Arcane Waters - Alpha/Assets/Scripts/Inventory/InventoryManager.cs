using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InventoryManager : MonoBehaviour
{
   #region Public Variables

   // The weapons new characters start with
   public static int[] STARTING_WEAPON_TYPE_IDS = new int[] { 25, 17, 16, 35, 30 };

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
         if (Global.getUserObjects().weapon != null) {
            TutorialManager3.self.checkUneqipHammerStep();
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
      if (itemId <= 0 || Global.player == null) {
         return false;
      }

      if (Global.player is PlayerBodyEntity) {
         PlayerBodyEntity playerBody = (PlayerBodyEntity) Global.player;
         if (playerBody == null) {
            return false;
         }

         if (itemId == playerBody.weaponManager.equippedWeaponId || itemId == playerBody.armorManager.equippedArmorId || itemId == playerBody.hatsManager.equippedHatId) {
            return true;
         }
      }

      if (Global.player is PlayerShipEntity) {
         UserObjects playerUserObj = Global.getUserObjects();
         if (itemId == playerUserObj.weapon.id || itemId == playerUserObj.armor.id || itemId == playerUserObj.hat.id) {
            return true;
         }
      }
        
      return false;
   }

   public static UserObjects getUserObjectsForPlayer (NetEntity player) {
      if (player == null) {
         D.error("Could not get user objects because player is null");
         return null;
      }

      UserObjects objects = new UserObjects();

      if (player is PlayerBodyEntity) {
         PlayerBodyEntity bodyEntity = player.getPlayerBodyEntity();
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(bodyEntity.weaponManager.equipmentDataId);
         objects.weapon = weaponData != null ? WeaponStatData.translateDataToWeapon(weaponData) : new Weapon();
         objects.weapon.itemTypeId = bodyEntity.weaponManager.equipmentDataId;
         objects.weapon.id = bodyEntity.weaponManager.equippedWeaponId;
         objects.weapon.paletteNames = bodyEntity.weaponManager.palettes;

         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(bodyEntity.armorManager.equipmentDataId);
         objects.armor = armorData != null ? ArmorStatData.translateDataToArmor(armorData) : new Armor();
         objects.armor.itemTypeId = bodyEntity.armorManager.equipmentDataId;
         objects.armor.id = bodyEntity.armorManager.equippedArmorId;
         objects.armor.paletteNames = bodyEntity.armorManager.palettes;

         HatStatData hatData = EquipmentXMLManager.self.getHatData(bodyEntity.hatsManager.equipmentDataId);
         objects.hat = hatData != null ? HatStatData.translateDataToHat(hatData) : new Hat();
         objects.hat.itemTypeId = bodyEntity.hatsManager.equipmentDataId;
         objects.hat.id = bodyEntity.hatsManager.equippedHatId;
         objects.hat.paletteNames = bodyEntity.hatsManager.palettes;
      } else if(player is PlayerShipEntity) {
         PlayerShipEntity shipEntity = player.getPlayerShipEntity();
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(shipEntity.weaponType);
         objects.weapon = weaponData != null ? WeaponStatData.translateDataToWeapon(weaponData) : new Weapon();
         objects.weapon.itemTypeId = shipEntity.weaponType;
         objects.weapon.id = shipEntity.weaponType;
         objects.weapon.paletteNames = shipEntity.weaponColors;

         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(shipEntity.armorType);
         objects.armor = armorData != null ? ArmorStatData.translateDataToArmor(armorData) : new Armor();
         objects.armor.itemTypeId = shipEntity.armorType;
         objects.armor.id = shipEntity.armorType;
         objects.armor.paletteNames = shipEntity.armorColors;

         HatStatData hatData = EquipmentXMLManager.self.getHatData(shipEntity.hatType);
         objects.hat = hatData != null ? HatStatData.translateDataToHat(hatData) : new Hat();
         objects.hat.itemTypeId = shipEntity.hatType;
         objects.hat.id = shipEntity.hatType;
         objects.hat.paletteNames = shipEntity.hatColors;
      }

      objects.guildInfo = getGuildInfoForPlayer(player);
      objects.userInfo = getUserInfoForPlayer(player);

      return objects;
   }

   public static GuildInfo getGuildInfoForPlayer (NetEntity player) {
      GuildInfo info = new GuildInfo();
      info.iconBackground = player.guildIconBackground;
      info.iconBackPalettes = player.guildIconBackPalettes;
      info.iconBorder = player.guildIconBorder;
      info.iconSigil = player.guildIconSigil;
      info.iconSigilPalettes = player.guildIconSigilPalettes;
      info.guildId = player.guildId;

      return info;
   }

   public static UserInfo getUserInfoForPlayer (NetEntity player) {
      UserInfo info = new UserInfo();
      info.areaKey = player.areaKey;
      info.gender = player.gender;
      info.XP = player.XP;

      info.bodyType = player.bodyType;
      info.eyesPalettes = player.eyesPalettes;
      info.eyesType = player.eyesType;
      info.hairPalettes = player.hairPalettes;
      info.hairType = player.hairType;

      return info;
   }

   #region Private Variables

   #endregion
}
