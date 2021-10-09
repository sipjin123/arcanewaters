using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using MapCreationTool.Serialization;

public class GraveToolTip : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The message upon hover
   public string toolTipMessage;

   // The tooltip panel
   public GameObject toolTipPanel;

   // The text of the tooltip
   public TextMeshProUGUI toolTipText;

   // The rect transform of the tooltip canvas
   public RectTransform toolTipRectTransform;

   // The tooltip canvas group
   public CanvasGroup toolTipCanvasGroup;

   // The message
   public static string MESSAGE = "Here lies ";

   #endregion

   protected virtual void Awake () {
      toolTipText.SetText(toolTipMessage);
   }

   public virtual void toggleToolTip (bool isActive) {
      if (toolTipPanel.activeSelf == isActive) {
         return;
      }

      toolTipPanel.SetActive(isActive);
      toolTipCanvasGroup.alpha = 0f;

      StopAllCoroutines();
      if (isActive) {
         StartCoroutine(CO_ShowTooltip());
      }
   }

   private IEnumerator CO_ShowTooltip () {
      // Wait for the panel to be drawn
      yield return null;

      // Reset position of tooltip to default
      toolTipRectTransform.anchoredPosition = new Vector2(0,  toolTipRectTransform.rect.size.y / 2);

      // Check if tooltip repositioning is needed
      TooltipManager.self.keepToolTipOnScreen(toolTipRectTransform);

      // Show the tooltip
      toolTipCanvasGroup.alpha = 1;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.GRAVE_NAME) == 0) {
            string graveName = field.v.Split(':')[0];
            toolTipMessage = MESSAGE + graveName;
            toolTipText.SetText(toolTipMessage);
         }

         // If the grave stone text is used, it overwrites the name
         if (field.k.CompareTo(DataField.GRAVE_TEXT) == 0) {
            string graveText = field.v;
            if (!string.IsNullOrEmpty(graveText)) {
               toolTipText.SetText(field.v);
            }
         }

      }
   }

   #region Private Variables

   #endregion
}
