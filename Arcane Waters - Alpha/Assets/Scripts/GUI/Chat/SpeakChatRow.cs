using UnityEngine;

public class SpeakChatRow : MonoBehaviour
{
   #region Public Variables

   // Reference to the chat line
   public SpeakChatLine chatLine;

   // Reference to the GameObject that expresses the highlighted state
   public GameObject highlighter;

   #endregion

   public void toggleHighlight(bool show) {
      if (highlighter == null) {
         return;
      }

      highlighter.gameObject.SetActive(show);
   }

   #region Private Variables

   #endregion
}
