using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MiningTrigger : MonoBehaviour
{
   #region Public Variables

   // Reference to the animator component of the player body
   public Animator animator;

   // Reference to the player body
   public BodyEntity bodyEntity;

   // The reference to the collider
   public Collider2D arcCollider;

   // The main reference to the coroutine
   public IEnumerator processingCoroutine;

   // The direction where the ore spawn will be moving towards
   public Direction oreSpawnEffectDirection;

   // The particle sprite fading speed
   public static float fadeSpeed = 1.5f;

   // Animator Key for direction
   public const string FACING_KEY = "facing";

   #endregion

   public void interactOres () {
      // Force stop existing coroutine
      if (processingCoroutine != null) {
         StopCoroutine(processingCoroutine);
      }

      // Make sure the player is not moving
      if (bodyEntity.getVelocity().magnitude < .07f) {
         processingCoroutine = CO_ProcessInteraction();
         StartCoroutine(processingCoroutine);
      }
   }

   private IEnumerator CO_ProcessInteraction () {
      yield return new WaitForSeconds(.1f);

      Weapon.ActionType currentActionType = bodyEntity.weaponManager.actionType;
      AnimatorClipInfo[] m_AnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(0);
      Collider2D currentCollider = arcCollider;
      List<int> oreIdsInteracted = new List<int>();
      if (m_AnimatorClipInfo[0].clip.name.ToLower().Contains("interact")) {
         // Handle the angle of the cone shaped collider
         float currentAngle = 0;
         oreSpawnEffectDirection = Direction.North;
         if (bodyEntity.facing == Direction.East || bodyEntity.facing == Direction.SouthEast || bodyEntity.facing == Direction.NorthEast) {
            currentAngle = -90;
            oreSpawnEffectDirection = Direction.East;
         } else if (bodyEntity.facing == Direction.West || bodyEntity.facing == Direction.SouthWest || bodyEntity.facing == Direction.NorthWest) {
            currentAngle = 90;
            oreSpawnEffectDirection = Direction.West;
         } else if (bodyEntity.facing == Direction.South) {
            currentAngle = 180;
            oreSpawnEffectDirection = Direction.South;
         }
         transform.localEulerAngles = new Vector3(0, 0, currentAngle);

         // Interact with ore overlapping the cone collider
         RaycastHit2D[] rayHits = new RaycastHit2D[10];
         int hitNum = currentCollider.Cast(new Vector2(0, 0), rayHits);
         foreach (RaycastHit2D hit in rayHits) {
            if (hit.collider != null && hit.collider.GetComponent<OreNode>() != null) {
               OreNode oreNode = hit.collider.GetComponent<OreNode>();
               if (!oreNode.hasBeenMined() && !oreIdsInteracted.Exists(_=>_ == oreNode.id) && !oreNode.finishedMining()) {
                  oreNode.tryToMineNodeOnClient();
                  oreIdsInteracted.Add(oreNode.id);

                  if (oreNode.finishedMining()) {
                     ExplosionManager.createMiningParticle(hit.collider.transform.position);

                     GameObject oreBounce = Instantiate(PrefabsManager.self.oreDropPrefab);
                     OreMineEffect oreMine = oreBounce.GetComponent<OreMineEffect>();
                     oreBounce.transform.position = hit.collider.transform.position;

                     if (oreSpawnEffectDirection == Direction.East) {
                        oreBounce.transform.localScale = new Vector3(-1, 1, 1);
                     } else if (oreSpawnEffectDirection == Direction.North || oreSpawnEffectDirection == Direction.South) {
                        oreMine.animator.SetFloat(FACING_KEY, (float) oreSpawnEffectDirection);
                     }
                     oreMine.animator.speed = Random.Range(.8f, 1.2f);
                     oreMine.oreNode = oreNode;
                  }
               }
            }
         }
      }

      #region Private Variables

      #endregion
   }
}