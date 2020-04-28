using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class MapSign : ClientMonoBehaviour, IMapEditorDataReceiver {
   #region Public Variables

   // The starting index of the sign icon in the sprite sheet Assets/Sprites/Map/signs
   public static int MAP_SIGN_START_INDEX = 7;

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
   public Text signLabel;

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
      Sprite[] mapSignSprites = ImageManager.getSprites(mapSignIcon.sprite.texture);
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.MAP_SIGN_TYPE_KEY:
               int mapTypeIndex = int.Parse(field.v);
               mapTypeIndex = Mathf.Clamp(mapTypeIndex, 0, MapSign.MAP_SIGN_START_INDEX);
               mapSignPost.sprite = mapSignSprites[mapTypeIndex];
               break;
            case DataField.MAP_ICON_KEY:
               int mapIconIndex = int.Parse(field.v);
               mapIconIndex = Mathf.Clamp(mapIconIndex, 0, (mapSignSprites.Length - 1) - MapSign.MAP_SIGN_START_INDEX);
               mapSignIcon.sprite = mapSignSprites[MapSign.MAP_SIGN_START_INDEX + mapIconIndex];
               break;
            case DataField.MAP_SIGN_LABEL:
               signLabel.text = field.v;
               break;
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }
   }

   #region Private Variables

   // Our various components
   protected SpriteOutline _outline;
   protected ClickableBox _clickableBox;

   #endregion
}
