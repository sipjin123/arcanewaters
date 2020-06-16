using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_SeaMonsterIcon : ClientMonoBehaviour {
   #region Public Variables

   // Associated monster entity
   public SeaMonsterEntity seaMonster;

   // Area in which sea monster exists
   public Area currentArea;

   #endregion

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
   }

   private void Update () {
      if (seaMonster == null || seaMonster.isDead()) {
         gameObject.SetActive(false);
         return;
      }

      // Prepare data for position calculations
      Vector2 mapPos = Minimap.self.backgroundImage.rectTransform.localPosition;
      Vector2 minimapSize = Minimap.self.backgroundImage.rectTransform.sizeDelta;
      Vector2 minimapMaskSize = Minimap.self.backgroundImage.GetComponentInParent<Mask>().rectTransform.sizeDelta;

      // Get object position relative to area
      Vector2 relativePosition = seaMonster.transform.position - currentArea.transform.position;

      // Move it to bottom-left corner (because area position is centered)
      relativePosition -= currentArea.getAreaHalfSize();

      // Calculate relative position in [0, 1] range
      relativePosition /= currentArea.getAreaSize();

      // Map [0, 1] to minimap
      relativePosition *= minimapSize;

      // Adjust based on minimap translation (map is focused on player icon)
      relativePosition += mapPos;
      relativePosition += minimapMaskSize;

      Util.setLocalXY(this.transform, relativePosition);
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
