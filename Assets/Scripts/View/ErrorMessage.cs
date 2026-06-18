using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorMessage : MonoBehaviour
{
    private static ErrorMessage Instance;
    private SudokuViewModel viewModel;
    [Header("Error Dialog")]
    [SerializeField] private GameObject errorDialog;

    [Header("Error dialog fields")]
    [SerializeField] private TextMeshProUGUI Title;
    [SerializeField] private TextMeshProUGUI Message;
    [SerializeField] private TextMeshProUGUI Status;

    [Header("Close Button")]
    [SerializeField] private Button closeButton;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Awake()
    {
        if(Instance == null) Instance = this;

        Title.text = "";
        Message.text = "";
        Status.text = "";

        closeButton.onClick.AddListener(() => errorDialog.SetActive(false)); 
    }

    public void Bind(SudokuViewModel arg)
    {
        viewModel = arg;
        arg.ShowMessage.OnChanged += (arg) => {
                Title.text = arg.title;
                Message.text = arg.message;
                Status.text = arg.status;
                errorDialog.SetActive(true);
            };
    }
}
