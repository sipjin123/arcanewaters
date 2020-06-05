using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System;

public class CraftingStatColumn : MonoBehaviour
{
   #region Public Variables

   // The row element
   public Element element;

   // The stat value of the item
   public Text statText;

   // The difference between the item and the currently equipped item
   public Text modText;

   // The color used for positive stat modifiers
   public Color positiveStatModColor;

   // The color used for negative stat modifiers
   public Color negativeStatModColor;

   // The icon tooltip
   public Tooltipped tooltip;

   #endregion

   public void Awake () {
      tooltip.text = element.ToString();
   }

   public void clear () {
      statText.text = "0";
      modText.text = "0";
   }

   public void setColumnForWeapon(Weapon weapon, Weapon equippedWeapon) {
      // Calculate the attack numbers
      float itemStat = weapon.getDamage(element);
      float difference = 0;
      if (equippedWeapon != null) {
         difference = itemStat - equippedWeapon.getDamage(element);
      } else {
         difference = itemStat;
      }

      // Set the values
      setStat(itemStat, difference);
   }

   public void setColumnForArmor(Armor armor, Armor equippedArmor) {
      // Calculate the defense numbers
      float itemStat = armor.getDefense(element);
      float difference = 0;
      if (equippedArmor != null) {
         difference = itemStat - equippedArmor.getDefense(element);
      } else {
         difference = itemStat;
      }

      // Set the values
      setStat(itemStat, difference);
   }

   public void setColumnForHat (Hat hat, Hat equippedHat) {
      // Calculate the defense numbers
      float itemStat = hat.getDefense(element);
      float difference = 0;
      if (equippedHat != null) {
         difference = itemStat - equippedHat.getDefense(element);
      } else {
         difference = itemStat;
      }

      // Set the values
      setStat(itemStat, difference);
   }

   private void setStat(float itemStat, float difference) {
      // Set the stat value
      statText.text = itemStat.ToString();

      // Display the stat difference and color according to its positive or negative effect
      if (difference >= 0) {
         modText.text = "+" + difference.ToString();
         modText.color = positiveStatModColor;
      } else {
         modText.text = difference.ToString();
         modText.color = negativeStatModColor;
      }
   }

   #region Private Variables

   #endregion
}
