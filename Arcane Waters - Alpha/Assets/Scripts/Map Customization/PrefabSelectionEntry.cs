using UnityEngine;
using UnityEngine.UI;

namespace MapCustomization {
   public class PrefabSelectionEntry : MonoBehaviour {
      #region Public Variables

      // Image that is showing the prefab's icon
      public Image iconImage;

      // Canvas group of the whole entry
      public CanvasGroup canvasGroup;

      // Text that shows how much of the prefab is left
      public Text countText;

      // Image that is displaying the frame of the entry
      public Image frameImage;

      // Sprite that is used for the frame when the entry is at a default state
      public Sprite defaultFrame;

      // Sprite that is used for the frame when the entry is selected
      public Sprite selectedFrame;

      // The prop item definition id that this entry is targeting
      public int propDefinitionId;

      // Arrow buttons for selecting different prefabs
      public Button previousButton;
      public Button nextButton;

      #endregion

      public PlaceablePrefabData getSelectedData () {
         return _prefabs[_selectedIndex];
      }

      public void setData (int propDefinitionId, PlaceablePrefabData[] prefabs) {
         this.propDefinitionId = propDefinitionId;
         _prefabs = prefabs;

         previousButton.gameObject.SetActive(prefabs.Length > 1);
         nextButton.gameObject.SetActive(prefabs.Length > 1);

         selectIndex(0);
      }

      private void selectIndex (int index) {
         _selectedIndex = index;
         iconImage.sprite = _prefabs[index].displaySprite;
         previousButton.interactable = _selectedIndex != 0;
         nextButton.interactable = _selectedIndex != _prefabs.Length - 1;
         SoundEffectManager.self.playSoundEffect(SoundEffectManager.NEXTPREFAB_SELECTION, SoundEffectManager.self.transform);
      }

      public void setSelected (bool selected) {
         frameImage.sprite = selected ? selectedFrame : defaultFrame;
      }

      public void setCount (int count) {
         countText.text = count.ToString();
         canvasGroup.alpha = count > 0 ? 1f : 0.5f;
         canvasGroup.interactable = count > 0;
         canvasGroup.blocksRaycasts = count > 0;
      }

      public void onClick () {
         CustomizationUI.prefabEntryClick(this);
      }

      public void onPrevious () {
         if (_selectedIndex == 0) return;
         selectIndex(_selectedIndex - 1);
         previousButton.interactable = _selectedIndex != 0;
         nextButton.interactable = _selectedIndex != _prefabs.Length - 1;
      }

      public void onNext () {
         if (_selectedIndex == _prefabs.Length - 1) return;
         selectIndex(_selectedIndex + 1);
      }

      #region Private Variables

      // Prefabs that can be selected by this entry
      private PlaceablePrefabData[] _prefabs;

      // Which prefab is currently selected
      private int _selectedIndex;

      #endregion
   }
}