using UnityEngine;
using UnityEngine.UI;

public class StoreFilterToggle : MonoBehaviour
{
   #region Public Variables

   // Reference to the StoreFilter object
   public StoreFilter storeFilter;

   // Reference to the toggle component
   public Toggle toggle;

   // Reference to the label
   public Text label;

   // Type of the toggle
   public ToggleType type;

   // The Filter Toggle types
   public enum ToggleType
   {
      // All toggle
      All,

      // Primary toggle
      Primary,

      // Secondary toggle
      Secondary,

      // Tertiary toggle
      Tertiary
   }

   #endregion

   private void OnEnable () {
      this.toggle.onValueChanged.RemoveAllListeners();
      this.toggle.onValueChanged.AddListener(onToggleValueChanged);
   }

   private void OnDisable () {
      this.toggle.onValueChanged.RemoveAllListeners();
   }

   private void onToggleValueChanged (bool isToggled) {
      if (this.storeFilter == null) {
         return;
      }

      this.storeFilter.notifyToggleValueChanged(this);
   }

   #region Private Variables

   #endregion
}
