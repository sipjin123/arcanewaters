using UnityEngine;
using UnityEngine.UI;

public class SpeakChatRow : MonoBehaviour
{
   #region Public Variables

   // Reference to the chat line
   public SpeakChatLine chatLine;

   // Reference to the GameObject that expresses the highlighted state
   public GameObject highlighter;
	
   // Reference to the generic icon
   public Image genericIcon;
	
   #endregion

   public void toggleHighlight(bool show) {
      if (highlighter == null) {
         return;
      }

      highlighter.gameObject.SetActive(show);
   }

   public void toggleGenericIcon (bool show) {
      if (genericIcon == null) {
         return;
      }

      genericIcon.gameObject.SetActive(show);
   }

   public void setGenericIcon(Sprite sprite) {
      if (genericIcon == null || sprite == null) {
         return;
      }

      genericIcon.sprite = sprite;
   }

   #region Private Variables

   #endregion
}
