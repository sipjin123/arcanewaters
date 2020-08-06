using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using TMPro;

public class MapSign : ClientMonoBehaviour, IMapEditorDataReceiver {
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

   // If this prefab is in map editor mode
   public bool isEditorMode = false;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      _outline = GetComponentInChildren<SpriteOutline>();
      _clickableBox = GetComponentInChildren<ClickableBox>();
   }

   private void Start () {
      _outline.Regenerate();
      _outline.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
   }

   public void Update () {
      // Figure out whether our outline should be showing
      handleSpriteOutline();
   }

   public void handleSpriteOutline () {
      if (_outline == null || _clickableBox == null || isEditorMode) {
         return;
      }

      // Only show our outline when the mouse is over us
      bool isHovering = MouseManager.self.isHoveringOver(_clickableBox);
      _outline.setVisibility(isHovering);
      directionLabelUI.SetActive(isHovering);
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

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}
