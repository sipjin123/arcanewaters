using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;

public class PowerupIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // A reference to the icon for this powerup
   public Image iconImage;

   // A reference to the border image for this powerup
   public Image borderImage;

   // A reference to the tooltip for this powerup
   public RectTransform toolTip;

   // A reference to the rect transform containing the content for the tooltip
   public RectTransform toolTipContent;

   // Text fields for the name and description of this powerup
   public TextMeshProUGUI nameText, descriptionText;

   // The rarity of the powerup that this icon represents
   [HideInInspector]
   public Rarity.Type rarity;

   // The type of this powerup that this icon represents
   [HideInInspector]
   public Powerup.Type type;

   // Cached border sprites
   public List<Sprite> borderSprites = new List<Sprite>();

   #endregion

   public void init(Powerup.Type type, Rarity.Type rarity) {
      if (type == Powerup.Type.None || rarity == Rarity.Type.None) {
         D.error("Tried to initialise powerup icon with no type / rarity");
         return;
      }

      if (borderSprites.Count < 1) {
         borderSprites = Resources.LoadAll<Sprite>(LandPowerupData.BORDER_SPRITES_LOCATION).ToList();
      }
      Sprite[] iconSprites = Resources.LoadAll<Sprite>(Powerup.ICON_SPRITES_LOCATION);

      iconImage.sprite = iconSprites[(int)type - 1];
      borderImage.sprite = borderSprites[(int)rarity - 1];

      PowerupData data = PowerupManager.self.getPowerupData(type);
      nameText.text = data.powerupName;
      descriptionText.text = data.description;
      nameText.faceColor = PowerupPanel.self.rarityColors[(int) rarity];

      this.type = type;
      this.rarity = rarity;

      _parentCanvasTransform = PowerupPanel.self.parentCanvas.GetComponent<RectTransform>();
   }

   public void initializeItemPowerup (Item itemData) {
      string itemName = EquipmentXMLManager.self.getItemName(itemData);
      string itemDescription = EquipmentXMLManager.self.getItemDescription(itemData);
      string itemIcon = EquipmentXMLManager.self.getItemIconPath(itemData);
      Rarity.Type itemRarity = itemData.getRarity();

      if (borderSprites.Count < 1) {
         borderSprites = Resources.LoadAll<Sprite>(LandPowerupData.BORDER_SPRITES_LOCATION).ToList();
      }

      iconImage.sprite = ImageManager.getSprite(itemIcon);
      borderImage.sprite = borderSprites[(int) itemRarity - 1];

      nameText.text = itemName;
      descriptionText.text = itemDescription;
      nameText.faceColor = PowerupPanel.self.rarityColors[(int) rarity];
      this.rarity = itemRarity;
      //this.landPowerupType = type;

      _parentCanvasTransform = PowerupPanel.self.parentCanvas.GetComponent<RectTransform>();
   }


   public void OnPointerEnter (PointerEventData eventData) {
      toolTip.gameObject.SetActive(true);
   }

   public void OnPointerExit (PointerEventData eventData) {
      toolTip.gameObject.SetActive(false);
      toolTip.anchoredPosition = new Vector2(0.0f, toolTip.anchoredPosition.y);
   }

   private void Update () {
      keepTooltipOnScreen();
   }

   private void keepTooltipOnScreen () {
      Rect screenRect = Util.rectTransformToScreenSpace(toolTipContent);

      float distanceToRightEdge = Screen.width - screenRect.xMax;
      if (distanceToRightEdge < 0.0f) {
         toolTip.anchoredPosition += Vector2.right * distanceToRightEdge;
      }

      float distanceToLeftEdge = screenRect.xMin;
      if (distanceToLeftEdge < 0.0f) {
         toolTip.anchoredPosition += Vector2.right * -distanceToLeftEdge;
      }
   }

   #region Private Variables

   // A reference to the rect transform of the canvas that holds this powerup icon
   private RectTransform _parentCanvasTransform;

   #endregion
}
