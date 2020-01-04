using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class Field : MonoBehaviour
   {
      public event System.Action<string> ValueChanged;
      public Text fieldName { get; private set; }
      public InputField valueInput { get; private set; }
      public Toggle valueToggle { get; private set; }
      public Dropdown valueDropdown { get; private set; }

      private RectTransform rectT;

      private void Awake () {
         fieldName = GetComponentInChildren<Text>();
         valueInput = GetComponentInChildren<InputField>();
         valueDropdown = GetComponentInChildren<Dropdown>();
         valueToggle = GetComponentInChildren<Toggle>(true);

         rectT = GetComponent<RectTransform>();
      }

      private void OnEnable () {
         if (valueInput != null)
            valueInput.onValueChanged.AddListener(inputValueChanged);
         if (valueDropdown != null)
            valueDropdown.onValueChanged.AddListener(dropdownValueChanged);
         if (valueToggle != null)
            valueToggle.onValueChanged.AddListener(toggleValueChanged);
      }

      private void OnDisable () {
         if (valueInput != null)
            valueInput.onValueChanged.RemoveListener(inputValueChanged);
         if (valueDropdown != null)
            valueDropdown.onValueChanged.RemoveListener(dropdownValueChanged);
      }

      public void setFieldProperties (DataFieldType type) {
         if (type == DataFieldType.Int) {
            valueInput.contentType = InputField.ContentType.IntegerNumber;
            valueInput.placeholder.GetComponent<Text>().text = "Enter integer...";
            rectT.sizeDelta = new Vector2(290, rectT.sizeDelta.y);
         } else if (type == DataFieldType.Float) {
            valueInput.contentType = InputField.ContentType.DecimalNumber;
            valueInput.placeholder.GetComponent<Text>().text = "Enter float...";
            rectT.sizeDelta = new Vector2(290, rectT.sizeDelta.y);
         } else if (type == DataFieldType.String) {
            valueInput.contentType = InputField.ContentType.Standard;
            valueInput.placeholder.GetComponent<Text>().text = "Enter text...";
            rectT.sizeDelta = new Vector2(450, rectT.sizeDelta.y);
         } else if (type == DataFieldType.Bool) {
            valueInput.gameObject.SetActive(false);
            valueToggle.gameObject.SetActive(true);
            rectT.sizeDelta = new Vector2(180, rectT.sizeDelta.y);
         }
      }

      public void setFieldProperties (string[] options) {
         valueDropdown.options.Clear();
         valueDropdown.options.AddRange(options.Select(o => new Dropdown.OptionData { text = o }));
      }

      public void setValue (string value) {
         if (valueInput != null && valueInput.gameObject.activeSelf) {
            valueInput.SetTextWithoutNotify(value);
         }
         if (valueToggle != null && valueToggle.gameObject.activeSelf) {
            valueToggle.SetIsOnWithoutNotify(bool.Parse(value));
         }
         if (valueDropdown != null) {
            int index = -1;
            for (int i = 0; i < valueDropdown.options.Count; i++)
               if (valueDropdown.options[i].text.CompareTo(value) == 0)
                  index = i;
            valueDropdown.SetValueWithoutNotify(index);
         }
      }

      private void inputValueChanged (string value) {
         ValueChanged?.Invoke(value);
      }

      private void dropdownValueChanged (int value) {
         ValueChanged?.Invoke(valueDropdown.options[value].text);
      }

      private void toggleValueChanged(bool value) {
         ValueChanged?.Invoke(value.ToString());
      }
   }
}
