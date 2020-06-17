using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_LandMonsterIcon : MonoBehaviour {
   #region Public Variables

   // Associated monster entity
   public Enemy enemy;

   // Area in which monster exists
   public Area currentArea;

   // Sprite for special enemy type - boss
   public Sprite bossSprite;

   #endregion

   protected void Start () {
      // Lookup components
      _image = GetComponent<Image>();
   }

   public void setBossSprite () {
      if (!_image) {
         _image = GetComponent<Image>();
      }
      _image.sprite = bossSprite;
   }

   private void Update () {
      if (enemy == null || enemy.isDead()) {
         gameObject.SetActive(false);
         return;
      }

      // Set correct sea monster icon position in minimap
      Util.setLocalXY(this.transform, Minimap.self.getCorrectedPosition(enemy.transform, currentArea));
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
