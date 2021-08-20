﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;

public class InventoryStatRow : MonoBehaviour
{
   #region Public Variables

   // The row element
   public Element element;

   // The attack value of the equipped weapon
   public Text attackText;

   // The defense value of the equipped armor
   public Text defenseText;

   // The difference between the currently equipped item and the hovered one
   public Text modText;

   // The color used for positive stat modifiers
   public Color positiveStatModColor;

   // The color used for negative stat modifiers
   public Color negativeStatModColor;

   // The color used without modifiers
   public Color normalStatModColor;

   #endregion

   public void clear () {
      attackText.text = "0";
      defenseText.text = "0";
      _equippedAttackValue = 0;
      _equippedDefenseValue = 0;
      disableStatModifiers();
   }

   public void setEquippedWeapon (Weapon weapon) {
      Rarity.Type equipmentRarity = Weapon.getRarity(weapon);
      float rarityModier = Weapon.getDamageModifier(equipmentRarity);

      _equippedAttackValue = (int) (rarityModier * weapon.getDamage(element));
      attackText.text = _equippedAttackValue.ToString();
   }

   public void setEquippedArmor (Armor armor) {
      Rarity.Type equipmentRarity = Armor.getRarity(armor);
      float rarityModier = Armor.getArmorModifier(equipmentRarity);

      _equippedDefenseValue = (int) (rarityModier * armor.getDefense(element));
      defenseText.text = _equippedDefenseValue.ToString();
   }

   public void setEquippedHat (Hat hat) {
      Rarity.Type equipmentRarity = Hat.getRarity(hat);
      float rarityModier = Hat.getDefenseModifier(equipmentRarity);

      _equippedDefenseValue = (int) (rarityModier * hat.getDefense(element));
      defenseText.text = _equippedDefenseValue.ToString();
   }

   public void setStatModifiersForWeapon (Weapon weapon) {
      // Calculate the attack modifier
      float newAttack = weapon.getDamage(element) * Weapon.getDamageModifier(weapon.getRarity());
      float attackDifference = (int) (newAttack - _equippedAttackValue);
      attackText.text = newAttack.ToString();

      // Display the stat difference and color according to its positive or negative effect
      if (attackDifference >= 0) {
         modText.text = "+" + attackDifference.ToString();
         modText.color = positiveStatModColor;
         attackText.color = positiveStatModColor;
      } else {
         modText.text = attackDifference.ToString();
         modText.color = negativeStatModColor;
         attackText.color = negativeStatModColor;
      }
   }

   public void setStatModifiersForArmor (Armor armor) {
      // Calculate the defense modifier
      float newDefense = armor.getDefense(element) * Armor.getArmorModifier(armor.getRarity());
      float defenseDifference = (int) (newDefense - _equippedDefenseValue);
      defenseText.text = newDefense.ToString();

      // Display the stat difference and color according to its positive or negative effect
      if (defenseDifference >= 0) {
         modText.text = "+" + defenseDifference.ToString();
         modText.color = positiveStatModColor;
         defenseText.color = positiveStatModColor;
      } else {
         modText.text = defenseDifference.ToString();
         modText.color = negativeStatModColor;
         defenseText.color = negativeStatModColor;
      }
   }

   public void disableStatModifiers () {
      modText.text = "";
      attackText.text = _equippedAttackValue.ToString();
      defenseText.text = _equippedDefenseValue.ToString();
      attackText.color = normalStatModColor;
      defenseText.color = normalStatModColor;
   }

   #region Private Variables

   // The attack stat of the equipped weapon
   private float _equippedAttackValue = 0;

   // The defense stat of the equipped armor
   private float _equippedDefenseValue = 0;

   #endregion
}
