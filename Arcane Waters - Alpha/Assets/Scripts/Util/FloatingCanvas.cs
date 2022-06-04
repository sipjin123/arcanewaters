using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class FloatingCanvas : MonoBehaviour
{
   #region Public Variables

   // How long this should live
   public float lifetime = 1.75f;

   // How fast this should float up
   public float riseSpeed = .0025f;

   // Main text component of this canvas
   public TextMeshProUGUI text;

   // Simple value used to embed custom data
   public string customTag;

   #endregion

   void Start () {
      _startTime = Time.time;

      // Our Canvas Group
      _canvasGroup = GetComponentInChildren<CanvasGroup>();

      // Make sure we show up in front
      Util.setZ(this.transform, -2f);

      // Start floating upwards
      InvokeRepeating("floatUp", 0f, .02f);

      // Destroy after a couple seconds
      Destroy(this.gameObject, lifetime);
   }

   protected void floatUp () {
      float timeAlive = Time.time - _startTime;

      // Slowly move upwards
      Vector3 currentPos = this.transform.position;
      currentPos.y += riseSpeed;
      this.transform.position = currentPos;

      // Also fade out
      _canvasGroup.alpha = 1f - (timeAlive / lifetime);
   }

   public static FloatingCanvas instantiateAt (Vector2 position) {
      return Instantiate(PrefabsManager.self.floatingCanvasPrefab, position, Quaternion.identity);
   }

   public FloatingCanvas asTooFar () {
      if (text != null) {
         text.text = "Too Far...";
      }

      return this;
   }

   public FloatingCanvas asTooClose () {
      if (text != null) {
         text.text = "Too Close...";
      }

      return this;
   }

   public FloatingCanvas asInvalidLoot () {
      if (text != null) {
         text.text = "Cannot Loot this...";
      }

      return this;
   }

   public FloatingCanvas asNoResponse () {
      if (text != null) {
         text.text = "No Response...";
      }

      return this;
   }
   
   public FloatingCanvas asPowerupIgnored () {
      if (text != null) {
         text.text = "Weaker powerup is ignored!";
      }

      return this;
   }

   public FloatingCanvas asEnemiesAround () {
      if (text != null) {
         text.text = "There are enemies around...";
      }

      return this;
   }

   public FloatingCanvas asPickedUpItem (Item castItem) {
      if (text != null && castItem != null) {
         text.text = $"+{ castItem.count } { castItem.getName() }";
      }

      return this;
   }

   public FloatingCanvas asCustomMessage (string customMessage) {
      if (text != null) {
         text.text = customMessage;
      }

      return this;
   }

   #region Private Variables

   // The time at which we were created
   protected float _startTime;

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   #endregion
}
