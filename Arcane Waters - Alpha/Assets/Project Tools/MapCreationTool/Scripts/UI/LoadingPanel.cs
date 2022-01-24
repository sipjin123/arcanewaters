using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class LoadingPanel : UIPanel
   {
      [SerializeField]
      private Text titleText = null;

      private void Update () {
         if (targetTask != null && targetTask.IsCompleted) {
            targetTask = null;
            hide();
         }
      }

      public void display (string title) {
         titleText.text = title;

         show();
      }

      public void display (string title, UnityThreading.Task displayWhileTask) {
         display(title);

         displayWhileTask.TaskEnded += (handler) => UnityThreadHelper.UnityDispatcher.Dispatch(() => hide());
      }

      public void display (string title, Task displayWhileTask) {
         display(title);
         targetTask = displayWhileTask;
      }

      public void close () {
         hide();
      }

      public void closeButton_click () {
         hide();
      }

      protected override void hide () {
         base.hide();

         // Return to the default behaviour of pausing while in background
         Application.runInBackground = false;
      }

      protected override void show () {
         base.show();

         // While we are loading something, make it the application run in the background
         Application.runInBackground = true;
      }

      // Task, for which we are showing loading panel
      private Task targetTask = null;
   }
}