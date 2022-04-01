using UnityEngine;
using UnityEngine.UI;
using System;

namespace ItemDefinitionTool
{
   /// <summary>
   /// THIS CLASS IS DISCONTINUED
   /// </summary>
   public class HatSpecificAttributes : TypeSpecificAttributes
   {
      #region Public Variables

      // The target class type of this attribute section
      public override Type targetType => typeof(object);

      // Various attribute inputs and controls
      public InputField baseDefenseInput;
      public InputField fireResistInput;
      public InputField waterResistInput;
      public InputField airResistInput;
      public InputField earthResistInput;

      #endregion

      public override void applyAttributeValues (ItemDefinition target) {
         //HatDefinition hat = target as HatDefinition;
         //
         //hat.baseDefense = int.Parse(baseDefenseInput.text);
         //hat.fireResist = int.Parse(fireResistInput.text);
         //hat.waterResist = int.Parse(waterResistInput.text);
         //hat.airResist = int.Parse(airResistInput.text);
         //hat.earthResist = int.Parse(earthResistInput.text);
      }

      public override void setValuesWithoutNotify (ItemDefinition itemDefinition) {
         //HatDefinition hat = itemDefinition as HatDefinition;
         //
         //baseDefenseInput.SetTextWithoutNotify(hat.baseDefense.ToString());
         //fireResistInput.SetTextWithoutNotify(hat.fireResist.ToString());
         //waterResistInput.SetTextWithoutNotify(hat.waterResist.ToString());
         //airResistInput.SetTextWithoutNotify(hat.airResist.ToString());
         //earthResistInput.SetTextWithoutNotify(hat.earthResist.ToString());
      }

      #region Private Variables

      #endregion
   }
}
