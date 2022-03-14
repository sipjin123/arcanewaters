using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_Icon : ClientMonoBehaviour
{
   #region Public Variables

   // The target object that we represent
   public GameObject target;

   // The tooltip we want for this icon
   public Tooltipped tooltip;

   [Tooltip("Should we round the icon's position to 4ths?")]
   public bool roundPositionTo4ths = false;

   [Tooltip("Is icon anchored to the background? It needs to account for player position otherwise")]
   public bool iconAnchoredToTheBackground = false;

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
            if (roundPositionTo4ths) {
               Util.setLocalXY(this.transform, roundTo4ths(Minimap.self.getCorrectedPosition(target.transform, currentArea, !iconAnchoredToTheBackground)));
            } else {
               Util.setLocalXY(this.transform, Minimap.self.getCorrectedPosition(target.transform, currentArea, !iconAnchoredToTheBackground));
            }
         }
      }
   }

   private Vector2 roundTo4ths (Vector2 position) {
      return new Vector2(
         Mathf.Round(position.x * 0.25f) * 4f,
         Mathf.Round(position.y * 0.25f) * 4f
         );
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

   public void onHoverEnter () {
      if (tooltip != null && tooltip.text.Length > 0) {
         Minimap.self.tooltipText.text = tooltip.text;
         Minimap.self.toolTipContainer.SetActive(true);
      }
   }

   public void onHoverExit () {
      Minimap.self.toolTipContainer.SetActive(false);
   }

   #region Private Variables

   // Our Image
   protected Image _image;

   #endregion
}
