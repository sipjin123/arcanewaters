using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class ErrorDialog : MonoBehaviour
   {
      private CanvasGroup cGroup;

      [SerializeField]
      private Text titleText = null;
      [SerializeField]
      private Text contentText = null;

      private void Awake () {
         cGroup = GetComponent<CanvasGroup>();
         hide();
      }

      public void display (string title, string content) {
         titleText.text = title;
         contentText.text = content;

         show();
      }

      public void closeButton_click () {
         hide();
      }


      private void hide () {
         cGroup.alpha = 0;
         cGroup.blocksRaycasts = false;
         cGroup.interactable = false;
      }

      private void show () {
         cGroup.alpha = 1;
         cGroup.blocksRaycasts = true;
         cGroup.interactable = true;
      }
   }
}