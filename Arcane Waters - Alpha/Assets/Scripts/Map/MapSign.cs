using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using TMPro;
using UnityEngine.EventSystems;

public class MapSign : ClientMonoBehaviour, IMapEditorDataReceiver, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // Type of sign this object is
   public int mapSignType;

   // The type of icon to show in the sign
   public int mapIconType;

   // The sprite of the sign object
   public SpriteRenderer mapSignPost;

   // The sprite of the sign icon
   public SpriteRenderer mapSignIcon;

   // The object containing the direction label
   public GameObject directionLabelUI;

   // The content of the sign
   public TextMeshProUGUI signLabel;

   // The rect transform of the tooltip canvas
   public RectTransform toolTipRectTransform;

   // If this prefab is in map editor mode
   public bool isEditorMode = false;

   // Distance limit to determine if player is close enough to display tooltip
   public float distanceToPlayer = 0.3f;

   // Is mouse pointer over the sign
   public bool pointerIsHovering;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Start () {
      _outline.Regenerate();
      _outline.setVisibility(false);
   }

   private void Update () {
      if (Global.player == null) {
         return;
      }

      if ((Vector2.Distance(this.transform.position, Global.player.transform.position) < distanceToPlayer) || pointerIsHovering) {
         directionLabelUI.SetActive(true);
         _outline.setVisibility(true);
      } 
      else {
         directionLabelUI.SetActive(false);
         _outline.setVisibility(false);
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.MAP_SIGN_TYPE_KEY:
               setPostType(field.intValue);
               break;
            case DataField.MAP_ICON_KEY:
               setIconType(field.intValue);
               break;
            case DataField.MAP_SIGN_LABEL:
               signLabel.SetText(field.v);
               break;
         }
      }
   }

   public void setPostType (int type) {
      Sprite[] postSprites = ImageManager.getSprites(mapSignPost.sprite.texture);
      int mapTypeIndex = Mathf.Clamp(type, 0, postSprites.Length - 1);
      mapSignPost.sprite = postSprites[mapTypeIndex];
   }

   public void setIconType (int type) {
      Sprite[] iconSprites = ImageManager.getSprites(mapSignIcon.sprite.texture);
      int mapIconIndex = Mathf.Clamp(type, 0, iconSprites.Length - 1);
      mapSignIcon.sprite = iconSprites[mapIconIndex];
   }

   public void OnPointerEnter (PointerEventData eventData) {
      directionLabelUI.SetActive(true);
      _outline.setVisibility(true);
      pointerIsHovering = true;

      // Check if tooltip repositioning is needed
      TooltipManager.self.keepToolTipOnScreen(toolTipRectTransform);
   }

   public void OnPointerExit (PointerEventData eventData) {
      directionLabelUI.SetActive(false);
      _outline.setVisibility(false);
      pointerIsHovering = false;

      // Reset position of tooltip to default
      toolTipRectTransform.anchoredPosition = Vector2.zero;
   }

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}
