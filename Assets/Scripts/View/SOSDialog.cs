using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;

public class SOSAdDialog : MonoBehaviour
{
    public static SOSAdDialog Instance { get; private set; }

    private SudokuViewModel _vm;
    private bool _isSOSRunning;

    void Awake()
    {
        Instance = this;
        _isSOSRunning = false;
    }

    public void Bind(SudokuViewModel vm)
    {
        if (ReferenceEquals(_vm, vm)) return;
        if (_vm != null) _vm.IsSOSMode.OnChanged -= Show;

        _vm = vm;

        if (_vm != null) _vm.IsSOSMode.OnChanged += Show;
    }
    private void OnDestroy()
    {
        if (_vm != null)
            _vm.IsSOSMode.OnChanged -= Show;

        if (Instance == this)
            Instance = null;
    }
    public void Show(bool arg)
    {
        if (_vm == null || _isSOSRunning) return;

        _isSOSRunning = true;

        try{
            StartAd();
        }
        catch(Exception ex)
        {
            _isSOSRunning = false;
            Debug.Log($"SOS Pressed: {ex.Message}");
        }
    }

    private void StartAd()
    {
        if (AdManager.Instance == null || !AdManager.Instance.IsAdReady())
        {
            _vm.ApplySOSCommand.Execute();
            MakeChangesProvidedBySOS(_vm);
            _isSOSRunning = false;
            return;
        }

        AdManager.Instance.PlayAd(
            onCompleted: OnAdCompleted,
            onFailed:    OnAdCompleted
        );
    }
    
    private void OnAdCompleted()
    {
        if (_vm != null)
        {
            _vm.ApplySOSCommand.Execute();
            MakeChangesProvidedBySOS(_vm);
            _isSOSRunning = false;
        }
    }

    public async void MakeChangesProvidedBySOS(SudokuViewModel vm)
    {
        vm.SetDemoMode();
        vm.HideHUD.Value = false;
        var arglist = vm.SOSChangedCells.Value;
        try
        {
            foreach(var entry in arglist)
            {
                if(_vm.BoardValues.Value[entry.row, entry.col] != 0)
                {
                    _vm.SelectedRow.Value = entry.row;
                    _vm.SelectedCol.Value = entry.col;
                    _vm.EnterValueCommand.Execute(0);
                    Debug.Log($"SOS: Deleting row {entry.row} and column {entry.col} entry");
                    await Task.Delay(1000);
                }
                _vm.SelectedRow.Value = entry.row;
                _vm.SelectedCol.Value = entry.col;
                _vm.EnterValueCommand.Execute(entry.number);
                Debug.Log($"SOS: Entering row {entry.row} and column {entry.col} entry to {entry.number}");
                await Task.Delay(1000);
            }
        }
        finally
        {
            vm.ResetDemoMode();
            vm.HideHUD.Value = true;
            vm.SOSChangedCells.Value.Clear();
        }
    }

}