using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FixedGUIScaler : MonoBehaviour {
   #region Public Variables

   #endregion

   public void Start () {
      // This component tries to counteract the GUI scale of the parent canvas, so that the UI control with this component preserves its visual size
      _rectTransform = GetComponent<RectTransform>();
      _referenceScale = _rectTransform.localScale;
   }

   public void Update () {
      _rectTransform.localScale = _referenceScale / OptionsManager.self.mainGameCanvas.scaleFactor;
   }

   public void setReferenceScale(Vector3 newReferenceScale) {
      _referenceScale = newReferenceScale;
   }

   #region Private Variables

   // Reference to the managed RectTransform instance
   private RectTransform _rectTransform;

   // Reference scale
   private Vector3 _referenceScale;

   #endregion
}
