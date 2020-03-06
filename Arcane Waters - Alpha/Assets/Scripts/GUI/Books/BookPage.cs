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
      
   // Text layout information - The height (in pixels) of each line in the text
   [HideInInspector] 
   public float textLineHeight;

   // The height of the text container
   [HideInInspector] 
   public float textContainerFullHeight;

   // The max number of characters that can fit in a line
   [HideInInspector] 
   public int maxCharactersPerLine;

   // The max number of characters that can fit in a page
   [HideInInspector] 
   public int maxCharactersFullPage;

   #endregion

   private void Awake () {
      image.preserveAspect = true;
   }

   public void updateLayoutValues () {
      textLineHeight = text.font.faceInfo.lineHeight;
      textContainerFullHeight = getRectTransform().rect.height;
      maxCharactersPerLine = Mathf.CeilToInt(getRectTransform().rect.width / text.fontSize * 2);
      maxCharactersFullPage = Mathf.CeilToInt(textContainerFullHeight / textLineHeight) * maxCharactersPerLine;
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
