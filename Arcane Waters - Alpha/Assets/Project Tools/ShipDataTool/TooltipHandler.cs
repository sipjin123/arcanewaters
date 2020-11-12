using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class TooltipHandler : MonoBehaviour
{
   #region Public Variables

   // Self
   public static TooltipHandler self;

   // Tool tip UI components
   public GameObject toolTipPanel;
   public TextMeshProUGUI toolTipTMPText;
   public RectTransform backgroundRect;
   public static int WIDTH_THRESHOLD = 100;

   #endregion

   private void Awake () {
      self = this;
   }

   public void callToolTip (string msg, ToolTipComponent.TooltipPlacement placement, Vector3 elementPosition, GameObject panelRoot) {
         toolTipTMPText.SetText(msg);

      // Coroutine needed to allow TMP rect transform to update at end of frame
      StartCoroutine(CO_placeTooltip(placement, elementPosition, panelRoot));
   }

   public IEnumerator CO_placeTooltip (ToolTipComponent.TooltipPlacement placement, Vector3 elementPosition, GameObject panelRoot) {

      // Wait for end of frame for rect transform to update correctly
      toolTipTMPText.ForceMeshUpdate();
      backgroundRect.ForceUpdateRectTransforms();
      yield return new WaitForEndOfFrame();

      // Find the width and height of the tooltip
      Vector3[] cornersTooltip = new Vector3[4];
      backgroundRect.gameObject.GetComponent<RectTransform>().GetWorldCorners(cornersTooltip);
      _tooltipDimensions = new Vector2(cornersTooltip[2].x - cornersTooltip[1].x, cornersTooltip[2].y - cornersTooltip[3].y);

      //  If this tooltip belongs to a panel, find the dimensions of the panel
      if (panelRoot != null) {
         Vector3[] cornersPanel = new Vector3[4];
         panelRoot.gameObject.GetComponent<RectTransform>().GetWorldCorners(cornersPanel);
         _leftEdge = cornersPanel[0].x;
         _rightEdge = cornersPanel[2].x;
         _middleOfEdge = cornersPanel[0].y + (cornersPanel[1].y - cornersPanel[0].y) / 2;
      }

      // Place the panel according to Enum selection
      switch (placement) {
         case ToolTipComponent.TooltipPlacement.AboveUIElement:
            placeAboveUIELement(elementPosition);
            break;
         case ToolTipComponent.TooltipPlacement.AutoPlacement:
            placeAutomatically(elementPosition, panelRoot);
            break;
         case ToolTipComponent.TooltipPlacement.LeftSideOfPanel:
            placeOnLeftSideOfPanel(_leftEdge, _middleOfEdge, _tooltipDimensions);
            break;
         case ToolTipComponent.TooltipPlacement.RightSideOfPanel:
            placeOnRightSideOfPanel(_rightEdge, _middleOfEdge, _tooltipDimensions);
            break;
      }

      // Turn the tooltip visible
      toolTipPanel.GetComponent<CanvasGroup>().alpha = 1;
      toolTipPanel.GetComponent<CanvasGroup>().blocksRaycasts = false;
   }

   public void cancelToolTip () {
      toolTipPanel.GetComponent<CanvasGroup>().alpha = 0;
      toolTipPanel.GetComponent<HorizontalLayoutGroup>().childControlWidth = true;
      backgroundRect.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
      toolTipPanel.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = false;
   }

   public void placeAutomatically (Vector3 elementPosition, GameObject panelRoot) {
      // If the tooltip is small, place it above the element
      if (_tooltipDimensions.x < WIDTH_THRESHOLD) {
         placeAboveUIELement(elementPosition);
      }
      // If the tooltip is large, place it to the side of panel that is closest to the element
      else 
      if (elementPosition.x < panelRoot.transform.position.x) {
         placeOnLeftSideOfPanel(_leftEdge, _middleOfEdge, _tooltipDimensions);
      } 
      else {
         placeOnRightSideOfPanel(_rightEdge, _middleOfEdge, _tooltipDimensions);
      }
   }

   public void placeAboveUIELement (Vector3 elementPosition) {
      toolTipPanel.transform.position = new Vector3(elementPosition.x, elementPosition.y + _offSetY, 0);
   }

   public void placeOnLeftSideOfPanel (float leftEdge, float middleOfEdge, Vector2 _tooltipDimensions) {
      toolTipPanel.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
      toolTipPanel.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
      backgroundRect.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
      Vector3 calculatedPosition = new Vector3(leftEdge - _tooltipDimensions.x / 2 - _offSetX, middleOfEdge, 0);
      toolTipPanel.transform.position = calculatedPosition;
   }

   public void placeOnRightSideOfPanel (float rightEdge, float middleOfEdge, Vector2 _tooltipDimensions) {
      toolTipPanel.GetComponent<HorizontalLayoutGroup>().childControlWidth = false;
      toolTipPanel.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
      backgroundRect.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
      toolTipPanel.transform.position = new Vector3(rightEdge + _tooltipDimensions.x / 2 + _offSetX, middleOfEdge, 0);
   }

   #region Private Variables

   // Dimensions of the populated tooltip
   private Vector2 _tooltipDimensions;

   // Offset in Y axis for padding
   private float _offSetY = 50;

   // Offset in X axis for padding
   private float _offSetX = 10;

   // The left edge x value of the tooltip
   private float _leftEdge;

   // The right edge x value of the tooltip
   private float _rightEdge;

   // The midpoint of the tooltip height
   private float _middleOfEdge;

   #endregion
}
