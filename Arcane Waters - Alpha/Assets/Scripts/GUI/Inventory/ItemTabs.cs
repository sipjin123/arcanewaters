using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections.Generic;
using System;

public class ItemTabs : MonoBehaviour
{
   #region Public Variables

   // The tab renderers
   public GameObject allTab;
   public GameObject weaponTab;
   public GameObject armorTab;
   public GameObject ingredientsTab;
   public GameObject othersTab;

   // The tab buttons
   public Button allTabUnderButton;
   public Button weaponTabUnderButton;
   public Button armorTabUnderButton;
   public Button ingredientsTabUnderButton;
   public Button othersTabUnderButton;

   // The category filters for the selected tab
   [HideInInspector]
   public List<Item.Category> categoryFilters = new List<Item.Category> { Item.Category.None };

   #endregion

   public void initialize (UnityAction onTabButtonPressedAction) {
      _onTabButtonPressedAction = onTabButtonPressedAction;

      // Create a hashset with all the item categories
      HashSet<Item.Category> workSet = new HashSet<Item.Category>();
      foreach (Item.Category c in Enum.GetValues(typeof(Item.Category))) {
         workSet.Add(c);
      }

      // Remove the categories managed by the other tabs
      workSet.Remove(Item.Category.None);
      workSet.Remove(Item.Category.Weapon);
      workSet.Remove(Item.Category.Armor);
      workSet.Remove(Item.Category.CraftingIngredients);

      // Hats are displayed in the Armor tab
      workSet.Remove(Item.Category.Hats);

      // Save the categories in a list
      _othersTabCategories = new List<Item.Category>();
      foreach (Item.Category c in workSet) {
         _othersTabCategories.Add(c);
      }

      // Set the tab button click events
      allTabUnderButton.onClick.RemoveAllListeners();
      weaponTabUnderButton.onClick.RemoveAllListeners();
      armorTabUnderButton.onClick.RemoveAllListeners();
      ingredientsTabUnderButton.onClick.RemoveAllListeners();
      othersTabUnderButton.onClick.RemoveAllListeners();

      allTabUnderButton.onClick.AddListener(onAllTabButtonPress);
      weaponTabUnderButton.onClick.AddListener(onWeaponTabButtonPress);
      armorTabUnderButton.onClick.AddListener(onArmorTabButtonPress);
      ingredientsTabUnderButton.onClick.AddListener(onIngredientsTabButtonPress);
      othersTabUnderButton.onClick.AddListener(onOthersTabButtonPress);

      // Select the 'all' tab by default
      updateCategoryTabs(Item.Category.None);
   }

   
   public void onAllTabButtonPress () {
      categoryFilters.Clear();
      categoryFilters.Add(Item.Category.None);
      updateCategoryTabs(Item.Category.None);
      _onTabButtonPressedAction.Invoke();
   }

   public void onWeaponTabButtonPress () {
      categoryFilters.Clear();
      categoryFilters.Add(Item.Category.Weapon);
      updateCategoryTabs(Item.Category.Weapon);
      _onTabButtonPressedAction.Invoke();
   }

   public void onArmorTabButtonPress () {
      categoryFilters.Clear();
      categoryFilters.Add(Item.Category.Armor);
      categoryFilters.Add(Item.Category.Hats);
      updateCategoryTabs(Item.Category.Armor);
      _onTabButtonPressedAction.Invoke();
   }

   public void onIngredientsTabButtonPress () {
      categoryFilters.Clear();
      categoryFilters.Add(Item.Category.CraftingIngredients);
      updateCategoryTabs(Item.Category.CraftingIngredients);
      _onTabButtonPressedAction.Invoke();
   }

   public void onOthersTabButtonPress () {
      categoryFilters.Clear();
      foreach (Item.Category c in _othersTabCategories) {
         categoryFilters.Add(c);
      }
      updateCategoryTabs(Item.Category.Potion);
      _onTabButtonPressedAction.Invoke();
   }

   public void updateCategoryTabs (Item.Category itemCategory) {
      switch (itemCategory) {
         case Item.Category.None:
            allTab.SetActive(true);
            weaponTab.SetActive(false);
            armorTab.SetActive(false);
            ingredientsTab.SetActive(false);
            othersTab.SetActive(false);

            allTabUnderButton.gameObject.SetActive(false);
            weaponTabUnderButton.gameObject.SetActive(true);
            armorTabUnderButton.gameObject.SetActive(true);
            ingredientsTabUnderButton.gameObject.SetActive(true);
            othersTabUnderButton.gameObject.SetActive(true);
            break;
         case Item.Category.Weapon:
            allTab.SetActive(false);
            weaponTab.SetActive(true);
            armorTab.SetActive(false);
            ingredientsTab.SetActive(false);
            othersTab.SetActive(false);

            allTabUnderButton.gameObject.SetActive(true);
            weaponTabUnderButton.gameObject.SetActive(false);
            armorTabUnderButton.gameObject.SetActive(true);
            ingredientsTabUnderButton.gameObject.SetActive(true);
            othersTabUnderButton.gameObject.SetActive(true);
            break;
         case Item.Category.Armor:
            allTab.SetActive(false);
            weaponTab.SetActive(false);
            armorTab.SetActive(true);
            ingredientsTab.SetActive(false);
            othersTab.SetActive(false);

            allTabUnderButton.gameObject.SetActive(true);
            weaponTabUnderButton.gameObject.SetActive(true);
            armorTabUnderButton.gameObject.SetActive(false);
            ingredientsTabUnderButton.gameObject.SetActive(true);
            othersTabUnderButton.gameObject.SetActive(true);
            break;
         case Item.Category.CraftingIngredients:
            allTab.SetActive(false);
            weaponTab.SetActive(false);
            armorTab.SetActive(false);
            ingredientsTab.SetActive(true);
            othersTab.SetActive(false);

            allTabUnderButton.gameObject.SetActive(true);
            weaponTabUnderButton.gameObject.SetActive(true);
            armorTabUnderButton.gameObject.SetActive(true);
            ingredientsTabUnderButton.gameObject.SetActive(false);
            othersTabUnderButton.gameObject.SetActive(true);
            break;
         default:
            // "Others" tab
            allTab.SetActive(false);
            weaponTab.SetActive(false);
            armorTab.SetActive(false);
            ingredientsTab.SetActive(false);
            othersTab.SetActive(true);

            allTabUnderButton.gameObject.SetActive(true);
            weaponTabUnderButton.gameObject.SetActive(true);
            armorTabUnderButton.gameObject.SetActive(true);
            ingredientsTabUnderButton.gameObject.SetActive(true);
            othersTabUnderButton.gameObject.SetActive(false);
            break;
      }
   }

   #region Private Variables

   // The action to invoke when a tab is clicked
   private UnityAction _onTabButtonPressedAction;

   // The list of categories listed by the 'others' tab
   private List<Item.Category> _othersTabCategories;

   #endregion
}
