using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class MessagePanel : UIPanel
   {
      [SerializeField]
      private Text titleText = null;
      [SerializeField]
      private Text contentText = null;
      [SerializeField]
      private int maxErrorCharacters = 1000;

      [SerializeField, Space(5)]
      private Color infoColor = new Color();
      [SerializeField]
      private Color warningColor = new Color();
      [SerializeField]
      private Color errorColor = new Color();

      public void displayInfo (string title, string content) {
         titleText.color = infoColor;
         contentText.color = infoColor;

         titleText.text = title.ToUpper();
         contentText.text = content;

         show();
      }

      public void displayWarning (string title, string content) {
         titleText.color = warningColor;
         contentText.color = warningColor;

         titleText.text = title.ToUpper();
         contentText.text = content;

         show();
      }

      public void displayWarning (string content) => displayWarning("Warning", content);

      public void displayError (string title, string content) {
         titleText.color = errorColor;
         contentText.color = errorColor;

         titleText.text = title.ToUpper();
         contentText.text = content.Length > maxErrorCharacters ? content.Substring(0, maxErrorCharacters) : content;

         show();
      }

      public void displayError (string content) {
         displayError("Error", content);
      }

      public void displayNotImplemented () {
         displayError("Not implemented", "This feature has not been implemented yet");
      }

      public void displayUnauthorized (string content) {
         displayError("Unauthorized", content);
      }

      public void closeButton_click () {
         hide();
      }
   }
}
