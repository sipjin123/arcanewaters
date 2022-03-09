using UnityEngine;
using UnityEngine.UI;

public class GifProgressBox : ClientMonoBehaviour
{
   #region Public Variables

   #endregion

   protected override void Awake () {
      base.Awake();
      if (!enabled) {
         return;
      }

      // Cache componenets
      _rectTransform = GetComponent<RectTransform>();
      _layoutElement = GetComponent<LayoutElement>();

      // Hide component fully
      _rectTransform.sizeDelta = new Vector2(0, _rectTransform.sizeDelta.y);
      _layoutElement.preferredWidth = 0;
   }

   private void Update () {
      if (GIFReplayManager.self.isEncoding()) {
         _layoutElement.preferredHeight = Mathf.MoveTowards(_layoutElement.preferredHeight, _fullHeight, Time.deltaTime * _fullHeight);
         if (_layoutElement.preferredHeight == _fullHeight) {
            _rectTransform.sizeDelta = new Vector2(Mathf.MoveTowards(_rectTransform.sizeDelta.x, _fullWidth, Time.deltaTime * _fullWidth), _rectTransform.sizeDelta.y);
         }
      } else {
         _rectTransform.sizeDelta = new Vector2(Mathf.MoveTowards(_rectTransform.sizeDelta.x, 0, Time.deltaTime * _fullWidth), _rectTransform.sizeDelta.y);
         if (_rectTransform.sizeDelta.x == 0) {
            _layoutElement.preferredHeight = Mathf.MoveTowards(_layoutElement.preferredHeight, 0, Time.deltaTime * _fullHeight);
         }
      }

      // Update progress message
      if (GIFReplayManager.self.currentProgress == null) {
         _progressText.text = "";
      } else if (GIFReplayManager.self.currentProgress.error) {
         _progressText.text = "ERROR";
      } else {
         _progressText.text = Mathf.RoundToInt(GIFReplayManager.self.currentProgress.progress * 100f) + "%";
      }
   }

   #region Private Variables

   // UI components
   private RectTransform _rectTransform;
   private LayoutElement _layoutElement;
   [SerializeField] private Text _progressText;

   // Width of the element when fully visible
   [SerializeField] private float _fullWidth = 0;

   // height of the element when fully visible
   [SerializeField] private float _fullHeight = 0;

   #endregion
}
