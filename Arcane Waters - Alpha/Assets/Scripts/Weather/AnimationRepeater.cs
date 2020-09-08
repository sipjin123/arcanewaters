using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationRepeater : MonoBehaviour
{
   // The animator component
   public Animator animator;

   // Timer variables
   public float loopDelayMin = 4, loopDelayMax = 10;

   // Cooldown timer
   public float cooldownTimer;

   void Start () {
      // Animator setup
      animator = GetComponent<Animator>();
      animator.Update(Random.Range(0f, 1f));

      // The interval between the lightninge effect
      cooldownTimer = Random.Range(loopDelayMin, loopDelayMax);

      StartCoroutine(CO_PlayThunderAnimation());
   }

   private IEnumerator CO_PlayThunderAnimation () {
      animator.Play(animator.GetCurrentAnimatorClipInfo(0)[0].clip.name, 0, 0);
      yield return new WaitForSeconds(cooldownTimer);
      initCooldown();
   }

   private void initCooldown () {
      cooldownTimer = Random.Range(loopDelayMin, loopDelayMax);
      StartCoroutine(CO_PlayThunderAnimation());
   }
}