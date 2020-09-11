using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ItemDefinitionTool
{
   public class EquipmentSpecificAttributes : TypeSpecificAttributes
   {
      #region Public Variables

      // The target class type of this attribute section
      public override Type targetType => typeof(EquipmentDefinition);

      // Various attribute inputs and controls
      public InputField priceInput;
      public Toggle canBeTrashedToggle;
      public Toggle setAllColorsToggle;
      public InputField intelligenceInput;
      public InputField strengthInput;
      public InputField spiritInput;
      public InputField vitalityInput;
      public InputField precisionInput;
      public InputField luckInput;

      // Elemental modifiers section
      public Text elementalTitleLabel;
      public Button elementalAddButton;
      public GameObject elementalEntryTemplate;
      public Transform elementalEntryParent;

      // Rarity modifiers section
      public Text rarityTitleLabel;
      public Button rarityAddButton;
      public GameObject rarityEntryTemplate;
      public Transform rarityEntryParent;

      #endregion

      private void Awake () {
         elementalAddButton.onClick.AddListener(addElementalEntry);
         rarityAddButton.onClick.AddListener(addRarityEntry);
      }

      private void Update () {
         elementalTitleLabel.text = $"Elemental Modifiers ({ _elementalEntries.Count })";
         rarityTitleLabel.text = $"Rarity Modifiers ({ _rarityEntries.Count })";
      }

      public override void applyAttributeValues (ItemDefinition target) {
         EquipmentDefinition eq = target as EquipmentDefinition;

         eq.price = int.Parse(priceInput.text);
         eq.canBeTrashed = canBeTrashedToggle.isOn;
         eq.setAllColors = setAllColorsToggle.isOn;
         eq.statsData.intelligence = int.Parse(intelligenceInput.text);
         eq.statsData.strength = int.Parse(strengthInput.text);
         eq.statsData.spirit = int.Parse(spiritInput.text);
         eq.statsData.vitality = int.Parse(vitalityInput.text);
         eq.statsData.precision = int.Parse(precisionInput.text);
         eq.statsData.luck = int.Parse(luckInput.text);

         eq.elementModifiers = new ElementModifier[_elementalEntries.Count];
         eq.rarityModifiers = new RarityModifier[_rarityEntries.Count];

         for (int i = 0; i < eq.elementModifiers.Length; i++) {
            eq.elementModifiers[i] = getElementalValues(_elementalEntries[i]);
         }
         for (int i = 0; i < eq.rarityModifiers.Length; i++) {
            eq.rarityModifiers[i] = getRarityValues(_rarityEntries[i]);
         }
      }

      public override void setValuesWithoutNotify (ItemDefinition itemDefinition) {
         EquipmentDefinition eq = itemDefinition as EquipmentDefinition;

         priceInput.SetTextWithoutNotify(eq.price.ToString());
         canBeTrashedToggle.SetIsOnWithoutNotify(eq.canBeTrashed);
         setAllColorsToggle.SetIsOnWithoutNotify(eq.setAllColors);
         intelligenceInput.SetTextWithoutNotify(eq.statsData.intelligence.ToString());
         strengthInput.SetTextWithoutNotify(eq.statsData.strength.ToString());
         spiritInput.SetTextWithoutNotify(eq.statsData.spirit.ToString());
         vitalityInput.SetTextWithoutNotify(eq.statsData.vitality.ToString());
         precisionInput.SetTextWithoutNotify(eq.statsData.precision.ToString());
         luckInput.SetTextWithoutNotify(eq.statsData.luck.ToString());

         // Remove if too many entries
         while (eq.elementModifiers.Length < _elementalEntries.Count) {
            removeElementalEntry(_elementalEntries.Last());
         }
         while (eq.rarityModifiers.Length < _rarityEntries.Count) {
            removeRarityEntry(_rarityEntries.Last());
         }

         // Added if not enough entries
         while (eq.elementModifiers.Length > _elementalEntries.Count) {
            addElementalEntry();
         }
         while (eq.rarityModifiers.Length > _rarityEntries.Count) {
            addRarityEntry();
         }

         // Set entry values
         for (int i = 0; i < eq.elementModifiers.Length; i++) {
            setElementalValues(_elementalEntries[i], eq.elementModifiers[i]);
         }
         for (int i = 0; i < eq.rarityModifiers.Length; i++) {
            setRarityValues(_rarityEntries[i], eq.rarityModifiers[i]);
         }
      }

      public void addElementalEntry () {
         GameObject entry = Instantiate(elementalEntryTemplate, elementalEntryParent);
         entry.SetActive(true);
         _elementalEntries.Add(entry);

         // Set dropdown options
         Dropdown drop = entry.GetComponentInChildren<Dropdown>();
         drop.options.Clear();
         foreach (Element element in Enum.GetValues(typeof(Element))) {
            drop.options.Add(new Dropdown.OptionData { text = element.ToString() });
         }

         // Add delete button hook
         entry.GetComponentInChildren<Button>().onClick.AddListener(() => removeElementalEntry(entry));

         setElementalValues(entry, new ElementModifier { elementType = Element.Physical, multiplier = 0 });
      }

      public void addRarityEntry () {
         GameObject entry = Instantiate(rarityEntryTemplate, rarityEntryParent);
         entry.SetActive(true);
         _rarityEntries.Add(entry);

         // Set dropdown options
         Dropdown drop = entry.GetComponentInChildren<Dropdown>();
         drop.options.Clear();
         foreach (Rarity.Type rarity in Enum.GetValues(typeof(Rarity.Type))) {
            drop.options.Add(new Dropdown.OptionData { text = rarity.ToString() });
         }

         // Add delete button hook
         entry.GetComponentInChildren<Button>().onClick.AddListener(() => removeRarityEntry(entry));

         setRarityValues(entry, new RarityModifier { rarityType = Rarity.Type.None, multiplier = 0 });
      }

      private void setElementalValues (GameObject target, ElementModifier values) {
         Dropdown drop = target.GetComponentInChildren<Dropdown>();
         drop.SetValueWithoutNotify(drop.options.FindIndex(c => c.text.Equals(values.elementType.ToString())));

         target.GetComponentInChildren<InputField>().SetTextWithoutNotify(values.multiplier.ToString());
      }

      private void setRarityValues (GameObject target, RarityModifier values) {
         Dropdown drop = target.GetComponentInChildren<Dropdown>();
         drop.SetValueWithoutNotify(drop.options.FindIndex(c => c.text.Equals(values.rarityType.ToString())));

         target.GetComponentInChildren<InputField>().SetTextWithoutNotify(values.multiplier.ToString());
      }

      private ElementModifier getElementalValues (GameObject target) {
         Dropdown drop = target.GetComponentInChildren<Dropdown>();
         return new ElementModifier {
            elementType = (Element) Enum.Parse(typeof(Element), drop.options[drop.value].text),
            multiplier = int.Parse(target.GetComponentInChildren<InputField>().text)
         };
      }

      private RarityModifier getRarityValues (GameObject target) {
         Dropdown drop = target.GetComponentInChildren<Dropdown>();
         return new RarityModifier {
            rarityType = (Rarity.Type) Enum.Parse(typeof(Rarity.Type), drop.options[drop.value].text),
            multiplier = int.Parse(target.GetComponentInChildren<InputField>().text)
         };
      }

      public void removeElementalEntry (GameObject entry) {
         if (_elementalEntries.Contains(entry)) {
            _elementalEntries.Remove(entry);
            Destroy(entry.gameObject);
         }
      }

      public void removeRarityEntry (GameObject entry) {
         if (_rarityEntries.Contains(entry)) {
            _rarityEntries.Remove(entry);
            Destroy(entry.gameObject);
         }
      }

      #region Private Variables

      // Elemental modifier entries
      private List<GameObject> _elementalEntries = new List<GameObject>();

      // Rarity modifier entries
      private List<GameObject> _rarityEntries = new List<GameObject>();

      #endregion
   }
}