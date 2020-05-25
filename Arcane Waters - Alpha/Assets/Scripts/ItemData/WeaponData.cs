using UnityEngine;

public class WeaponData : BattleItemData {
   #region Public Variables

   #endregion

   // Builder scriptable object instance builder. (Override from the ScriptableObject class)
   public static WeaponData CreateInstance (WeaponData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      WeaponData data = new WeaponData();

      // Basic battle item data
      data.setBaseBattleItemData(datacopy);

      // Weapon Data
      data.setPrimaryColor(datacopy.getPrimaryColor);
      data.setSecondaryColor(datacopy.getSecondaryColor);
      data.setItemDamage(datacopy.getBaseDamage());

      return data;
   }

   // Builder for the weapon data in item builder
   public static WeaponData CreateInstance (BattleItemData basicData, Weapon.Class _classRequirement, string _primaryPalette, string _secondaryPalette, int damage) {
      WeaponData data = new WeaponData();

      // Basic battle item data.
      data.classRequirement = _classRequirement;

      data.setBaseBattleItemData(basicData);

      // Weapon Data
      data.setPrimaryColor(_primaryPalette);
      data.setSecondaryColor(_secondaryPalette);
      data.setItemDamage(damage);

      return data;
   }

   protected void setPrimaryColor (string value) { _primaryPalette = value; }
   protected void setSecondaryColor (string value) { _secondaryPalette = value; }
   protected void setItemDamage (int value) { _baseDamage = value; }

   public string getPrimaryColor { get { return _primaryPalette; } }
   public string getSecondaryColor { get { return _secondaryPalette; } }
   public int getBaseDamage () { return _baseDamage; }

   #region Private Variables

   // Weapon data main colors that the weapon will have/has

   [SerializeField] private string _primaryPalette;
   [SerializeField] private string _secondaryPalette;
   [SerializeField] private int _baseDamage;

   #endregion
}