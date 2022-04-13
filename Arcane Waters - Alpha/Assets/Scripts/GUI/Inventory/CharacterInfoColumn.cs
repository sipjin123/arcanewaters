using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CharacterInfoColumn : MonoBehaviour {
   #region Public Variables

   // Our character stack
   public CharacterStack characterStack;

   // The cell containers for the equipped items
   public ItemCellInventory equippedWeaponCell;
   public ItemCellInventory equippedArmorCell;
   public ItemCellInventory equippedHatCell;
   public ItemCellInventory equippedRingCell;
   public ItemCellInventory equippedNecklaceCell;
   public ItemCellInventory equippedTrinketCell;

   // The text components
   public Text characterNameText;
   public Text goldText;
   public Text gemsText;
   public Text levelText;
   public Text hpText;
   public Text xpText;

   // The guild icon
   public GuildIcon guildIcon;

   // The experience progress bar
   public Image levelProgressBar;

   // The player we're currently displaying
   [HideInInspector]
   public NetEntity currentPlayer;

   // Whether the player we're displaying is the local player
   public bool isDisplayingLocalPlayer => currentPlayer != null && Global.player != null && currentPlayer == Global.player;

   // The equipment stats
   public EquipmentStatsGrid equipmentStats;

   #endregion

   private void Awake () {
   }

   private void clear () {
      // Clear the equipped gear cells
      equippedArmorCell.clear();
      equippedWeaponCell.clear();
      equippedHatCell.clear();
      if (equippedRingCell != null) {
         equippedRingCell.clear();
      }
      if (equippedNecklaceCell != null) {
         equippedNecklaceCell.clear();
      }
      if (equippedTrinketCell != null) {
         equippedTrinketCell.clear();
      }

      equipmentStats.clearAll();
            
      characterStack.gameObject.SetActive(false);

      goldText.text = "";
      gemsText.text = "";
      characterNameText.text = "";
   }

   public void setPlayer (NetEntity player) {
      if (player == null) {
         D.error("Trying to initialize CharacterInfoColumn with a null player.");         
         return;
      }

      clear();

      currentPlayer = player;

      // Set the player name
      characterNameText.text = player.entityName;

      // Get the player's UserObjects
      UserObjects userObjects = getUserObjectsForPlayer(player);
      PlayerBodyEntity bodyEntity = player.getPlayerBodyEntity();

      if (bodyEntity != null) {
         characterStack.gameObject.SetActive(true);
         characterStack.updateLayers(bodyEntity);
      }

      updateGoldAndGems(player);
      updateLevelInfo(userObjects);
      updateHPInfo(player.userId, player.XP);
      updateEquipmentCells(userObjects);

      // Initialize the guild icon
      guildIcon.initialize(userObjects.guildInfo);      
   }

   private void updateGoldAndGems (NetEntity player) {
      if (Global.player != null && player == Global.player) {
         // Update the gold and gems count, with commas in thousands place
         goldText.text = string.Format("{0:n0}", Global.lastUserGold);
         gemsText.text = string.Format("{0:n0}", Global.lastUserGems);
      }
   }

   private void updateLevelInfo (UserObjects userObjects) {
      // Update the level and level progress bar
      int currentLevel = LevelUtil.levelForXp(userObjects.userInfo.XP);
      levelProgressBar.fillAmount = (float) LevelUtil.getProgressTowardsCurrentLevel(userObjects.userInfo.XP) / (float) LevelUtil.xpForLevel(currentLevel + 1);
      levelText.text = "LVL " + currentLevel;
      xpText.text = "EXP: " + LevelUtil.getProgressTowardsCurrentLevel(userObjects.userInfo.XP) + " / " + LevelUtil.xpForLevel(currentLevel + 1);
   }

   private void updateHPInfo (int userId, int xp) {
      // Update the HP bar
      Battler playerBattler = BattleManager.self.getBattler(userId);

      if (playerBattler != null) {
         hpText.text = playerBattler.health.ToString();
      } else {
         BattlerData battData = MonsterManager.self.getBattlerData(Enemy.Type.PlayerBattler);
         int level = LevelUtil.levelForXp(xp);
         int health = (int) battData.baseHealth + ((int) battData.healthPerlevel * level);

         hpText.text = health.ToString();
      }
   }

   private void updateEquipmentCells (UserObjects userObjects) {
      Item weapon = userObjects.weapon;      
      if (weapon.itemTypeId != 0) {
         Weapon castedWeapon = Weapon.castItemToWeapon(weapon);
         equippedWeaponCell.setCellForItem(castedWeapon);
         equippedWeaponCell.show();

         equipmentStats.refreshStats(castedWeapon);
      }

      Item armor = userObjects.armor;
      if (armor.itemTypeId != 0) {
         Armor castedArmor = Armor.castItemToArmor(armor);
         equippedArmorCell.setCellForItem(castedArmor);
         equippedArmorCell.show();

         equipmentStats.refreshStats(castedArmor);
      }

      Item hat = userObjects.hat;
      if (hat.itemTypeId != 0) {
         Hat castedHat = Hat.castItemToHat(hat);
         equippedHatCell.setCellForItem(castedHat);
         equippedHatCell.show();

         if (castedHat.getHatDefense() > 0) {
            equipmentStats.refreshStats(castedHat);
         }
      }
   }

   private UserObjects getUserObjectsForPlayer (NetEntity player) {
      if (player == null) {
         D.error("Could not get user objects because player is null");
         return null;
      }

      UserObjects objects = new UserObjects();

      PlayerBodyEntity bodyEntity = player.getPlayerBodyEntity();
      if (bodyEntity != null) {
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(bodyEntity.weaponManager.equipmentDataId);         
         objects.weapon = weaponData != null ? WeaponStatData.translateDataToWeapon(weaponData) : new Weapon();
         objects.weapon.itemTypeId = bodyEntity.weaponManager.equipmentDataId;
         objects.weapon.id = bodyEntity.weaponManager.equippedWeaponId;
         objects.weapon.paletteNames = bodyEntity.weaponManager.palettes;

         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(bodyEntity.armorManager.armorType);         
         objects.armor = armorData != null ? ArmorStatData.translateDataToArmor(armorData) : new Armor();
         objects.armor.itemTypeId = bodyEntity.armorManager.equipmentDataId;
         objects.armor.id = bodyEntity.armorManager.equippedArmorId;
         objects.armor.paletteNames = bodyEntity.armorManager.palettes;

         HatStatData hatData = EquipmentXMLManager.self.getHatData(bodyEntity.hatsManager.hatType);         
         objects.hat = hatData != null ? HatStatData.translateDataToHat(hatData) : new Hat();
         objects.hat.itemTypeId = bodyEntity.hatsManager.equipmentDataId;
         objects.hat.id = bodyEntity.hatsManager.equippedHatId;
         objects.hat.paletteNames = bodyEntity.hatsManager.palettes;
      }

      if (currentPlayer is PlayerShipEntity) {
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(characterStack.weaponFrontLayer.getType());
         objects.weapon = weaponData != null ? WeaponStatData.translateDataToWeapon(weaponData) : new Weapon();
         if (weaponData != null) {
            objects.weapon.itemTypeId = characterStack.weaponFrontLayer.getType();
            objects.weapon.id = weaponData.sqlId;
         }

         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(characterStack.armorLayer.getType());
         objects.armor = armorData != null ? ArmorStatData.translateDataToArmor(armorData) : new Armor();
         if (armorData != null) {
            objects.armor.itemTypeId = characterStack.armorLayer.getType();
            objects.armor.id = armorData.sqlId;
         }

         HatStatData hatData = EquipmentXMLManager.self.getHatData(characterStack.armorLayer.getType());
         objects.hat = hatData != null ? HatStatData.translateDataToHat(hatData) : new Hat();
         if (hatData != null) {
            objects.hat.itemTypeId = characterStack.armorLayer.getType();
            objects.hat.id = hatData.sqlId;
         }
      }

      objects.guildInfo = getGuildInfoForPlayer(player);
      objects.userInfo = getUserInfoForPlayer(player);

      return objects;
   }

   private GuildInfo getGuildInfoForPlayer (NetEntity player) {
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

   private UserInfo getUserInfoForPlayer (NetEntity player) {
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
