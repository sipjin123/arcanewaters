using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FlockManager : ClientMonoBehaviour {
   #region Public Variables

   // The prefab we use for creating Flocks
   public List<Flock> flockPrefabs;

   // The area we spawn in
   public BoxCollider2D spawnBox;

   // The sprite we use while coasting
   public Sprite birdCoastSprite;

   // The sprite we use while coasting
   public Sprite shadowCoastSprite;

   #endregion

   void Start () {
      _area = GetComponentInParent<Area>();

      // Routinely create flocks of birds
      InvokeRepeating("maybeCreateFlock", 0f, 1f);
   }

   protected void maybeCreateFlock () {
      // Sometimes we won't do anything
      if (Random.Range(0f, 1f) < .75f) {
         return;
      }

      // Pick a spawn pos
      Vector3 spawnPos = Util.RandomPointInBounds(spawnBox.bounds);

      // Make the target on the other side
      Vector2 targetPos = spawnPos + new Vector3(15f, 0f);

      // Create the instance
      Flock flock = Instantiate(flockPrefabs.ChooseRandom(), spawnPos, Quaternion.identity);
      flock.transform.SetParent(this.transform, true);
      flock.targetPos = targetPos;
      flock.flockManager = this;
   }

   #region Private Variables

   // The Area we're in
   protected Area _area;

   #endregion
}
