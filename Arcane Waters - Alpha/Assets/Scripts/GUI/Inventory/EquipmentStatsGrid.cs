using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EquipmentStatsGrid : MonoBehaviour {
   #region Public Variables

   // The stat rows for each element
   public InventoryStatRow physicalStatRow;
   public InventoryStatRow fireStatRow;
   public InventoryStatRow earthStatRow;
   public InventoryStatRow airStatRow;
   public InventoryStatRow waterStatRow;

   #endregion

   public void refreshStats (Weapon equippedWeapon) {
      physicalStatRow.setEquippedWeapon(equippedWeapon);
      fireStatRow.setEquippedWeapon(equippedWeapon);
      earthStatRow.setEquippedWeapon(equippedWeapon);
      airStatRow.setEquippedWeapon(equippedWeapon);
      waterStatRow.setEquippedWeapon(equippedWeapon);
   }

   public void refreshStats (Hat equippedHat) {
      physicalStatRow.setEquippedHat(equippedHat);
      fireStatRow.setEquippedHat(equippedHat);
      earthStatRow.setEquippedHat(equippedHat);
      airStatRow.setEquippedHat(equippedHat);
      waterStatRow.setEquippedHat(equippedHat);
   }

   public void refreshStats (Armor equippedArmor) {
      physicalStatRow.setEquippedArmor(equippedArmor);
      fireStatRow.setEquippedArmor(equippedArmor);
      earthStatRow.setEquippedArmor(equippedArmor);
      airStatRow.setEquippedArmor(equippedArmor);
      waterStatRow.setEquippedArmor(equippedArmor);
   }

   public void setStatModifiers (Item item) {
      // Skip equipped items
      if (item == null || InventoryManager.isEquipped(item.id)) {
         return;
      }

      // Determine if the hovered item is a weapon or an armor
      if (item.category == Item.Category.Weapon) {
         Weapon weapon = Weapon.castItemToWeapon(item);

         // Set how the stats would change if the item was equipped
         physicalStatRow.setStatModifiersForWeapon(weapon);
         fireStatRow.setStatModifiersForWeapon(weapon);
         earthStatRow.setStatModifiersForWeapon(weapon);
         airStatRow.setStatModifiersForWeapon(weapon);
         waterStatRow.setStatModifiersForWeapon(weapon);

      } else if (item.category == Item.Category.Armor) {
         Armor armor = Armor.castItemToArmor(item);

         // Set how the stats would change if the item was equipped
         physicalStatRow.setStatModifiersForArmor(armor);
         fireStatRow.setStatModifiersForArmor(armor);
         earthStatRow.setStatModifiersForArmor(armor);
         airStatRow.setStatModifiersForArmor(armor);
         waterStatRow.setStatModifiersForArmor(armor);
      }
   }

   public void clearStatModifiers () {
      physicalStatRow.disableStatModifiers();
      fireStatRow.disableStatModifiers();
      earthStatRow.disableStatModifiers();
      airStatRow.disableStatModifiers();
      waterStatRow.disableStatModifiers();
   }

   public void clearAll () {
      physicalStatRow.clear();
      fireStatRow.clear();
      earthStatRow.clear();
      airStatRow.clear();
      waterStatRow.clear();
   }

   #region Private Variables

   #endregion
}
