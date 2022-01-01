using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Flock : ClientMonoBehaviour {
   #region Public Variables

   // The prefab we use for creating individual birds
   public SeaBird birdPrefab;

   // The target position we're moving towards
   public Vector2 targetPos;

   // Our associated Flock Manager
   public FlockManager flockManager;

   #endregion

   private void Start () {
      _body = GetComponent<Rigidbody2D>();
      _birds = new List<SeaBird>(GetComponentsInChildren<SeaBird>());

      // Randomize the speed
      float speed = Random.Range(1.5f, 2.5f);

      // Randomize the height
      float randomHeight = Random.Range(-.1f, -.3f);

      foreach (SeaBird bird in _birds) {
         bird.height = randomHeight;
         bird.flock = this;

         // Flip the sprites if the target is towards opposite direction
         bird.birdSprite.flipX = transform.position.x > targetPos.x;
         bird.shadowSprite.flipX = transform.position.x > targetPos.x;
      }

      // Move towards the target
      _body.velocity = (targetPos - (Vector2) this.transform.position).normalized / speed;
   }

   private void Update () {
      // If we're close enough to the target, we're done
      if (Vector2.Distance(this.transform.position, targetPos) < .1f) {
         Destroy(this.gameObject);
      }
   }

   #region Private Variables

   // Our Rigid body
   protected Rigidbody2D _body;

   // The Birds in this flock
   protected List<SeaBird> _birds;

   #endregion
}
