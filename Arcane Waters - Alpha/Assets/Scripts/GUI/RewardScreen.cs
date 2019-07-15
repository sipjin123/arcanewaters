using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class RewardScreen : MonoBehaviour {
    public CanvasGroup canvasGroup;

    // Our various components that we need references to
    public Text text;
    public Button confirmButton;
    public Image imageIcon;

    public void Show(Item item)
    {
        D.print("I wana show this ite");
        imageIcon.sprite = ImageManager.getSprite(item.getIconPath());
        show();
    }

    public void show()
    {
        D.print("Enable canvas group and game object");
        this.canvasGroup.alpha = 1f;
        this.canvasGroup.blocksRaycasts = true;
        this.canvasGroup.interactable = true;
        this.gameObject.SetActive(true);
    }

    public void hide()
    {
        this.canvasGroup.alpha = 0f;
        this.canvasGroup.blocksRaycasts = false;
        this.canvasGroup.interactable = false;
    }

    public void disableButtons()
    {
        canvasGroup.interactable = false;
    }
}
