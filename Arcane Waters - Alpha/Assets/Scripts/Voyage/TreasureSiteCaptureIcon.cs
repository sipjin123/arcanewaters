using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TreasureSiteCaptureIcon : MonoBehaviour
{
   #region Public Variables

   // The icon renderer
   public SpriteRenderer iconRenderer;

   // The aura renderer
   public SpriteRenderer auraRenderer;

   // The image used when the site is being captured by our voyage group
   public Sprite allyImage;

   // The aura image used when the site is being captured by our voyage group
   public Sprite allyAuraImage;

   // The image used when the site is being captured by an enemy group
   public Sprite enemyImage;

   // The aura image used when the site is being captured by an enemy group
   public Sprite enemyAuraImage;

   #endregion

   public void Awake () {
      _animator = GetComponent<Animator>();
      _treasureSite = GetComponentInParent<TreasureSite>();

      // Hide the icon by default
      iconRenderer.gameObject.SetActive(false);
   }

   public void Update () {
      // Only active on clients
      if (_treasureSite == null || !_treasureSite.isClient || Global.player == null ||
         !VoyageManager.isInVoyage(Global.player)) {
         return;
      }

      // Set the correct icon
      if (_treasureSite.inRangeVoyageGroupId == Global.player.voyageGroupId) {
         iconRenderer.sprite = allyImage;
         auraRenderer.sprite = allyAuraImage;
      } else {
         iconRenderer.sprite = enemyImage;
         auraRenderer.sprite = enemyAuraImage;
      }

      // Show or hide the icon
      switch (_treasureSite.status) {
         case TreasureSite.Status.Capturing:
         case TreasureSite.Status.Stealing:
            // Show the capture icon
            if (!iconRenderer.gameObject.activeSelf) {
               iconRenderer.gameObject.SetActive(true);
               _animator.SetBool("visible", true);
            }
            break;
         case TreasureSite.Status.Idle:
         case TreasureSite.Status.Captured:
         case TreasureSite.Status.Blocked:
         case TreasureSite.Status.Resetting:
         default:
            // Hide the capture icon
            if (iconRenderer.gameObject.activeSelf) {
               _animator.SetBool("visible", false);
               iconRenderer.gameObject.SetActive(false);
            }
            break;
      }
   }

   #region Private Variables

   // The associated treasure site
   public TreasureSite _treasureSite;

   // The animator component
   private Animator _animator;

   #endregion
}
