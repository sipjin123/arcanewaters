using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class StoreFilter : MonoBehaviour
{
   #region Public Variables

   // Event when the filter option changes
   public Dropdown.DropdownEvent onFilterChanged = new Dropdown.DropdownEvent();

   // Reference to a dropdown with the options
   public Dropdown dropdown;

   #endregion

   private void Start () {
      activateFirstOption();

      dropdown.onValueChanged.RemoveAllListeners();
      dropdown.onValueChanged.AddListener(onDropDownValueChanged);
   }

   public int getCurrentOption() {
      return dropdown.value;
   }

   public string getCurrentOptionText () {
      int index = getCurrentOption();
      Dropdown.OptionData option = dropdown.options[index];
      return option.text;
   }

   public void addOption (string name) {
      if (string.IsNullOrWhiteSpace(name)) {
         return;
      }

      dropdown.AddOptions(new List<string> { name });
   }

   public void addOptions (IEnumerable<string> names) {
      if (dropdown == null) {
         return;
      }

      dropdown.AddOptions(names.ToList());
   }

   public void activateOption (string value) {
      if (dropdown == null || string.IsNullOrWhiteSpace(value)) {
         return;
      }

      Dropdown.OptionData option = dropdown.options.FirstOrDefault(_ => Util.areStringsEqual(_.text, value));

      if (option == null) {
         return;
      }

      int index = dropdown.options.IndexOf(option);

      if (index < 0) {
         return;
      }

      dropdown.SetValueWithoutNotify(index);
   }

   public void activateFirstOption () {
      if (getOptionsCount() == 0) {
         return;
      }

      dropdown.SetValueWithoutNotify(0);
   }

   public bool hasOption(string value, bool ignoreCase = true) {
      if (dropdown == null || string.IsNullOrWhiteSpace(value)) {
         return false;
      }

      return dropdown.options.Any(_ => Util.areStringsEqual(_.text, value, ignoreCase));
   }

   public int getOptionsCount () {
      if (dropdown == null) {
         return 0;
      }

      return dropdown.options.Count;
   }

   public void clearOptions () {
      if (dropdown == null) {
         return;
      }

      dropdown.ClearOptions();
   }

   public void onDropDownValueChanged (int newIndex) {
      if (onFilterChanged == null) {
         return;
      }

      onFilterChanged.Invoke(newIndex);
   }

   #region Private Variables

   #endregion
}
