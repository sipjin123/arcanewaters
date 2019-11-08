using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DamageModifierTemplate : MonoBehaviour {
   #region Public Variables

   // Determines the type of variable value
   public Slider typeSlidier;
   public Text typeText;

   // Determines the damage modifier
   public InputField damageMultiplier;

   // Deletes the template
   public Button deleteButton;

   // Label of the template
   public Text label;

   // The label of the modifier
   public Text modifiers;

   #endregion

   #region Private Variables
      
   #endregion
}
