using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_Icon : ClientMonoBehaviour {
   #region Public Variables

   // The target object that we represent
   public GameObject target;

   // The tooltip we want for this icon
   public Tooltipped tooltip;

   #endregion

   private void Start () {
      _image = GetComponent<Image>();
      if (_image == null) {
         gameObject.SetActive(false);
      }
   }

   private void Update () {
      if (target == null) {
         gameObject.SetActive(false);
         return;
      }

      // Hide the icon if necessary
      Util.setAlpha(_image, shouldShowIcon() ? 1f : 0f);

      // Keep the icon in the right position
      if (Global.player != null) {
         Area currentArea = AreaManager.self.getArea(Global.player.areaKey);
         if (currentArea != null) {
            // Keep the icon in the right position
            Util.setLocalXY(this.transform, Minimap.self.getCorrectedPosition(target.transform, currentArea));
         }
      }
   }

   public virtual bool shouldShowIcon () {
      // Children classes can override this functionality
      return true;
   }

   public string getTooltip () {
      return tooltip.text;
   }

   public Image getImage () {
      if (_image) {
         return _image;
      }
      return _image = GetComponent<Image>();
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
