using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BookPage : MonoBehaviour
{
   #region Public Variables

   // The page text
   public TextMeshProUGUI text;

   // The page number text
   public TextMeshProUGUI pageNumber;

   // The page image
   public Image image;

   // The image container layout element
   public LayoutElement imageContainer;

   #endregion

   private void Awake () {
      image.preserveAspect = true;
   }

   public void setUpPage (PageContent content) {
      text.text = content.content;

      if (content.hasImage) {
         imageContainer.minHeight = content.image.height;
         imageContainer.preferredHeight = content.image.height;
         image.sprite = content.image.sprite;
      }

      imageContainer.gameObject.SetActive(content.hasImage);
      pageNumber.text = content.pageNumber.ToString();

   }

   public void clearPage () {
      pageNumber.SetText("");
      imageContainer.gameObject.SetActive(false);
      text.SetText("");
   }

   public void setImageWithHeight (Sprite sprite, int height) {
      imageContainer.minHeight = height;
      imageContainer.flexibleHeight = 0;
      imageContainer.preferredHeight = height;
      image.sprite = sprite;
      imageContainer.gameObject.SetActive(true);
   }

   #region Private Variables

   #endregion
}
