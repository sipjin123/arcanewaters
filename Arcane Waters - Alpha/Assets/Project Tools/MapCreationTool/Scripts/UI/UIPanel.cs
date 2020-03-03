using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class UIPanel : MonoBehaviour
   {
      protected CanvasGroup cGroup;

      public bool showing { get; private set; }

      protected virtual void Awake () {
         cGroup = GetComponent<CanvasGroup>();
         hide();
      }

      protected virtual void hide () {
         cGroup.alpha = 0;
         cGroup.blocksRaycasts = false;
         cGroup.interactable = false;
         showing = false;
      }

      protected virtual void show () {
         cGroup.alpha = 1;
         cGroup.blocksRaycasts = true;
         cGroup.interactable = true;
         showing = true;

         cGroup.transform.SetAsLastSibling();
      }
   }
}
