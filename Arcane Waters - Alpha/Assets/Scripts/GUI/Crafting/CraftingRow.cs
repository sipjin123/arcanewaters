using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CraftingRow : MonoBehaviour {

    private Item item;
    public Item Item { get { return item; } }

    private bool isOccupied;
    [SerializeField]
    private Button button;
    public Button Button { get { return button; } }

    [SerializeField]
    private Image icon, selectionIcon;

    [SerializeField]
    private Text nameText;

    private bool hasData;
    public bool HasData { get { return hasData; } }

    [SerializeField]
    private Sprite emptySprite;

    public void InjectItem(Item itemvar)
    {
        hasData = true;
        item = itemvar;
        nameText.text = item.getName();
        icon.sprite = ImageManager.getSprite(item.getIconPath());
    }
    public void PurgeData()
    {
        hasData = false;
        item = null;
        nameText.text = "";
        icon.sprite = emptySprite;
    }

    public void SelectItem()
    {
        selectionIcon.enabled = true;
    }

    public void UnselectItem()
    {
        selectionIcon.enabled = false; 
    }
   #region Public Variables
      
   #endregion

   #region Private Variables
      
   #endregion

}
