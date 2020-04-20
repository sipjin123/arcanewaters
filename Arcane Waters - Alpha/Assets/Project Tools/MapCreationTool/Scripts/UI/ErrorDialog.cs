using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class ErrorDialog : UIPanel
   {
      [SerializeField]
      private Color errorColor = new Color();
      [SerializeField]
      private Color infoColor = new Color();

      [SerializeField]
      private Text titleText = null;
      [SerializeField]
      private Text contentText = null;

      public void display (string title, string content) {
         contentText.color = errorColor;
         titleText.text = title;
         contentText.text = content;

         show();
      }

      public void displayNotImplemented () {
         display("Not implemented", "This feature has not been implemented yet");
      }

      public void displayUnauthorized (string content) {
         display("Unauthorized", content);
      }

      public void displayInfoMessage (string title, string content) {
         contentText.color = infoColor;
         titleText.text = title;
         contentText.text = content;

         show();
      }

      public void display (string content) {
         display("Error", content);
      }

      public void closeButton_click () {
         hide();
      }
   }
}