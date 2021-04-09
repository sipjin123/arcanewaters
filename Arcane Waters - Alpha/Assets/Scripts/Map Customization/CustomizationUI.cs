using UnityEngine;
using UnityEngine.UI;
using MapCustomization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MapCustomization
{
   public class CustomizationUI : MonoBehaviour
   {
      #region Public Variables

      // Singleton instance
      public static CustomizationUI self { get; private set; }

      // Is the UI showing the loading screen
      public static bool isLoading { get; private set; }

      // Title text of UI
      public Text titleText;

      // Object that is controlling scrollable area for prefab selection
      public ScrollRect prefabSelection;

      // Object, which holds placeable prefabs in the UI
      public RectTransform prefabSelectEntryParent;

      // Template for prefab selection entries
      public PrefabSelectionEntry prefabSelectEntryPref;

      #endregion

      private void Awake () {
         self = this;

         _cGroup = GetComponent<CanvasGroup>();

         ensureHidden();
      }

      private void Update () {
         if (!_isShowing || isLoading) return;

         Vector2 pointerPos = Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition);

         if (!KeyUtils.GetButton(MouseButton.Left)) {
            MapCustomizationManager.pointerHover(pointerPos);
         } else {
            MapCustomizationManager.pointerDrag(pointerPos - _lastPointerPos);
         }

         _lastPointerPos = pointerPos;

         // Allow user to select prefab variations with scroll wheel
         if (MouseUtils.mouseScrollY != 0 && _selectedPrefabEntry != null) {
            if (MouseUtils.mouseScrollY > 0) {
               _selectedPrefabEntry.onNext();
            }
            if (MouseUtils.mouseScrollY < 0) {
               _selectedPrefabEntry.onPrevious();
            }
         }

         if (KeyUtils.GetKey(Key.Delete)) {
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

      public void pointerUp (BaseEventData eventData) {
         PointerEventData pointerData = eventData as PointerEventData;
         if (pointerData.button == PointerEventData.InputButton.Left) {
            MapCustomizationManager.pointerUp(Camera.main.ScreenToWorldPoint(pointerData.position));
         }
      }

      public void pointerDown (BaseEventData eventData) {
         PointerEventData pointerData = eventData as PointerEventData;
         if (pointerData.button == PointerEventData.InputButton.Left) {
            MapCustomizationManager.pointerDown(Camera.main.ScreenToWorldPoint(pointerData.position));
         } else if (pointerData.button == PointerEventData.InputButton.Right) {
            selectEntry(null);
         }
      }

      public static void prefabEntryClick (PrefabSelectionEntry entry) {
         selectEntry(_selectedPrefabEntry == entry ? null : entry);
      }

      public static void selectEntry (PrefabSelectionEntry entry) {
         if (_selectedPrefabEntry != null) {
            _selectedPrefabEntry.setSelected(false);
         }

         _selectedPrefabEntry = entry;

         if (_selectedPrefabEntry != null) {
            _selectedPrefabEntry.setSelected(true);
            MapCustomizationManager.selectPrefab(null);

            TutorialManager3.self.tryCompletingStep(TutorialTrigger.SelectObject);
         }
      }

      public static void ensureShowing () {
         if (_isShowing) {
            return;
         }

         Util.enableCanvasGroup(self._cGroup);

         _isShowing = true;
      }

      public static void ensureHidden () {
         Util.disableCanvasGroup(self._cGroup);

         _isShowing = false;
      }

      // Non-static method used to assign to UI button
      public void hideCustomizationPanel () {
         ensureHidden();
      }

      public static PlaceablePrefabData? getSelectedPrefabData () {
         if (_selectedPrefabEntry == null) return null;
         return _selectedPrefabEntry.getSelectedData();
      }

      public static void setLoading (bool loading) {
         isLoading = loading;
         self.titleText.text = loading ? "Loading..." : "Customization";
         self.prefabSelection.gameObject.SetActive(!loading);
      }

      public static void setPlaceablePrefabData (ICollection<PlaceablePrefabData> dataCollection) {
         // Clear the current entries
         _selectedPrefabEntry = null;
         foreach (PrefabSelectionEntry entry in self.prefabSelectEntryParent.GetComponentsInChildren<PrefabSelectionEntry>(true)) {
            Destroy(entry.gameObject);
         }
         _prefabEntries.Clear();

         // Group prefabs by their item id
         IEnumerable<IGrouping<int, PlaceablePrefabData>> itemPrefabs = dataCollection.GroupBy(d => d.prefab.propDefinitionId);

         foreach (IGrouping<int, PlaceablePrefabData> itemPrefab in itemPrefabs) {
            PrefabSelectionEntry entry = Instantiate(self.prefabSelectEntryPref, self.prefabSelectEntryParent);
            entry.setData(itemPrefab.Key, itemPrefab.ToArray());
            int count = MapCustomizationManager.amountOfPropLeft(MapCustomizationManager.remainingProps, itemPrefab.Key);
            entry.setCount(count);
            _prefabEntries.Add(entry);
         }
      }

      public static void updatePropCount (ItemInstance prop) {
         foreach (PrefabSelectionEntry entry in _prefabEntries) {
            if (entry.propDefinitionId == prop.itemDefinitionId) {
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

      // Is panel currently showing
      private static bool _isShowing = false;

      // Which prefab data is currently selected 
      private static PrefabSelectionEntry _selectedPrefabEntry;

      #endregion
   }
}
