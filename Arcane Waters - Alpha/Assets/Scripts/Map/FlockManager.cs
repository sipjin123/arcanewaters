using UnityEngine;
using System.Collections.Generic;

public class FlockManager : ClientMonoBehaviour
{
   #region Public Variables

   // The prefab we use for creating Flocks
   public List<Flock> flockPrefabs;

   // The area we spawn in
   public BoxCollider2D spawnBox;

   // The sprite we use while coasting
   public Sprite birdCoastSprite;

   // The sprite we use while coasting
   public Sprite shadowCoastSprite;

   // Maximum number of flocks per second
   public int maxFlocks;

   // What part of 'maxFlocks' should be the average amount of flocks per second
   public float flockChance;

   #endregion

   void Start () {
      // Routinely create flocks of birds
      InvokeRepeating("flockCreationUpdate", 0f, 1f);
   }

   protected void flockCreationUpdate () {
      for (int i = 0; i < maxFlocks; i++) {
         if (Random.value > flockChance) {
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

         // Make sure the spawn point is outside of the collider bounds
         Vector3 localPost = flock.transform.localPosition;
         flock.transform.localPosition += localPost.x < 0 ? new Vector3(localPost.x - 5, localPost.y, localPost.z) : new Vector3(localPost.x + 5, localPost.y, localPost.z);
         flock.flockManager = this;
      }
   }

   #region Private Variables

   #endregion
}
