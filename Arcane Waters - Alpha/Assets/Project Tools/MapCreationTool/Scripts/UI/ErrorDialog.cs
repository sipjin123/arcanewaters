using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class ErrorDialog : UIPanel
   {
      [SerializeField]
      private Text titleText = null;
      [SerializeField]
      private Text contentText = null;

      public void display (string title, string content) {
         titleText.text = title;
         contentText.text = content;

         show();
      }

      public void display (string content) {
         titleText.text = "Error";
         contentText.text = content;

         show();
      }

      public void closeButton_click () {
         hide();
      }
   }
}