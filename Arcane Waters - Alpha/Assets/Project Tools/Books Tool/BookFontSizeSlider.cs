using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;

public class BookFontSizeSlider : MonoBehaviour {
   #region Public Variables

   public event System.Action<int> onApply;

   #endregion

   private void Start () {
      _slider.onValueChanged.AddListener((x) => {
         _sizeLabel.text = Mathf.RoundToInt(x) + "%";
      });

      _applyButton.onClick.AddListener(() => {
         onApply?.Invoke(getValue());
      });
   }

   public int getValue () {
      return Mathf.RoundToInt(_slider.value);
   }

   #region Private Variables

   // The slider
   [SerializeField]
   private Slider _slider = default;

   // The label showing the percent
   [SerializeField]
   private Text _sizeLabel = default;

   // The apply button
   [SerializeField]
   private Button _applyButton = default;

   #endregion
}
