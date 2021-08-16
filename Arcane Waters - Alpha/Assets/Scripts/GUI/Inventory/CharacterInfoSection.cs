using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CharacterInfoSection : MonoBehaviour
{
   #region Public Variables

   // Our character stack
   public CharacterStack characterStack;

   // The cell containers for the equipped items
   public ItemCellInventory equippedWeaponCell;
   public ItemCellInventory equippedArmorCell;
   public ItemCellInventory equippedHatCell;

   // The text components
   public Text characterNameText;
   public Text levelText;
   public Text hpText;
   public Text xpText;

   // The guild icon
   public GuildIcon guildIcon;

   // The experience progress bar
   public Image levelProgressBar;

   #endregion

   private void clear () {
      // Clear the equipped gear cells
      equippedArmorCell.clear();
      equippedWeaponCell.clear();
      equippedHatCell.clear();

      characterNameText.text = "";
   }

   public void setPlayer (NetEntity player) {
      if (player == null) {
         D.error("Trying to initialize CharacterInfoColumn with a null player.");
         return;
      }

      // Get the player's UserObjects
      UserObjects userObjects = InventoryManager.getUserObjectsForPlayer(player);

      setUserObjects(userObjects);
   }

   public void setUserObjects (UserObjects userObjects) {
      clear();

      characterNameText.text = userObjects.userInfo.username;

      characterStack.updateLayers(userObjects, false);

      updateLevelInfo(userObjects);
      updateHPInfo(userObjects.userInfo.userId, userObjects.userInfo.XP);
      updateEquipmentCells(userObjects);

      guildIcon.initialize(userObjects.guildInfo);
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
         equippedWeaponCell.setCellForItem(weapon);
         equippedWeaponCell.show();
      }

      Item armor = userObjects.armor;
      if (armor.itemTypeId != 0) {
         equippedArmorCell.setCellForItem(armor);
         equippedArmorCell.show();
      }

      Item hat = userObjects.hat;
      if (hat.itemTypeId != 0) {
         equippedHatCell.setCellForItem(hat);
         equippedHatCell.show();
      }
   }

   #region Private Variables

   #endregion
}

