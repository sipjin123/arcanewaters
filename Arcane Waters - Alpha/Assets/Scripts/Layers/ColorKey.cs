using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ColorKey {
   #region Public Variables

   // The prefix associated with this Color key
   public string prefix;

   // The suffix associated with this Color key
   public string suffix;

   #endregion

   public ColorKey (Gender.Type gender, string suffix) {
      this.prefix = gender + "";
      this.suffix = suffix;
   }

   public ColorKey (Gender.Type gender, Layer layerType) {
      this.prefix = gender + "";
      this.suffix = layerType + "";
   }

   public ColorKey (Gender.Type gender, Armor.Type armorType) {
      this.prefix = gender + "";
      this.suffix = armorType + "";
   }

   public ColorKey (Gender.Type gender, Weapon.Type weaponType) {
      this.prefix = gender + "";
      this.suffix = weaponType + "";
   }

   public ColorKey (Ship.Type shipType, Layer layerType) {
      this.prefix = shipType + "";
      this.suffix = layerType + "";
   }

   public override bool Equals (object rhs) {
      if (rhs is ColorKey) {
         var other = rhs as ColorKey;
         return prefix == other.prefix && suffix == other.suffix;
      }
      return false;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + prefix.GetHashCode();
         hash = hash * 23 + suffix.GetHashCode();
         return hash;
      }
   }

   public override string ToString () {
      return string.Format("ColorKey: {0} {1}", prefix, suffix);
   }

   #region Private Variables

   #endregion
}
