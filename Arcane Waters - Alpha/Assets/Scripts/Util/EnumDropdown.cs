using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Use this class when you want your dropdown to model an enum, without any additional options
/// </summary>
public class EnumDropdown : Dropdown
{
   #region Public Variables

   #endregion

   public void setEnumType (Type enumType) {
      _enumType = enumType;

      // Get the int values and names of enum values
      int[] values = (int[]) Enum.GetValues(enumType);
      string[] displayNames = Enum.GetNames(enumType);

      // Remember these names and values
      _optionValues = new (int value, string displayname)[values.Length];
      for (int i = 0; i < _optionValues.Length; i++) {
         _optionValues[i] = (values[i], displayNames[i]);
      }

      // Set dropdown options
      options.Clear();
      foreach (string displayName in displayNames) {
         options.Add(new OptionData { text = displayName });
      }
   }

   public T getValue<T> () where T : Enum {
      if (_enumType == null) {
         throw new Exception("Invalid state in enum dropdown: enum type was not set yet");
      }

      return (T) (object) _optionValues[value].value;
   }

   public void setEnumValueWithoutNotify (int enumValue) {
      for (int i = 0; i < _optionValues.Length; i++) {
         if (_optionValues[i].value == enumValue) {
            SetValueWithoutNotify(i);
            break;
         }
      }
   }

   public void overrideOptionName (int enumValue, string newDisplayName) {
      if (_enumType == null) {
         throw new Exception("Invalid state in enum dropdown: enum type was not set yet");
      }

      for (int i = 0; i < _optionValues.Length; i++) {
         if (_optionValues[i].value == enumValue) {
            _optionValues[i].displayname = newDisplayName;
            options[i].text = newDisplayName;
            break;
         }
      }
   }

   #region Private Variables

   // Type of enum we are targeting
   private Type _enumType;

   // Dropdown option values, where every option entry has the enum integer value and display name
   private (int value, string displayname)[] _optionValues;

   #endregion
}
