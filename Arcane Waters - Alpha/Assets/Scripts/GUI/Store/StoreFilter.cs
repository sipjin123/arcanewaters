using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StoreFilter : MonoBehaviour
{
   #region Public Variables

   // Event called when a child toggle changes state
   public StoreFilterToggleValueChangedEvent onFilterToggleValueChanged = new StoreFilterToggleValueChangedEvent();

   // When a toggle, belonging to a group, is toggled on, all the other toggles will turned off
   public bool exclusiveMode;

   // The set of toggles
   public StoreFilterToggle[] toggles;

   #endregion

   private void Start () {
      setToggle(StoreFilterToggle.ToggleType.All);
   }

   public StoreFilterToggle getCurrentToggle () {
      if (_currentToggle == null) {
         return toggles.First();
      }

      return _currentToggle;
   }

   public void notifyToggleValueChanged(StoreFilterToggle toggle) {
      if (onFilterToggleValueChanged == null) {
         return;
      }

      if (exclusiveMode) {
         disableOtherToggles(toggle.type);
      }

      this._currentToggle = toggle;
      onFilterToggleValueChanged.Invoke(toggle);
   }

   public void showToggle(StoreFilterToggle.ToggleType toggleType, bool show = true) {
      if (toggles == null || toggles.Length == 0) {
         return;
      }

      StoreFilterToggle toggle = getToggleByType(toggleType);

      if (toggle == null) {
         return;
      }

      toggle.gameObject.SetActive(show);
   }

   public void setToggle(StoreFilterToggle.ToggleType toggleType) {
      StoreFilterToggle toggle = getToggleByType(toggleType);

      if (toggle == null) {
         return;
      }

      toggle.toggle.SetIsOnWithoutNotify(true);
      this._currentToggle = toggle;

      if (exclusiveMode) {
         disableOtherToggles(toggle.type);
      }
   }

   private StoreFilterToggle getToggleByType (StoreFilterToggle.ToggleType toggleType) {
      return toggles.FirstOrDefault(_ => _.type == toggleType);
   }

   private void disableOtherToggles(StoreFilterToggle.ToggleType toggleType) {
      StoreFilterToggle toggle = getToggleByType(toggleType);

      if (toggle == null) {
         return;
      }

      if (exclusiveMode) {
         IEnumerable<StoreFilterToggle> otherToggles = toggles.Where(_ => _ != toggle);

         foreach (StoreFilterToggle otherToggle in otherToggles) {
            otherToggle.toggle.SetIsOnWithoutNotify(false);
         }
      }
   }

   #region Private Variables

   // The current toggle
   private StoreFilterToggle _currentToggle;

   #endregion
}
