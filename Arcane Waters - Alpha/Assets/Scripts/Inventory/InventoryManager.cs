using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InventoryManager : MonoBehaviour
{
   #region Public Variables

   // The weapons new characters start with
   public static Dictionary<int, int> STARTING_WEAPON_TYPE_IDS_AND_COUNT = new Dictionary<int, int>() { { 25, 50 }, { 17, 1 }, { 16, 1 }, { 35, 1 }, { 30, 1 } };

   // ID of the hammer item
   public const int HAMMER_ID = 30;

   #endregion

   public static void tryEquipOrUseItem (Item castedItem, Jobs jobsData = null) {
      // If level requirements are met
      int level = LevelUtil.levelForXp(Global.player.XP);
      if (!EquipmentXMLManager.self.isLevelValid(level, castedItem)) {
         int levelRequirement = EquipmentXMLManager.self.equipmentLevelRequirement(castedItem);
         ChatManager.self.addChat("You need to reach Level (" + levelRequirement + ") to equip this item", ChatInfo.Type.System);
         return;
      }

      // Equip the item if it is equippable
      if (castedItem.canBeEquipped()) {
         equipOrUnequipItem(castedItem, jobsData);
         return;
      }

      // Use the item if it is usable
      if (castedItem.canBeUsed()) {
         useItem(castedItem);
         return;
      }
   }

   public static void equipOrUnequipItem (Item castedItem, Jobs jobsData = null) {
      int level = LevelUtil.levelForXp(Global.player.XP);
      InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

      // Check if it's currently equipped or not
      int itemIdToSend = isEquipped(castedItem.id) ? 0 : castedItem.id;

      // If level requirements are met
      if (itemIdToSend != 0 && !EquipmentXMLManager.self.isLevelValid(level, castedItem)) {
         return;
      }

      // Check which type of item we requested to equip/unequip
      if (castedItem.category == Item.Category.Weapon) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.showBlocker(large: true);
         }

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetWeaponId(itemIdToSend);

         // Trigger the tutorial
         if (Global.getUserObjects().weapon != null) {
            TutorialManager3.self.checkUneqipHammerStep();
         }
      } else if (castedItem.category == Item.Category.Armor) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.showBlocker(large: true);
         }

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetArmorId(itemIdToSend);
      } else if (castedItem.category == Item.Category.Hats) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.showBlocker(large: true);
         }

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetHatId(itemIdToSend);
      } else if (castedItem.category == Item.Category.Ring) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.showBlocker(large: true);
         }

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetRingId(itemIdToSend);
      } else if (castedItem.category == Item.Category.Necklace) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.showBlocker(large: true);
         }

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetNecklaceId(itemIdToSend);
      } else if (castedItem.category == Item.Category.Trinket) {
         if (inventoryPanel.isShowing()) {
            inventoryPanel.showBlocker(large: true);
         }

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetTrinketId(itemIdToSend);
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
      PanelManager.self.confirmScreen.show("Are you sure you want to use '" + castedItem.getName() + "'?");
   }

   protected static void confirmUseItem (Item item) {
      PanelManager.self.confirmScreen.hide();
      Global.player.rpc.Cmd_RequestUseItem(item.id, confirmed: true);

      if (InventoryPanel.self.isShowing()) {
         InventoryPanel.self.showBlocker(large: true);
      }
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

      if (InventoryPanel.self.isShowing()) {
         InventoryPanel.self.showBlocker(large: true);
      }
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

         if (itemId == playerBody.weaponManager.equippedWeaponId || itemId == playerBody.armorManager.equippedArmorId || itemId == playerBody.hatsManager.equippedHatId
            || itemId == playerBody.gearManager.equippedRingDbId || itemId == playerBody.gearManager.equippedNecklaceDbId || itemId == playerBody.gearManager.equippedTrinketDbId) {
            return true;
         }
      }

      if (Global.player is PlayerShipEntity) {
         UserObjects playerUserObj = Global.getUserObjects();
         if (itemId == playerUserObj.weapon.id || itemId == playerUserObj.armor.id || itemId == playerUserObj.hat.id
            || (playerUserObj.ring != null && itemId == playerUserObj.ring.id)
            || (playerUserObj.necklace != null && itemId == playerUserObj.necklace.id)
            || (playerUserObj.trinket != null && itemId == playerUserObj.trinket.id)) {
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
         objects.weapon.count = bodyEntity.weaponManager.count;

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
      } else if (player is PlayerShipEntity) {
         PlayerShipEntity shipEntity = player.getPlayerShipEntity();

         WeaponStatData weaponData = EquipmentXMLManager.self.weaponStatList.Find(_ => _.weaponType == shipEntity.weaponType);
         objects.weapon = weaponData != null ? WeaponStatData.translateDataToWeapon(weaponData) : new Weapon();
         objects.weapon.itemTypeId = shipEntity.weaponType;
         objects.weapon.id = shipEntity.weaponType;
         objects.weapon.paletteNames = shipEntity.weaponColors;
         objects.weapon.count = shipEntity.weaponCount;

         ArmorStatData armorData = EquipmentXMLManager.self.armorStatList.Find(_ => _.armorType == shipEntity.armorType);
         objects.armor = armorData != null ? ArmorStatData.translateDataToArmor(armorData) : new Armor();
         objects.armor.itemTypeId = shipEntity.armorType;
         objects.armor.id = shipEntity.armorType;
         objects.armor.paletteNames = shipEntity.armorColors;

         HatStatData hatData = EquipmentXMLManager.self.hatStatList.Find(_ => _.hatType == shipEntity.hatType);
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
      info.guildMapBaseId = player.guildMapBaseId;
      info.guildHouseBaseId = player.guildHouseBaseId;
      info.inventoryId = player.guildInventoryId;

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
