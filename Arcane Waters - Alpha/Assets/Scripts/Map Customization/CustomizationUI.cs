using UnityEngine;
using UnityEngine.UI;
using MapCustomization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

namespace MapCustomization
{
   public class CustomizationUI : Panel
   {
      #region Public Variables

      // Singleton instance
      public static CustomizationUI self { get; private set; }

      // Is the UI showing the loading screen
      public static bool isLoading { get; private set; }

      // Which prefab data is currently selected 
      public static PrefabSelectionEntry selectedPrefabEntry;

      // Title text of UI
      public Text titleText;

      // Object that is controlling scrollable area for prefab selection
      public ScrollRect prefabSelection;

      // Object, which holds placeable prefabs in the UI
      public RectTransform prefabSelectEntryParent;

      // Template for prefab selection entries
      public PrefabSelectionEntry prefabSelectEntryPref;

      #endregion

      private void OnEnable () {
         self = this;

         _cGroup = GetComponent<CanvasGroup>();
      }

      public override void Update () {
         if (!isShowing() || isLoading) return;

         Vector2 pointerPos = Input.mousePosition;
         if (pointerPos != _lastPointerPos) {
            if (!Input.GetMouseButton(0)) {
               MapCustomizationManager.pointerHover(Camera.main.ScreenToWorldPoint(pointerPos));
            }

            _lastPointerPos = pointerPos;
         }

         if (Input.GetKey(KeyCode.Delete)) {
            MapCustomizationManager.keyDelete();
         }
      }

      public void pointerEnter (BaseEventData eventData) {
         PointerEventData pointerData = eventData as PointerEventData;
         MapCustomizationManager.pointerEnter(Camera.main.ScreenToWorldPoint(pointerData.position));
      }

      public void pointerExit (BaseEventData eventData) {
         PointerEventData pointerData = eventData as PointerEventData;
         MapCustomizationManager.pointerExit(Camera.main.ScreenToWorldPoint(pointerData.position));
      }

      public void pointerDrag (BaseEventData eventData) {
         PointerEventData pointerData = eventData as PointerEventData;
         MapCustomizationManager.pointerDrag(Camera.main.ScreenToWorldPoint(pointerData.position) - Camera.main.ScreenToWorldPoint(pointerData.position - pointerData.delta));
      }

      public void pointerUp (BaseEventData eventData) {
         PointerEventData pointerData = eventData as PointerEventData;
         MapCustomizationManager.pointerUp(Camera.main.ScreenToWorldPoint(pointerData.position));
      }

      public void pointerDown (BaseEventData eventData) {
         PointerEventData pointerData = eventData as PointerEventData;
         MapCustomizationManager.pointerDown(Camera.main.ScreenToWorldPoint(pointerData.position));
      }

      public static void selectEntry (PrefabSelectionEntry entry) {
         if (selectedPrefabEntry != null) {
            selectedPrefabEntry.setSelected(false);
         }

         selectedPrefabEntry = entry;

         if (selectedPrefabEntry != null) {
            selectedPrefabEntry.setSelected(true);
            MapCustomizationManager.selectPrefab(null);
         }
      }

      public static void setLoading (bool loading) {
         isLoading = loading;
         self.titleText.text = loading ? "Loading..." : "Customization";
         self.prefabSelection.gameObject.SetActive(!loading);
      }

      public static void setPlaceablePrefabData (IEnumerable<PlaceablePrefabData> dataCollection) {
         selectedPrefabEntry = null;
         foreach (PrefabSelectionEntry entry in self.prefabSelectEntryParent.GetComponentsInChildren<PrefabSelectionEntry>(true)) {
            Destroy(entry.gameObject);
         }
         _prefabEntries.Clear();

         foreach (PlaceablePrefabData data in dataCollection) {
            PrefabSelectionEntry entry = Instantiate(self.prefabSelectEntryPref, self.prefabSelectEntryParent);
            entry.target = data;
            entry.setImage(data.displaySprite);
            int count = MapCustomizationManager.amountOfPropLeft(MapCustomizationManager.remainingProps, data.prefab);
            entry.setCount(count);
            _prefabEntries.Add(entry);
         }
      }

      public static void updatePropCount (ItemInstance prop) {
         foreach (PrefabSelectionEntry entry in _prefabEntries) {
            if (entry.target.prefab.propDefinitionId == prop.itemDefinitionId) {
               entry.setCount(prop.count);
            }
         }
      }

      #region Private Variables

      // Canvas group that wraps the entire UI
      private CanvasGroup _cGroup;

      // Last pointer position, cached by this component
      private Vector2 _lastPointerPos;

      // Entries of prefabs that can be selected and placed
      private static List<PrefabSelectionEntry> _prefabEntries = new List<PrefabSelectionEntry>();

      #endregion
   }
}
