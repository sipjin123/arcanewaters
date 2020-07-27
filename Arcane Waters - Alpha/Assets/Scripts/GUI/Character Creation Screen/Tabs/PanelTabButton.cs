using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

[RequireComponent(typeof(Button))]
public class PanelTabButton : MonoBehaviour {
   #region Public Variables

   // Is this the currently highlighted tab?
   public bool isSelected = false;

   // The image we show when this tag is highlighted
   public Image selectedTabImage;

   // The content we show when this tab is clicked
   public GameObject tabContent;

   // The controller this tab belongs to
   public TabbedPanelController tabbedPanel;

   #endregion

   private void Awake () {
      _button = GetComponent<Button>();
      _tabBorderImage = GetComponent<Image>();
      _styleGrids = tabContent.GetComponentsInChildren<CharacterStyleGrid>(true).ToList();
   }

   private void Start () {
      _button.onClick.AddListener(onTabClicked);
   }

   private void onTabClicked () {
      if (isSelected) {
         return;
      }

      // Unselect all the tabs
      tabbedPanel.unselectAllTabs();

      // Show this tab as selected and enable the content
      setSelected();
   }

   public void setSelected () {
      selectedTabImage.gameObject.SetActive(true);
      _tabBorderImage.enabled = false;
      isSelected = true;
      tabContent.gameObject.SetActive(true);

      foreach (CharacterStyleGrid grid in _styleGrids) {
         grid.show();
      }
   }

   public void setUnselected () {
      selectedTabImage.gameObject.SetActive(false);
      _tabBorderImage.enabled = true;
      isSelected = false;
      tabContent.gameObject.SetActive(false);
   }

   #region Private Variables

   // The button component
   private Button _button;

   // The image of the unselected tab, behind the panel
   private Image _tabBorderImage;

   // The style grids that belong to this section
   private List<CharacterStyleGrid> _styleGrids;

   #endregion
}
