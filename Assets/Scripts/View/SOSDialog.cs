using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System;

public class SOSAdDialog : MonoBehaviour
{
    public static SOSAdDialog Instance { get; private set; }

    private BaseViewModel _vm;
    private bool _isSOSRunning;

    void Awake()
    {
        Instance = this;
        _isSOSRunning = false;
    }

    public void Bind(BaseViewModel vm)
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
            RunSOSSequence(_vm);
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

        RunSOSSequence(_vm);
    }

    private void RunSOSSequence(BaseViewModel vm)
    {
        IEnumerator sequence = MakeChangesProvidedBySOS(vm);

        // User persists outside the HUD. Use it when available so a HUD
        // transition cannot stop the SOS animation midway.
        if (User.Instance != null)
            User.Instance.StartCoroutine(sequence);
        else
            StartCoroutine(sequence);
    }

    private IEnumerator MakeChangesProvidedBySOS(BaseViewModel vm)
    {
        vm.SetDemoMode();
        vm.HideHUD.Value = false;
        var arglist = vm.SOSChangedCells.Value;
        try
        {
            foreach(var entry in arglist)
            {
                if(vm.BoardValues.Value[entry.row, entry.col] != 0)
                {
                    vm.SelectedRow.Value = entry.row;
                    vm.SelectedCol.Value = entry.col;
                    vm.EnterValueCommand.Execute(0);
                    Debug.Log($"SOS: Deleting row {entry.row} and column {entry.col} entry");
                    yield return new WaitForSecondsRealtime(1f);
                }
                vm.SelectedRow.Value = entry.row;
                vm.SelectedCol.Value = entry.col;
                vm.EnterValueCommand.Execute(entry.number);
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