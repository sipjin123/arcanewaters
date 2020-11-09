using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonRepeater : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{

   #region Public Variables

   // The initial delay before the pressed button starts to repeat its action
   public float initialRepeatInterval = 0.4f;

   // The repeat interval
   public float repeatInterval = 0.1f;

   #endregion

   void Awake () {
      _button = GetComponent<Button>();
      if (_button == null) {
         enabled = false;
      }
   }

   void Update () {
      if (_isHeld) {
         if (Time.time - _lastPointerDownTime > initialRepeatInterval &&
             Time.time - _lastRepeatTime > repeatInterval) {
            _button.onClick.Invoke();
            _lastRepeatTime = Time.time;
         }
      }
   }

   public void OnPointerDown (PointerEventData eventData) {
      _lastPointerDownTime = Time.time;
      _lastRepeatTime = Time.time;
      _isHeld = true;
   }

   public void OnPointerUp (PointerEventData eventData) {
      _isHeld = false;
   }

   #region Private Variables

   // The button
   private Button _button;

   // Gets set to true when the button is being held down
   private bool _isHeld = false;

   // The last time the button was pressed
   private float _lastPointerDownTime;

   // The last time the button action was repeated
   private float _lastRepeatTime;

   #endregion
}
