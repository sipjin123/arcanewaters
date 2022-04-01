using System;
using UnityEngine;
using UnityEngine.UI;

namespace ItemDefinitionTool
{
   /// <summary>
   /// THIS CLASS IS DISCONTINUED
   /// </summary>
   public class WeaponSpecificAttributes : TypeSpecificAttributes
   {
      #region Public Variables

      public override Type targetType => typeof(object);

      // Various attribute inputs and controls
      public EnumDropdown actionTypeDropdown;
      public EnumDropdown classDropdown;
      public InputField baseDamageInput;
      public InputField fireDamageInput;
      public InputField waterDamageInput;
      public InputField airDamageInput;
      public InputField earthDamageInput;

      #endregion

      private void Awake () {
         //actionTypeDropdown.setEnumType(typeof(WeaponDefinition.ActionType));
         //classDropdown.setEnumType(typeof(WeaponDefinition.Class));
      }

      public override void applyAttributeValues (ItemDefinition target) {
         //WeaponDefinition weapon = target as WeaponDefinition;
         //
         //weapon.actionType = actionTypeDropdown.getValue<WeaponDefinition.ActionType>();
         //weapon.weaponClass = classDropdown.getValue<WeaponDefinition.Class>();
         //
         //weapon.baseDamage = int.Parse(baseDamageInput.text);
         //weapon.fireDamage = int.Parse(fireDamageInput.text);
         //weapon.waterDamage = int.Parse(waterDamageInput.text);
         //weapon.airDamage = int.Parse(airDamageInput.text);
         //weapon.earthDamage = int.Parse(earthDamageInput.text);
      }

      public override void setValuesWithoutNotify (ItemDefinition itemDefinition) {
         //WeaponDefinition weapon = itemDefinition as WeaponDefinition;
         //
         //actionTypeDropdown.setEnumValueWithoutNotify((int) weapon.actionType);
         //classDropdown.setEnumValueWithoutNotify((int) weapon.weaponClass);
         //
         //baseDamageInput.SetTextWithoutNotify(weapon.baseDamage.ToString());
         //fireDamageInput.SetTextWithoutNotify(weapon.fireDamage.ToString());
         //waterDamageInput.SetTextWithoutNotify(weapon.waterDamage.ToString());
         //airDamageInput.SetTextWithoutNotify(weapon.airDamage.ToString());
         //earthDamageInput.SetTextWithoutNotify(weapon.earthDamage.ToString());
      }

      #region Private Variables

      #endregion
   }
}
