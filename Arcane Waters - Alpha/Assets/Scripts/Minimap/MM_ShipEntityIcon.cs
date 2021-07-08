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

      if (currentRectTransform == null) {
         return;
      }

      // Set correct ship entity icon position in minimap
      currentRectTransform.anchoredPosition = Minimap.self.getCorrectedPosition(shipEntity.transform, currentArea);
   }

   public void onHoverBegin () {
      if (shipEntity != null) {
         Minimap.self.displayIconInfo(shipEntity.entityName);
      }
   }

   public void onHoverEnd () {
      Minimap.self.disableIconInfo();
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}