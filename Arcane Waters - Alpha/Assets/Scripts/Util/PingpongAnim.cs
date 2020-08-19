using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PingpongAnim : MonoBehaviour {
   #region Public Variables

   // Sprite component and asset reference
   public SpriteRenderer spriteRender;
   public List<Sprite> spriteFrames;

   // Index parameters
   public int spriteIndex, maxSpriteIndex;

   // Animation parameters
   public bool isReverse;
   public float animationSpeed = .15f;
   public float reverseSpeed = .25f;

   #endregion

   private void Awake () {
      spriteRender = GetComponent<SpriteRenderer>();
      maxSpriteIndex = spriteFrames.Count;
      InvokeRepeating("animate", animationSpeed, animationSpeed);
   }

   private void animate () {
      if (isReverse) {
         spriteRender.sprite = spriteFrames[spriteIndex];
         try {
         } catch {
            D.editorLog("Cant: " + spriteIndex, Color.blue);
         }
         if (spriteIndex < 1) {
            isReverse = false; 
            initPlay();
         } else {
            spriteIndex--;
         }
      } else {
         spriteRender.sprite = spriteFrames[spriteIndex];
         try {
         } catch {
            D.editorLog("Cant: " + spriteIndex, Color.green);
         }
         if (spriteIndex >= maxSpriteIndex - 1) {
            isReverse = true;
            initReverse();
         } else {
            spriteIndex++;
         }
      }
   }

   private void initPlay () {
      CancelInvoke();
      InvokeRepeating("animate", animationSpeed, animationSpeed);
   }

   private void initReverse () {
      CancelInvoke();
      InvokeRepeating("animate", reverseSpeed, reverseSpeed);
   }

   #region Private Variables

   #endregion
}
