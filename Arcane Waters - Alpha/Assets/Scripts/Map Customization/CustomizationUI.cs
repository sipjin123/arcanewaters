using UnityEngine;
using UnityEngine.UI;
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

      // Reference to the grouping of prefabs
      public static IEnumerable<IGrouping<(int itemTypeId, Item.Category itemCategory), PlaceablePrefabData>> itemPrefabs;

      // Text that shows we are using guild inventory
      public GameObject guildInventoryText = null;

      // Load blocker
      public GameObject loadBlocker = null;

      #endregion

      private void Awake () {
         self = this;

         _cGroup = GetComponent<CanvasGroup>();

         ensureHidden();
      }

      private void Update () {
         if (!_isShowing || isLoading) return;

         if (!MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager manager)) {
            ensureHidden();
            return;
         }

         Vector2 pointerPos = Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition);



         if (!KeyUtils.GetButton(MouseButton.Left)) {
            manager.pointerHover(pointerPos);
         } else {
            manager.pointerDrag(pointerPos - _lastPointerPos);
         }

         if (KeyUtils.GetButton(MouseButton.Right)) {
            manager.rightClick();
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

         if (KeyUtils.GetKeyDown(Key.Delete) || KeyUtils.GetKeyDown(Key.Backspace)) {
            manager.keyDeleteAt(pointerPos);
         }
      }

      public void pointerEnter (BaseEventData eventData) {
         if (MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager manager)) {
            PointerEventData pointerData = eventData as PointerEventData;
            manager.pointerEnter(Camera.main.ScreenToWorldPoint(pointerData.position));
         }
      }

      public void pointerExit (BaseEventData eventData) {
         if (MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager manager)) {
            PointerEventData pointerData = eventData as PointerEventData;
            manager.pointerExit(Camera.main.ScreenToWorldPoint(pointerData.position));
         }
      }

      public void pointerUp (BaseEventData eventData) {
         if (MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager manager)) {
            PointerEventData pointerData = eventData as PointerEventData;
            if (pointerData.button == PointerEventData.InputButton.Left) {
               manager.pointerUp(Camera.main.ScreenToWorldPoint(pointerData.position));
            }
         }
      }

      public void pointerDown (BaseEventData eventData) {
         if (MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager manager)) {
            PointerEventData pointerData = eventData as PointerEventData;
            if (pointerData.button == PointerEventData.InputButton.Left) {
               manager.pointerDown(Camera.main.ScreenToWorldPoint(pointerData.position));
            } else if (pointerData.button == PointerEventData.InputButton.Right) {
               selectEntry(null);
            }
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

            if (MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager manager)) {
               manager.selectPrefab(null);
               TutorialManager3.self.tryCompletingStep(TutorialTrigger.SelectObject);
            }
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
         if (self == null || self._cGroup == null) {
            return;
         }

         Util.disableCanvasGroup(self._cGroup);

         _isShowing = false;
      }

      public void hideCustomizationPanel () {
         // Unequip hammer to exit customization
         if (Global.player != null) {
            Global.player.rpc.Cmd_RequestSetWeaponId(0, false);
         }
      }

      public static PlaceablePrefabData? getSelectedPrefabData () {
         if (_selectedPrefabEntry == null) return null;
         return _selectedPrefabEntry.getSelectedData();
      }

      public static void setLoading (bool loading) {
         if (loading == isLoading) {
            return;
         }

         isLoading = loading;
         self.prefabSelection.gameObject.SetActive(!loading);
         self.loadBlocker.SetActive(loading);
      }

      public static void setPlaceablePrefabData (MapCustomizationManager manager, ICollection<PlaceablePrefabData> dataCollection, IList<ItemTypeCount> itemsLeft) {
         // Clear the current entries
         _selectedPrefabEntry = null;
         foreach (PrefabSelectionEntry entry in self.prefabSelectEntryParent.GetComponentsInChildren<PrefabSelectionEntry>(true)) {
            Destroy(entry.gameObject);
         }
         _prefabEntries.Clear();

         // Group prefabs by their item id
         itemPrefabs = dataCollection.GroupBy(d => (d.prefab.propDefinitionId, d.prefab.propItemCategory));

         foreach (var itemPrefab in itemPrefabs) {
            PrefabSelectionEntry entry = Instantiate(self.prefabSelectEntryPref, self.prefabSelectEntryParent);
            entry.setData(itemPrefab.Key.itemTypeId, itemPrefab.Key.itemCategory, itemPrefab.ToArray());
            _prefabEntries.Add(entry);
         }

         updatePropCount(itemsLeft);
      }

      public static void updatePropCount (IList<ItemTypeCount> items) {
         foreach (PrefabSelectionEntry entry in _prefabEntries) {
            bool found = false;
            foreach (ItemTypeCount item in items) {
               if (entry.propDefinitionId == item.itemTypeId && entry.propItemCategory == item.category) {
                  found = true;

                  if (entry.getCount() != item.count) {
                     entry.setCount(item.count);
                  }

                  break;
               }
            }

            if (!found) {
               if (entry.getCount() != 0) {
                  entry.setCount(0);

               }
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
