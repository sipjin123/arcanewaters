using UnityEngine;
using UnityEngine.UI;

public class GenericActionPromptScreen : MonoBehaviour
{
   #region Public Variables

   // Self
   public static GenericActionPromptScreen self;

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // A reference to the text component displayed on the button
   public Text buttonText;

   // Reference to the respawn button
   public Button button;

   // Reference to the content of the screen
   public RectTransform content;

   // The text displayed on the button
   public string text;

   #endregion

   private void Awake () {
      self = this;
   }

   public void Update () {
      buttonText.text = text;
   }

   public void show () {
      if (!this.canvasGroup.IsShowing()) {
         // Hide tutorial panel so it doesn't block the respawn button
         if (TutorialManager3.self.panel.getMode() != TutorialPanel3.Mode.Closed) {
            TutorialManager3.self.panel.gameObject.SetActive(false);
         }

         content.gameObject.SetActive(true);
         this.canvasGroup.Show();
      }
   }

   public void hide () {
      if (this.canvasGroup.IsShowing()) {
         this.canvasGroup.Hide();
      }
   }

   public bool isShowing () {
      return this.canvasGroup.IsShowing();
   }

   #region Private Variables

   #endregion
}
