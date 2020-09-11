using UnityEngine;
using UnityEngine.UI;
using System;

namespace ItemDefinitionTool
{
   public class ArmorSpecificAttributes : TypeSpecificAttributes
   {
      #region Public Variables

      // The target class type of this attribute section
      public override Type targetType => typeof(ArmorDefinition);

      // Various attribute inputs and controls
      public InputField baseDefenseInput;
      public InputField fireResistInput;
      public InputField waterResistInput;
      public InputField airResistInput;
      public InputField earthResistInput;

      #endregion

      public override void applyAttributeValues (ItemDefinition target) {
         ArmorDefinition armor = target as ArmorDefinition;

         armor.baseDefense = int.Parse(baseDefenseInput.text);
         armor.fireResist = int.Parse(fireResistInput.text);
         armor.waterResist = int.Parse(waterResistInput.text);
         armor.airResist = int.Parse(airResistInput.text);
         armor.earthResist = int.Parse(earthResistInput.text);
      }

      public override void setValuesWithoutNotify (ItemDefinition itemDefinition) {
         ArmorDefinition armor = itemDefinition as ArmorDefinition;



         baseDefenseInput.SetTextWithoutNotify(armor.baseDefense.ToString());
         fireResistInput.SetTextWithoutNotify(armor.fireResist.ToString());
         waterResistInput.SetTextWithoutNotify(armor.waterResist.ToString());
         airResistInput.SetTextWithoutNotify(armor.airResist.ToString());
         earthResistInput.SetTextWithoutNotify(armor.earthResist.ToString());
      }

      #region Private Variables

      #endregion
   }
}
