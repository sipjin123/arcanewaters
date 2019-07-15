using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftingMaterialRow : MonoBehaviour {
    #region Public Variables

    #endregion

    #region Private Variables

    #endregion

    [SerializeField]
    private Image icon;
    [SerializeField]
    private Text itemName;

    [SerializeField]
    private Item itemData;
    public Item ItemData { get { return itemData; } }

    [SerializeField]
    private Image selectionIndicator;
    [SerializeField]
    private Button button;
    public Button Button { get { return button; } }
    public void InitData(Item item)
    {
        itemData = item;
        itemName.text = item.getName();
        icon.sprite = ImageManager.getSprite(item.getIconPath());
    }
    public void SelectItem()
    {
        selectionIndicator.enabled = true;
    }
    public void DeselectItem()
    {
        selectionIndicator.enabled = false;
    }
}
