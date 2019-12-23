using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;

public class InventoryStatRow : MonoBehaviour
{
   #region Public Variables

   // The attack value of the equipped weapon
   public Text attackText;

   // The attack difference between the currently equipped weapon and the hovered one
   public Text attackModText;

   // The defense value of the equipped armor
   public Text defenseText;

   // The defense difference between the currently equipped armor and the hovered one
   public Text defenseModText;

   // The color used for positive stat modifiers
   public Color positiveStatModColor;

   // The color used for negative stat modifiers
   public Color negativeStatModColor;

   #endregion

   public void setElement (Element element) {
      _element = element;
   }

   public void clear () {
      attackText.text = "0";
      defenseText.text = "0";
      disableStatModifiers();
   }

   public void setEquippedWeapon (Weapon weapon) {
      _equippedAttackValue = weapon.getDamage(_element);
      attackText.text = _equippedAttackValue.ToString();
   }

   public void setEquippedArmor (Armor armor) {
      _equippedDefenseValue = armor.getDefense(_element);
      defenseText.text = _equippedDefenseValue.ToString();
   }

   public void setStatModifiersForWeapon (Weapon weapon) {
      // Enable the modifier text object
      attackModText.gameObject.SetActive(true);

      // Calculate the attack modifier
      float newAttack = weapon.getDamage(_element);
      float attackDifference = newAttack - _equippedAttackValue;

      // Display the stat difference and color according to its positive or negative effect
      if (attackDifference >= 0) {
         attackModText.text = "+" + attackDifference.ToString();
         attackModText.color = positiveStatModColor;
      } else {
         attackModText.text = attackDifference.ToString();
         attackModText.color = negativeStatModColor;
      }
   }

   public void setStatModifiersForArmor (Armor armor) {
      // Enable the modifier text objects
      defenseModText.gameObject.SetActive(true);

      // Calculate the defense modifier
      float newDefense = armor.getDefense(_element);
      float defenseDifference = newDefense - _equippedDefenseValue;

      // Display the stat difference and color according to its positive or negative effect
      if (defenseDifference >= 0) {
         defenseModText.text = "+" + defenseDifference.ToString();
         defenseModText.color = positiveStatModColor;
      } else {
         defenseModText.text = defenseDifference.ToString();
         defenseModText.color = negativeStatModColor;
      }
   }

   public void disableStatModifiers () {
      attackModText.gameObject.SetActive(false);
      defenseModText.gameObject.SetActive(false);
   }

   #region Private Variables

   // The element displayed by the row
   private Element _element = Element.ALL;

   // The attack stat of the equipped weapon
   private float _equippedAttackValue = 0;

   // The defense stat of the equipped armor
   private float _equippedDefenseValue = 0;

   #endregion
}
