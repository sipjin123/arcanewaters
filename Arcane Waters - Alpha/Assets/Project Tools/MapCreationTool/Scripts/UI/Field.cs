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

      [SerializeField]
      private Toggle valueToggle = null;

      public Dropdown valueDropdown { get; private set; }

      private RectTransform rectT;
      private UIToolTip toolTip;
      private SelectOption[] options = new SelectOption[0];

      public string toolTipMessage
      {
         set { toolTip.message = value; }
      }

      private void Awake () {
         fieldName = transform.Find("name").GetComponent<Text>();
         valueInput = GetComponentInChildren<InputField>();
         valueDropdown = GetComponentInChildren<Dropdown>();

         rectT = GetComponent<RectTransform>();
         toolTip = GetComponent<UIToolTip>();
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

      public void setFieldProperties (PrefabDataDefinition.DataFieldType type) {
         if (type == PrefabDataDefinition.DataFieldType.Int) {
            valueInput.contentType = InputField.ContentType.IntegerNumber;
            valueInput.placeholder.GetComponent<Text>().text = "Enter integer...";
            rectT.sizeDelta = new Vector2(290, rectT.sizeDelta.y);
         } else if (type == PrefabDataDefinition.DataFieldType.Float) {
            valueInput.contentType = InputField.ContentType.DecimalNumber;
            valueInput.placeholder.GetComponent<Text>().text = "Enter float...";
            rectT.sizeDelta = new Vector2(290, rectT.sizeDelta.y);
         } else if (type == PrefabDataDefinition.DataFieldType.String) {
            valueInput.contentType = InputField.ContentType.Standard;
            valueInput.placeholder.GetComponent<Text>().text = "Enter text...";
            rectT.sizeDelta = new Vector2(450, rectT.sizeDelta.y);
         } else if (type == PrefabDataDefinition.DataFieldType.Bool) {
            valueInput.gameObject.SetActive(false);
            valueToggle.gameObject.SetActive(true);
            rectT.sizeDelta = new Vector2(180, rectT.sizeDelta.y);
         }
      }

      public void setFieldProperties (SelectOption[] options) {
         this.options = options;

         valueDropdown.options.Clear();
         valueDropdown.options.AddRange(options.Select(o => new Dropdown.OptionData { text = o.displayText }));
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
            for (int i = 0; i < options.Length; i++)
               if (options[i].value.CompareTo(value) == 0)
                  index = i;
            valueDropdown.SetValueWithoutNotify(index);
         }
      }

      public string value
      {
         get
         {
            if (valueInput != null && valueInput.gameObject.activeSelf) {
               return valueInput.text;
            }

            if (valueToggle != null && valueToggle.gameObject.activeSelf) {
               return valueToggle.isOn.ToString();
            }

            if (valueDropdown != null) {
               return valueDropdown.value >= 0 ? options[valueDropdown.value].value : "";
            }

            return "";
         }
      }

      private void inputValueChanged (string value) {
         ValueChanged?.Invoke(value);
      }

      private void dropdownValueChanged (int value) {
         ValueChanged?.Invoke(options[value].value);
      }

      private void toggleValueChanged (bool value) {
         ValueChanged?.Invoke(value.ToString());
      }
   }
}
