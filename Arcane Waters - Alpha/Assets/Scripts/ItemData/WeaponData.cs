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
   public static WeaponData CreateInstance (BattleItemData basicData, Weapon.Class _classRequirement, ColorType _primaryC, ColorType _secondaryC, int damage) {
      WeaponData data = new WeaponData();

      // Basic battle item data.
      data.classRequirement = _classRequirement;

      data.setBaseBattleItemData(basicData);

      // Weapon Data
      data.setPrimaryColor(_primaryC);
      data.setSecondaryColor(_secondaryC);
      data.setItemDamage(damage);

      return data;
   }

   protected void setPrimaryColor (ColorType value) { _primaryColor = value; }
   protected void setSecondaryColor (ColorType value) { _secondaryColor = value; }
   protected void setItemDamage (int value) { _baseDamage = value; }

   public ColorType getPrimaryColor { get { return _primaryColor; } }
   public ColorType getSecondaryColor { get { return _secondaryColor; } }
   public int getBaseDamage () { return _baseDamage; }

   #region Private Variables

   // Weapon data main colors that the weapon will have/has

   [SerializeField] private ColorType _primaryColor;
   [SerializeField] private ColorType _secondaryColor;
   [SerializeField] private int _baseDamage;

   #endregion
}