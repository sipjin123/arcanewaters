using UnityEngine;
using UnityEngine.UI;

public abstract class SpeakChatRowAction : MonoBehaviour
{
   #region Public Variables

   // Reference to parent SpeakChatRow
   public SpeakChatRow chatRow;

   // Reference to the button that implements this action
   public Button button;

   // Reference to the Tooltip
   public ToolTipComponent tooltip;

   #endregion

   public void Start () {
      if (button) {
         button.onClick.AddListener(onClick);
      }
   }

   private void onClick () {
      execute();
   }

   public void toggle (bool show) {
      gameObject.SetActive(show);
   }

   public abstract void execute ();

   public abstract void refresh ();

   public void OnDestroy () {
      if (button) {
         button.onClick.RemoveAllListeners();
      }
   }

   #region Private Variables

   #endregion
}
