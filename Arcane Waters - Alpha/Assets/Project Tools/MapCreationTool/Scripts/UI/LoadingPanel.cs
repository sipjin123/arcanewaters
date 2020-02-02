using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class LoadingPanel : UIPanel
   {
      [SerializeField]
      private Text titleText = null;

      public void display (string title) {
         titleText.text = title;

         show();
      }

      public void display (string title, UnityThreading.Task displayWhileTask) {
         display(title);

         displayWhileTask.TaskEnded += (handler) => UnityThreadHelper.UnityDispatcher.Dispatch(() => hide());
      }

      public void close () {
         hide();
      }

      public void closeButton_click () {
         hide();
      }
   }
}