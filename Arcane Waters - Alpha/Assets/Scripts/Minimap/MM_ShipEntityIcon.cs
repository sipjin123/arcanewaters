using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_ShipEntityIcon : MonoBehaviour {
   #region Public Variables

   // Associated ship entity
   public ShipEntity shipEntity;

   // Area in which ship should be (otherwise hide)
   public Area currentArea;

   // The icon that will be displayed when this ship icon is out of mini map bounds
   public RectTransform outBoundIcon;

   // Prefab to spawn when this icon is out of bounds
   public GameObject outBoundIconPrefab;

   // The current rect transform
   public RectTransform currentRectTransform;

   #endregion

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
      currentRectTransform = GetComponent<RectTransform>();
   }

   private void Update () {
      setCorrectPosition();
   }

   public void setCorrectPosition () {
      if (shipEntity == null || shipEntity.isDead() || shipEntity.areaKey != currentArea.areaKey || shipEntity.areaKey != Global.player?.areaKey) {
         Destroy(this.gameObject);
         return;
      }

      // Set correct ship entity icon position in minimap
      currentRectTransform.anchoredPosition = Minimap.self.getCorrectedPosition(shipEntity.transform, currentArea);

      // If the icon is out of bounds, generate a sub icon pointing towards this icon
      if (Mathf.Abs(currentRectTransform.anchoredPosition.x) > Minimap.MAP_CLAMP_VAL || Mathf.Abs(currentRectTransform.anchoredPosition.y) > Minimap.MAP_CLAMP_VAL) {
         if (outBoundIcon == null) {
            outBoundIcon = Instantiate(outBoundIconPrefab, transform.parent).GetComponent<RectTransform>();
         }  
         outBoundIcon.gameObject.SetActive(true);

         float horizontalClamp = Mathf.Clamp(currentRectTransform.anchoredPosition.x, -Minimap.MAP_CLAMP_VAL, Minimap.MAP_CLAMP_VAL);
         float verticalClamp = Mathf.Clamp(currentRectTransform.anchoredPosition.y, -Minimap.MAP_CLAMP_VAL, Minimap.MAP_CLAMP_VAL);
         Vector2 clampedPosition = new Vector2(horizontalClamp, verticalClamp);
         outBoundIcon.anchoredPosition = clampedPosition;

         Vector3 dir = outBoundIcon.anchoredPosition - Minimap.self.playerIcon.GetComponent<RectTransform>().anchoredPosition;
         outBoundIcon.rotation = Quaternion.Slerp(outBoundIcon.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 100);
      } else {
         if (outBoundIcon != null) {
            Destroy(outBoundIcon.gameObject);
         }
      }
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
