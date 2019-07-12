using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaBird : ClientMonoBehaviour {
   #region Public Variables

   // The bird sprite
   public SpriteRenderer birdSprite;

   // The shadow sprite
   public SpriteRenderer shadowSprite;

   // The height we should fly at
   public float height;

   // Our associated flock
   public Flock flock;

   #endregion

   void Start () {
      _simpleAnimations = new List<SimpleAnimation>(GetComponentsInChildren<SimpleAnimation>());

      // Set the shadow height according to our assigned fly height
      Util.setLocalY(shadowSprite.transform, height);

      // Occasionally stop flapping wings and coast
      InvokeRepeating("maybeStopFlapping", 0f, 1f);
   }

   protected void maybeStopFlapping () {
      // If we're already coasting, don't change anything
      if (_isCoasting) {
         return;
      }

      // Only do it occasionally
      if (Random.Range(0f, 1f) > .75f) {
         StartCoroutine(CO_CoastWings());
      }
   }

   protected IEnumerator CO_CoastWings () {
      birdSprite.sprite = flock.flockManager.birdCoastSprite;
      shadowSprite.sprite = flock.flockManager.shadowCoastSprite;

      foreach (SimpleAnimation anim in _simpleAnimations) {
         anim.isPaused = true;
      }

      _isCoasting = true;
      yield return new WaitForSeconds(Random.Range(2f, 4f));
      _isCoasting = false;

      foreach (SimpleAnimation anim in _simpleAnimations) {
         anim.isPaused = false;
      }
   }

   #region Private Variables

   // Our Simple Animation
   protected List<SimpleAnimation> _simpleAnimations;

   // Gets set to true while we're coasting
   protected bool _isCoasting = false;

   #endregion
}
