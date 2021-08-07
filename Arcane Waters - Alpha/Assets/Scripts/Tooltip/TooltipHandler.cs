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
   public Image backgroundImage;
   public Image starsImage;
   public ToolTipComponent.Type toolTipType;
   public GameObject defaultTooltipPanel;
   public GameObject inventoryTooltipPanel;
   public CanvasGroup tooltipCanvasGroup;

   // The Y value of the top edge of the tooltip owner
   public float tooltipOwnerTopEdge;

   // The directory for the rarity sprites
   public static string RARITY_PATH = "Assets/Resources/Sprites/GUI/ItemTooltips/";

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      toolTipPanel = defaultTooltipPanel;
   }
   private void OnEnable () {
      Panel.OnPanelOpened += cancelToolTip;
      Panel.OnPanelClosed += cancelToolTip;
   }

   private void OnDisable () {
      Panel.OnPanelOpened -= cancelToolTip;
      Panel.OnPanelClosed -= cancelToolTip;
   }

   public void callToolTip (Rarity.Type itemRarityType, GameObject tooltipOwner, ToolTipComponent.Type type, string msg, ToolTipComponent.TooltipPlacement placement, Vector3 elementPosition, GameObject panelRoot, float width) {
      // Choose the inventory style panel for the tooltip display
      toolTipType = type;
      if (toolTipType == ToolTipComponent.Type.ItemCellInventory) {
         toolTipPanel = inventoryTooltipPanel;
         backgroundRect = toolTipPanel.GetComponentInChildren<RectTransform>();
         prepTooltipForInventoryItem(msg, itemRarityType);

      } else {
         // Choose the default style panel for the tooltip display
         toolTipPanel = defaultTooltipPanel;
         backgroundRect = toolTipPanel.GetComponentInChildren<RectTransform>();
         backgroundHorizontalLayoutGroup = backgroundRect.GetComponent<HorizontalLayoutGroup>();
         tooltipHorizontalLayoutGroup = toolTipPanel.GetComponent<HorizontalLayoutGroup>();
         tooltipContentSizeFitter = GetComponent<ContentSizeFitter>();
         toolTipPanel.GetComponentInChildren<TextMeshProUGUI>().SetText(msg);
      }

      // Get a reference to the tooltip canvas group
      tooltipCanvasGroup = toolTipPanel.GetComponent<CanvasGroup>();

      // Add to list of currently open tooltips
      UIToolTipManager.openTooltips.Add(toolTipPanel);

      // Coroutine needed to allow TMP rect transform to update at end of frame
      StartCoroutine(CO_placeTooltip(tooltipOwner, placement, toolTipType, elementPosition, panelRoot, width));
   }

   public IEnumerator CO_placeTooltip (GameObject tooltipOwner, ToolTipComponent.TooltipPlacement placement, ToolTipComponent.Type type, Vector3 elementPosition, GameObject panelRoot, float width) {
      // Wait for end of frame for rect transform to update correctly
      backgroundRect.ForceUpdateRectTransforms();
      yield return new WaitForEndOfFrame();

      // Set width of tooltip. If width is set to zero, the tooltip will be sized automatically.
      if (type != ToolTipComponent.Type.ItemCellInventory) {
         if (width > 0) {
            setTooltipToSizeManually(width);
         } else {
            setTooltipToSizeAutomatically();
         }
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

      float backgroundWidth = toolTipPanel.gameObject.transform.GetChild(0).GetComponent<RectTransform>().sizeDelta.x;

      // If tooltip does not fit in the desired loaction, place it above the element or shift it back on screen.
      if (isTooltipOffScreen()) {
         if (tooltipOwner.GetComponent<ToolTipComponent>().nudgeAwayFromBorder == false) {
            placeAboveUIELement(elementPosition);
         } else {
            // If tooltip is off the right edge of screen
            if (_rightEdge + backgroundRect.sizeDelta.x + _offSetX > Screen.width) {
               toolTipPanel.transform.position = new Vector3(Screen.width - (backgroundWidth / 2 - (Screen.width - toolTipPanel.transform.position.x)) - (3 * _offSetX), tooltipOwnerTopEdge + _tooltipDimensions.y / 2 + _offSetY, 0);
            } else {
               // If tooltip is off the left edge of screen
               toolTipPanel.transform.position = new Vector3(backgroundWidth / 2 - toolTipPanel.transform.position.x + (3 * _offSetX), tooltipOwnerTopEdge + _tooltipDimensions.y / 2 + _offSetY, 0);
            }
         }
      }

      // Turn the tooltip visible
      toolTipPanel.GetComponent<CanvasGroup>().alpha = 1;
      toolTipPanel.GetComponent<CanvasGroup>().blocksRaycasts = false;
   }

   public void prepTooltipForInventoryItem (string message, Rarity.Type itemRarity) {
      CanvasGroup canvasGroup = toolTipPanel.GetComponent<CanvasGroup>();
      TextMeshProUGUI[] textArray = toolTipPanel.GetComponentsInChildren<TextMeshProUGUI>();

      Image backgroundSprite = backgroundImage;
      Image starSprite = starsImage;

      // Display correct background and stars for rarity level
      switch (itemRarity) {
         case Rarity.Type.None:
            break;
         case Rarity.Type.Common:
            backgroundSprite.sprite = ImageManager.getSprite(RARITY_PATH + "CommonTooltip");
            starSprite.sprite = ImageManager.getSprite(RARITY_PATH + "CommonStars");
            break;
         case Rarity.Type.Uncommon:
            backgroundSprite.sprite = ImageManager.getSprite(RARITY_PATH + "UncommonTooltip");
            starSprite.sprite = ImageManager.getSprite(RARITY_PATH + "UncommonStars");
            break;
         case Rarity.Type.Rare:
            backgroundSprite.sprite = ImageManager.getSprite(RARITY_PATH + "RareTooltip");
            starSprite.sprite = ImageManager.getSprite(RARITY_PATH + "RareStars");
            break;
         case Rarity.Type.Epic:
            backgroundSprite.sprite = ImageManager.getSprite(RARITY_PATH + "EpicTooltip");
            starSprite.sprite = ImageManager.getSprite(RARITY_PATH + "EpicStars");
            break;
         case Rarity.Type.Legendary:
            backgroundSprite.sprite = ImageManager.getSprite(RARITY_PATH + "LegendaryTooltip");
            starSprite.sprite = ImageManager.getSprite(RARITY_PATH + "LegendaryStars");
            break;

      }
      textArray[0].text = itemRarity.ToString();
      textArray[1].text = message;
   }

   public bool isTooltipOffScreen () {
      return (toolTipPanel.transform.position.x + _tooltipDimensions.x/2 + _offSetX > Screen.width) || (toolTipPanel.transform.position.x - _tooltipDimensions.x - _offSetX < 0);
   }

   public void setTooltipToSizeAutomatically () {
      tooltipHorizontalLayoutGroup.childControlWidth = true;
      if (backgroundHorizontalLayoutGroup != null) {
         backgroundHorizontalLayoutGroup.childForceExpandWidth = false;
      }
      tooltipHorizontalLayoutGroup.childForceExpandWidth = false;
      if (tooltipContentSizeFitter != null) {
         tooltipContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
      }
   }

   // Uses the width pulled from tooltip database. *** TO DO: Database implementation ***
   public void setTooltipToSizeManually (float width) {
      tooltipHorizontalLayoutGroup.childControlWidth = false;
      tooltipHorizontalLayoutGroup.childForceExpandWidth = true;
      if (backgroundHorizontalLayoutGroup != null) {
         backgroundHorizontalLayoutGroup.childForceExpandWidth = true;
      }
      if (tooltipContentSizeFitter != null) {
         tooltipContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
      }
      GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
      backgroundRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
   }

   public void cancelToolTip () {
      if (tooltipCanvasGroup != null) {
         if (tooltipCanvasGroup.alpha != 0) {
            tooltipCanvasGroup.alpha = 0;
            if (tooltipHorizontalLayoutGroup != null) {
               tooltipHorizontalLayoutGroup.childControlWidth = true;
            }
            if (backgroundHorizontalLayoutGroup != null) {
               backgroundHorizontalLayoutGroup.childForceExpandWidth = false;
            }
            if (tooltipHorizontalLayoutGroup != null) {
               tooltipHorizontalLayoutGroup.childForceExpandWidth = false;
            }

            // Remove this tooltip from the list of currently open tooltips
            UIToolTipManager.openTooltips.Remove(toolTipPanel);
         }
      }
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
      if (toolTipType != ToolTipComponent.Type.ItemCellInventory) {
         tooltipHorizontalLayoutGroup.childControlWidth = false;
         tooltipHorizontalLayoutGroup.childForceExpandWidth = true;
         if (backgroundHorizontalLayoutGroup != null) {
            backgroundHorizontalLayoutGroup.childForceExpandWidth = true;
         }
      }
      toolTipPanel.transform.position = new Vector3(leftEdge - _tooltipDimensions.x / 2, middleOfEdge, 0);
   }

   public void placeOnRightSideOfPanel (float rightEdge, float middleOfEdge, Vector2 _tooltipDimensions) {
      if (toolTipType != ToolTipComponent.Type.ItemCellInventory) {
         tooltipHorizontalLayoutGroup.childControlWidth = false;
         tooltipHorizontalLayoutGroup.childForceExpandWidth = true;
         if (backgroundHorizontalLayoutGroup != null) {
            backgroundHorizontalLayoutGroup.childForceExpandWidth = true;
         }
      }
      toolTipPanel.transform.position = new Vector3(rightEdge + _tooltipDimensions.x / 2, middleOfEdge, 0);
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
