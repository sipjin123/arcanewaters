using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class FloatingLoadIcon : MonoBehaviour
{
   #region Public Variables

   // How long this should live
   public float lifetime = 10f;

   // Main text component of this canvas
   public TextMeshProUGUI text;

   #endregion

   private void Start () {
      _startTime = Time.time;

      // Make sure we show up in front
      Util.setZ(this.transform, -2f);

      // Start animating the text
      text.SetText(".");
      InvokeRepeating(nameof(animateText), 0f, .4f);

      // Always destroy after a time limit
      Destroy(this.gameObject, lifetime);
   }

   private void Update () {
      // Destroy the loading icon as soon as the loading finishes
      if (!PanelManager.self.isLoading) {
         Destroy(this.gameObject);
      }
   }

   protected void animateText () {
      if (text.text.Length <= 1) {
         text.SetText("..");
      } else if (text.text.Length <= 2) {
         text.SetText("...");
      } else {
         text.SetText(".");
      }
   }

   public static FloatingLoadIcon instantiateAt (Vector2 position) {
      FloatingLoadIcon icon = Instantiate(PrefabsManager.self.floatingLoadIconPrefab, position, Quaternion.identity);
      return icon;
   }

   #region Private Variables

   // The time at which we were created
   protected float _startTime;

   #endregion
}
