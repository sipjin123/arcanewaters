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
   public HorizontalLayoutGroup tooltipHorizontalLayoutGroup;
   public HorizontalLayoutGroup backgroundHorizontalLayoutGroup;
   public ContentSizeFitter tooltipContentSizeFitter;
   public static int WIDTH_THRESHOLD = 150;

   // The Y value of the top edge of the tooltip owner
   public float tooltipOwnerTopEdge;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // Look up components
      backgroundHorizontalLayoutGroup = backgroundRect.GetComponent<HorizontalLayoutGroup>();
      tooltipHorizontalLayoutGroup = toolTipPanel.GetComponent<HorizontalLayoutGroup>();
      tooltipContentSizeFitter = GetComponent<ContentSizeFitter>();
   }

   private void OnEnable () {
      Panel.OnPanelOpened += cancelToolTip;
      Panel.OnPanelClosed += cancelToolTip;
   }

   private void OnDisable () {
      Panel.OnPanelOpened -= cancelToolTip;
      Panel.OnPanelClosed -= cancelToolTip;
   }

   public void callToolTip (GameObject tooltipOwner, string msg, ToolTipComponent.TooltipPlacement placement, Vector3 elementPosition, GameObject panelRoot, float width) {
         toolTipTMPText.SetText(msg);

      // Coroutine needed to allow TMP rect transform to update at end of frame
      StartCoroutine(CO_placeTooltip(tooltipOwner, placement, elementPosition, panelRoot, width));
   }

   public IEnumerator CO_placeTooltip (GameObject tooltipOwner, ToolTipComponent.TooltipPlacement placement, Vector3 elementPosition, GameObject panelRoot, float width) {
      // Wait for end of frame for rect transform to update correctly
      toolTipTMPText.ForceMeshUpdate();
      backgroundRect.ForceUpdateRectTransforms();
      yield return new WaitForEndOfFrame();

      // Set width of tooltip. If width is set to zero, the tooltip will be sized automatically.
      if (width > 0) {
         setTooltipToSizeManually(width);
      } else {
         setTooltipToSizeAutomatically();
      }

      // Find the top edge of the tooltip owner
      Vector3[] cornersTooltipOwner = new Vector3[4];
      tooltipOwner.GetComponent<RectTransform>().GetWorldCorners(cornersTooltipOwner);
      tooltipOwnerTopEdge = cornersTooltipOwner[2].y;

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

      // If tooltip does not fit in the desired loaction, place it above the element or shift it back on screen.
      if (isTooltipOffScreen()) {
         if (tooltipOwner.GetComponent<ToolTipComponent>().forceFitOnScreen == false) {
            placeAboveUIELement(elementPosition);
         } else {
            // If tooltip is off the right edge of screen
            if (_rightEdge + _tooltipDimensions.x + _offSetX > Screen.width) {
               toolTipPanel.transform.position = new Vector3(_rightEdge - _tooltipDimensions.x / 2, tooltipOwnerTopEdge + _tooltipDimensions.y / 2 + _offSetY, 0);
            } else {
               // If tooltip is off the left edge of screen
               toolTipPanel.transform.position = new Vector3(_rightEdge - _tooltipDimensions.x / 2, tooltipOwnerTopEdge + _tooltipDimensions.y / 2 + _offSetY, 0);
            }
         }
      }

      // Turn the tooltip visible
      toolTipPanel.GetComponent<CanvasGroup>().alpha = 1;
      toolTipPanel.GetComponent<CanvasGroup>().blocksRaycasts = false;
   }

   public bool isTooltipOffScreen () {
      return (toolTipPanel.transform.position.x + _tooltipDimensions.x/2 + _offSetX > Screen.width) || (toolTipPanel.transform.position.x - _tooltipDimensions.x - _offSetX < 0);
   }

   public void setTooltipToSizeAutomatically () {
      tooltipHorizontalLayoutGroup.childControlWidth = true;
      backgroundHorizontalLayoutGroup.childForceExpandWidth = false;
      tooltipHorizontalLayoutGroup.childForceExpandWidth = false;
      tooltipContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
   }

   // Uses the width pulled from tooltip database. *** TO DO: Database implementation ***
   public void setTooltipToSizeManually (float width) {
      tooltipHorizontalLayoutGroup.childControlWidth = false;
      tooltipHorizontalLayoutGroup.childForceExpandWidth = true;
      backgroundHorizontalLayoutGroup.childForceExpandWidth = true;
      tooltipContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
      GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
      backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
   }

   public void cancelToolTip () {
      toolTipPanel.GetComponent<CanvasGroup>().alpha = 0;
      tooltipHorizontalLayoutGroup.childControlWidth = true;
      backgroundHorizontalLayoutGroup.childForceExpandWidth = false;
      tooltipHorizontalLayoutGroup.childForceExpandWidth = false;
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
      toolTipPanel.transform.position = new Vector3(elementPosition.x, tooltipOwnerTopEdge + _tooltipDimensions.y / 2 + _offSetY );
   }

   public void placeOnLeftSideOfPanel (float leftEdge, float middleOfEdge, Vector2 _tooltipDimensions) {
      tooltipHorizontalLayoutGroup.childControlWidth = false;
      tooltipHorizontalLayoutGroup.childForceExpandWidth = true;
      backgroundHorizontalLayoutGroup.childForceExpandWidth = true;
      toolTipPanel.transform.position = new Vector3(leftEdge - _tooltipDimensions.x / 2 , middleOfEdge, 0);
   }

   public void placeOnRightSideOfPanel (float rightEdge, float middleOfEdge, Vector2 _tooltipDimensions) {
      tooltipHorizontalLayoutGroup.childControlWidth = false;
      tooltipHorizontalLayoutGroup.childForceExpandWidth = true;
      backgroundHorizontalLayoutGroup.childForceExpandWidth = true;
      toolTipPanel.transform.position = new Vector3(rightEdge + _tooltipDimensions.x / 2 , middleOfEdge, 0);
   }

   #region Private Variables

   // Dimensions of the populated tooltip
   private Vector2 _tooltipDimensions;

   // Offset in Y axis for padding
   private float _offSetY = 15;

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
