using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class FPSPanel : MonoBehaviour {
   #region Public Variables

   // The Text that shows our FPS
   public Text text;

   #endregion

   void Start () {
      // We only update the text every now and then, otherwise it's kind of overwhelming
      InvokeRepeating("updateText", 1f, 1f);
   }

   void Update () {
      // Calculate what our FPS would be if all of our frames took this long.
      float estimatedFPS = 1f / Time.deltaTime;

      // Keep a record of this
      _pastEstimates.Add(estimatedFPS);
   }

   protected void updateText () {
      // Find the lowest estimated FPS
      float minFPS = _pastEstimates.Min();

      // Update the text
      text.text = "FPS: " + minFPS;

      // Clear out the list
      _pastEstimates.Clear();
   }

   #region Private Variables

   // Keeps track of our estimated FPS
   protected List<float> _pastEstimates = new List<float>();

   #endregion
}
