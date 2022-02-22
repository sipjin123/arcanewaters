using UnityEngine;
using UnityEngine.UI;

public class IndeterminateProgressBar : MonoBehaviour
{
   #region Public Variables

   // Reference to the image
   public Image image;

   // The time that the progress bar takes to fill
   public float fillTimeSeconds = 10;

   // Is the animation looping?
   public bool isLooping = false;

   #endregion

   public void toggle(bool show) {
      gameObject.SetActive(show);
   }

   public bool isShowing () {
      return gameObject.activeInHierarchy;
   }

   public bool isPlaying () {
      return _isPlaying;
   }

   public void play () {
      InvokeRepeating(nameof(onStep), 0, 1.0f);
      _isPlaying = true;
      _startTime = Time.realtimeSinceStartup;
   }

   public void stop () {
      CancelInvoke(nameof(onStep));
      _isPlaying = false;
   }

   private void onStep () {
      float ratio = (Time.realtimeSinceStartup - _startTime) / fillTimeSeconds;
      ratio = Mathf.Min(1.0f, Mathf.Max(0.0f, ratio));
      image.fillAmount = _isBarFillingUp ? ratio : 1 - ratio;

      if (ratio <= 0.0f || ratio >= 1.0f) {
         _startTime = Time.realtimeSinceStartup;

         if (!isLooping) {
            stop();
         } else {
            _isBarFillingUp = !_isBarFillingUp;
         }
      }

      _value = ratio;
   }

   public float getValue () {
      return _value;
   }

   #region Private Variables

   // The starting time
   private float _startTime;

   // The current value
   private float _value;

   // Is the bar filling up or down?
   private bool _isBarFillingUp = true;

   // Is the progress bar playing?
   private bool _isPlaying = false;

   #endregion
}
