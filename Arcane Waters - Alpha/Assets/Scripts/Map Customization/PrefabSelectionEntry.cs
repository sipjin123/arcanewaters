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

      // Target prefab that this entry is referencing
      public PlaceablePrefabData target;

      // Image that is displaying the frame of the entry
      public Image frameImage;

      // Sprite that is used for the frame when the entry is at a default state
      public Sprite defaultFrame;

      // Sprite that is used for the frame when the entry is selected
      public Sprite selectedFrame;

      #endregion

      public void setSelected (bool selected) {
         frameImage.sprite = selected ? selectedFrame : defaultFrame;
      }

      public void setImage (Sprite sprite) {
         iconImage.sprite = sprite;
      }

      public void setCount (int count) {
         countText.text = count.ToString();
         canvasGroup.alpha = count > 0 ? 1f : 0.5f;
         canvasGroup.interactable = count > 0;
         canvasGroup.blocksRaycasts = count > 0;
      }

      public void onClick () {
         CustomizationUI.selectEntry(this);
      }

      #region Private Variables

      #endregion
   }
}