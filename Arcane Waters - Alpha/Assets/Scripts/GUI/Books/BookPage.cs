using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
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
   
   // The rect transform
   public RectTransform rectTransform;
      
   #endregion

   private void Awake () {
      image.preserveAspect = true;
   }

   public void setUpPage (PageContent content) {
      text.SetText(content.content);

      if (content.hasImage) {
         setImageWithHeight(content.image.sprite, content.image.height);
      } 

      imageContainer.gameObject.SetActive(content.hasImage);
      pageNumber.SetText(content.pageNumber.ToString());
   }

   public RectTransform getRectTransform () {
      if (rectTransform == null) {
         rectTransform = transform as RectTransform;
      }

      return rectTransform;
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
