using UnityEngine;

public class WeaponData : BattleItemData
{
   #region Public Variables

   #endregion

   // Builder scriptable object instance builder. (Override from the ScriptableObject class)
   public static WeaponData CreateInstance (WeaponData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      WeaponData data = CreateInstance<WeaponData>();

      // Basic battle item data.

      data.setName(datacopy.getName());
      data.setItemID(datacopy.getItemID());
      data.setDescription(datacopy.getDescription());
      data.setItemIcon(datacopy.getItemIcon());

      data.setItemDamage(datacopy.getBaseDamage());
      data.setItemElement(datacopy.getElementType());

      data.setHitAudioClip(datacopy.getHitAudioClip());
      data.setHitParticle(datacopy.getHitParticle());

      data.setBattleItemType(datacopy.getBattleItemType());
      data.setClassRequirement(datacopy.getClassRequirement());

      // Weapon Data
      data.setPrimaryColor(datacopy.getPrimaryColor);
      data.setSecondaryColor(datacopy.getSecondaryColor);

      return data;
   }

   // Builder for the weapon data in item builder.
   public static WeaponData CreateInstance (BattleItemData basicData, Weapon.Class _classRequirement, ColorType _primaryC, ColorType _secondaryC) {
      WeaponData data = CreateInstance<WeaponData>();

      // Basic battle item data.

      data.setName(basicData.getName());
      data.setItemID(basicData.getItemID());
      data.setDescription(basicData.getDescription());
      data.setItemIcon(basicData.getItemIcon());

      data.setItemDamage(basicData.getBaseDamage());
      data.setItemElement(basicData.getElementType());

      data.setHitAudioClip(basicData.getHitAudioClip());
      data.setHitParticle(basicData.getHitParticle());

      data.setBattleItemType(basicData.getBattleItemType());
      data.setClassRequirement(_classRequirement);

      // Weapon Data
      data.setPrimaryColor(_primaryC);
      data.setSecondaryColor(_secondaryC);

      return data;
   }

   protected void setPrimaryColor (ColorType value) { _primaryColor = value; }
   protected void setSecondaryColor (ColorType value) { _secondaryColor = value; }

   public ColorType getPrimaryColor { get { return _primaryColor; } }
   public ColorType getSecondaryColor { get { return _secondaryColor; } }

   #region Private Variables

   // Weapon data main colors that the weapon will have/has.

   [SerializeField] private ColorType _primaryColor;
   [SerializeField] private ColorType _secondaryColor;

   #endregion
}