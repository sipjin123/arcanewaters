using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BookPage : MonoBehaviour
{
   #region Public Variables

   // Set to true if this is the left page
   public bool isLeftPage;

   // The page text
   public TextMeshProUGUI contentText;

   // The page number text
   public TextMeshProUGUI pageNumberText;

   // The firstVisibleCharacter of the left page before this
   public int previousFirstVisibleCharacter;

   #endregion

   public void setPageNumber (int pageNumber) {
      pageNumberText.SetText(pageNumber.ToString());
   }

   public void clearPage () {
      pageNumberText.SetText("");
      contentText.SetText("");
   }

   #region Private Variables

   #endregion
}
