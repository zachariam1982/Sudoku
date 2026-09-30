using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorMessage : MonoBehaviour
{
    private static ErrorMessage Instance;
    private BaseViewModel viewModel;
    [Header("Error Dialog")]
    [SerializeField] private GameObject errorDialog;

    [Header("Error dialog fields")]
    [SerializeField] private TextMeshProUGUI Title;
    [SerializeField] private TextMeshProUGUI Message;
    [SerializeField] private TextMeshProUGUI Status;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    private Action<(string title, string message, string status)> _showMessageHandler;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Awake()
    {
        if(Instance == null) Instance = this;

        Title.text = "";
        Message.text = "";
        Status.text = "";

        closeButton.onClick.AddListener(() => errorDialog.SetActive(false)); 
    }

    public void Bind(BaseViewModel arg)
    {
        if (ReferenceEquals(viewModel, arg)) return;
        if (viewModel != null && _showMessageHandler != null)
            viewModel.ShowMessage.OnChanged -= _showMessageHandler;

        viewModel = arg;
        _showMessageHandler = value => {
                Title.text = value.title;
                Message.text = value.message;
                Status.text = value.status;
                errorDialog.SetActive(true);
            };
        arg.ShowMessage.OnChanged += _showMessageHandler;
    }

    private void OnDestroy()
    {
        if (viewModel != null && _showMessageHandler != null)
            viewModel.ShowMessage.OnChanged -= _showMessageHandler;
    }
}
