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
        if (!arg)
            return;

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

        #if UNITY_WEBGL && !UNITY_EDITOR

        AdManager.Instance.PlayAd(
            onCompleted: OnAdCompleted,
            onFailed: OnAdFailed
        );

        #else

        // Preserve existing Android behavior for now.
        AdManager.Instance.PlayAd(
            onCompleted: OnAdCompleted,
            onFailed: OnAdCompleted
        );

        #endif
    }
    
    private void OnAdFailed()
    {
        Debug.Log(
            "[SOS] Reward was not earned."
        );

        _isSOSRunning = false;

        if (_vm != null)
        {
            _vm.IsSOSMode.Value = false;
        }
    }
    private void OnAdCompleted()
    {
        if (_vm == null)
        {
            _isSOSRunning = false;
            return;
        }

        _vm.ApplySOSCommand.Execute();

        // Run the sequence from User, which is a persistent
        // active MonoBehaviour and is not part of the HUD.
        if (User.Instance != null)
        {
            User.Instance.StartCoroutine(
                MakeChangesProvidedBySOS(_vm)
            );
        }
        else
        {
            StartCoroutine(
                MakeChangesProvidedBySOS(_vm)
            );
        }
    }

    private IEnumerator MakeChangesProvidedBySOS(SudokuViewModel vm)
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
                    yield return new WaitForSecondsRealtime(1f);
                }
                _vm.SelectedRow.Value = entry.row;
                _vm.SelectedCol.Value = entry.col;
                _vm.EnterValueCommand.Execute(entry.number);
                Debug.Log($"SOS: Entering row {entry.row} and column {entry.col} entry to {entry.number}");
                yield return new WaitForSecondsRealtime(1f);
            }
        }
        finally
        {
            vm.ResetDemoMode();
            vm.HideHUD.Value = true;
            vm.SOSChangedCells.Value.Clear();
            vm.IsSOSMode.Value = false;
            _isSOSRunning = false;
        }
    }

}