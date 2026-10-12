using System;
using UnityEngine;
using UnityEngine.UI;

namespace VN
{
    public class VNConfirmDialog : MonoBehaviour
    {
        public Text messageText;
        public Button yesButton;
        public Button noButton;

        public bool IsOpen => gameObject.activeSelf;

        Action onYes, onNo;

        void Awake()
        {
            yesButton.onClick.AddListener(() => { var a = onYes; Close(); a?.Invoke(); });
            noButton.onClick.AddListener(() => { var a = onNo; Close(); a?.Invoke(); });
        }

        public void Show(string message, Action yes, Action no = null)
        {
            messageText.text = message;
            onYes = yes;
            onNo = no;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Close()
        {
            onYes = onNo = null;
            gameObject.SetActive(false);
        }
    }
}
