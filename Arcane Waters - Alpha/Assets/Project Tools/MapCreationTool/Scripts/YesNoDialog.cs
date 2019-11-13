using System;
using UnityEngine;
using UnityEngine.UI;

namespace MapCreationTool
{
    public class YesNoDialog : MonoBehaviour
    {
        private CanvasGroup cGroup;

        private Action onYes;
        private Action onNo;

        [SerializeField]
        private Text titleText = null;
        [SerializeField]
        private Text contentText = null;

        private void Awake()
        {
            cGroup = GetComponent<CanvasGroup>();
            Hide();
        }

        public void Display(string title, string content, Action onYes, Action onNo)
        {
            this.onYes = onYes;
            this.onNo = onNo;

            titleText.text = title;
            contentText.text = content;

            Show();
        }

        public void YesButton_Click()
        {
            onYes?.Invoke();
            Hide();
        }

        public void NoButton_Click()
        {
            onNo?.Invoke();
            Hide();
        }

        private void Hide()
        {
            cGroup.alpha = 0;
            cGroup.blocksRaycasts = false;
            cGroup.interactable = false;
        }

        private void Show()
        {
            cGroup.alpha = 1;
            cGroup.blocksRaycasts = true;
            cGroup.interactable = true;
        }
    }
}