using UnityEngine;
using UnityEngine.UI;

namespace MapCustomization
{
   public class PrefabSelectionEntry : MonoBehaviour
   {
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

      // The prop category this entry is targeting
      public Item.Category propItemCategory;

      // Arrow buttons for selecting different prefabs
      public Button previousButton;
      public Button nextButton;

      #endregion

      public PlaceablePrefabData getSelectedData () {
         return _prefabs[_selectedIndex];
      }

      public void setData (int propDefinitionId, Item.Category category, PlaceablePrefabData[] prefabs) {
         this.propDefinitionId = propDefinitionId;
         propItemCategory = category;
         _prefabs = prefabs;

         previousButton.gameObject.SetActive(prefabs.Length > 1);
         nextButton.gameObject.SetActive(prefabs.Length > 1);

         selectIndex(0);
      }

      private void selectIndex (int index) {
         _selectedIndex = index;
         iconImage.sprite = _prefabs[index].displaySprite;

         //SoundEffectManager.self.playSoundEffect(SoundEffectManager.NEXT_PREFAB_SELECTION, SoundEffectManager.self.transform);
      }

      public void setSelected (bool selected) {
         frameImage.sprite = selected ? selectedFrame : defaultFrame;
      }

      public void setCount (int count) {
         _count = count;

         countText.text = count.ToString();
         canvasGroup.alpha = count > 0 ? 1f : 0.5f;
         canvasGroup.interactable = count > 0;
         canvasGroup.blocksRaycasts = count > 0;
      }

      public int getCount () {
         return _count;
      }

      public void onClick () {
         CustomizationUI.prefabEntryClick(this);
      }

      public void onPrevious () {
         if (_selectedIndex == 0) {
            selectIndex(_prefabs.Length - 1);
         } else {
            selectIndex(_selectedIndex - 1);
         }
      }

      public void onNext () {
         if (_selectedIndex == _prefabs.Length - 1) {
            selectIndex(0);
         } else {
            selectIndex(_selectedIndex + 1);
         }
      }

      #region Private Variables

      // Prefabs that can be selected by this entry
      private PlaceablePrefabData[] _prefabs;

      // Which prefab is currently selected
      private int _selectedIndex;

      // How many of this prop is left
      private int _count = -1;

      #endregion
   }
}