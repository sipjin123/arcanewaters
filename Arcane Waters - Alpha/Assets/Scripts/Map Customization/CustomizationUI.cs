using UnityEngine;
using UnityEngine.UI;
using MapCustomization;
using System.Collections.Generic;
using System.Linq;

namespace MapCreationTool
{
   public class CustomizationUI : Panel
   {
      #region Public Variables

      // Singleton instance
      public static CustomizationUI self { get; private set; }

      // Is the UI showing the loading screen
      public static bool isLoading { get; private set; }

      // Title text of UI
      public Text titleText;

      // Dropdown, where user selects the prefab he wants to place
      public Dropdown prefabDropdown;

      // UI button, releasing a prefab on it canceles/deletes the prefab
      public GameObject deleteButton;

      #endregion

      public override void Awake () {
         base.Awake();

         MapCustomizationManager.SelectedPrefabChanged += (pref) => {
            deleteButton.SetActive(pref != null);
            deleteButtonHovered = deleteButtonHovered && deleteButton.activeSelf;
            _selectedPrefab = pref;
         };
      }

      public override void show () {
         base.show();

         deleteButton.SetActive(false);
      }

      private void OnEnable () {
         self = this;

         _cGroup = GetComponent<CanvasGroup>();
      }

      public override void Update () {
         if (!isShowing() || isLoading) return;

         Vector2 pointerPos = Input.mousePosition;
         if (pointerPos != _lastPointerPos) {
            if (Input.GetMouseButton(0)) {
               MapCustomizationManager.pointerDrag(Camera.main.ScreenToWorldPoint(pointerPos) - Camera.main.ScreenToWorldPoint(_lastPointerPos));
            } else {
               MapCustomizationManager.pointerHover(Camera.main.ScreenToWorldPoint(pointerPos));
            }

            _lastPointerPos = pointerPos;
         }

         if (Input.GetMouseButtonDown(0) && _selectedPrefab == null) {
            MapCustomizationManager.pointerDown(Camera.main.ScreenToWorldPoint(pointerPos));
         }

         if (Input.GetMouseButtonUp(0)) {
            MapCustomizationManager.pointerUp(Camera.main.ScreenToWorldPoint(pointerPos), deleteButtonHovered);
         }
      }

      public void deleteButtonEnter() {
         deleteButtonHovered = true; ;
      }

      public void deleteButtonExit() {
         deleteButtonHovered = false;
      }

      public void newPrefabSelected () {
         PlaceablePrefabData data = _placeablePrefabData[prefabDropdown.value];

         MapCustomizationManager.newPrefabClick(data, Camera.main.ScreenToWorldPoint(Input.mousePosition));
      }

      public static void setLoading (bool loading) {
         isLoading = loading;
         self.titleText.text = loading ? "Loading..." : "Customizing a map";
         self.prefabDropdown.gameObject.SetActive(!loading);
      }

      public static void setPlaceablePrefabData (IEnumerable<PlaceablePrefabData> dataCollection) {
         _placeablePrefabData = dataCollection.ToList();

         self.prefabDropdown.options.Clear();
         foreach (PlaceablePrefabData data in _placeablePrefabData) {
            self.prefabDropdown.options.Add(new Dropdown.OptionData {
               image = data.displaySprite,
               text = "Some text"
            });
         }
      }

      public void exitCustomization () {
         MapCustomizationManager.exitCustomization();
      }

      #region Private Variables

      // Canvas group that wraps the entire UI
      private CanvasGroup _cGroup;

      // Last pointer position, cached by this component
      private Vector2 _lastPointerPos;

      // List of prefabs that can be placed in the scene by the user
      private static List<PlaceablePrefabData> _placeablePrefabData = new List<PlaceablePrefabData>();

      // Whether we are currently hovering deleteButton
      private bool deleteButtonHovered = false;

      // Currently selected prefab that is being controlled
      private CustomizablePrefab _selectedPrefab;

      #endregion
   }
}
