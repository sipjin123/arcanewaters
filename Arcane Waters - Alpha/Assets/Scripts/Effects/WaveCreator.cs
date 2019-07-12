using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WaveCreator : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating our waves
   public TrailingWave wavePrefab;

   #endregion

   void Start () {
      _body = GetComponent<Rigidbody2D>();
      _seaEntity = GetComponent<SeaEntity>();

      // Continually make waves
      InvokeRepeating("maybeMakeTrailingWaves", 0f, .01f);
   }

   protected void maybeMakeTrailingWaves () {
      // Don't do anything if we aren't moving fast enough
      if (_body.velocity.magnitude < .2f) {
         return;
      }

      // Create the trailing waves
      for (int i = -1; i < 2; i+=2) {
         TrailingWave trailingWave = Instantiate(wavePrefab);
         trailingWave.transform.position = this.transform.position;
         trailingWave.transform.rotation = Quaternion.Euler(0, 0, -Util.getAngle(_seaEntity.facing));
         trailingWave.velocity = new Vector2(i, 0f);
         Util.setZ(trailingWave.transform, 95f);
      }
   }

   #region Private Variables

   // Our rigidbody
   protected Rigidbody2D _body;

   // Our sea entity
   protected SeaEntity _seaEntity;

   #endregion
}
