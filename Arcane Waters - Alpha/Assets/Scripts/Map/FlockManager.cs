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
         Vector3 spawnOffset = new Vector3(Random.Range(-minRandomVal, -maxRandomVal), 0f, 0f);
         Vector3 spawnPos = Util.RandomPointInBounds(spawnBox.bounds);
         spawnPos = new Vector3(spawnBox.bounds.min.x, spawnPos.y, spawnPos.z) - spawnOffset;

         // Make the target on the other side
         Vector3 targetOffset = new Vector3(Random.Range(minRandomVal, maxRandomVal), 0f, 0f);
         Vector2 targetPos = new Vector3(spawnBox.bounds.max.x, spawnPos.y, spawnPos.z) + targetOffset;

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

   // Random value parameters
   private float minRandomVal = .5f; 
   private float maxRandomVal = 1.5f;

   #endregion
}
