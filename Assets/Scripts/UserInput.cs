using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Copied from HunJam24
/// </summary>
public class UserInput : MonoBehaviour
{
    public static UserInput Instance;
    
    public float lastMoveInputsAvgTimeStep = 0.1f;
    public int lastMoveInputsAvgCount = 5;

    private float _lastMoveInputsAvgTimeCur;
    private int _lastMoveInputsAvgCurCount;

    #region Input mappings
    public Vector2 MoveInput { get; private set; }
    public Vector2 LastMoveInputsAvg { get; private set;  }


    public bool JumpButtonPressedThisFrame { get; private set; }
    public bool JumpButtonReleasedThisFrame { get; private set; }
    public bool JumpButtonHoldPerformed { get; private set; }
    

    public bool PauseMenuPressedThisFrame { get; private set; }
    
    private PlayerInput _playerInput;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _menuAction;

    #endregion



    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one UserInput instance in scene!");
            return;
        }
        Instance = this;

        _playerInput = GetComponent<PlayerInput>();

        if (_playerInput == null)
        {
            Debug.LogError("No PlayerInput on Player!");
            return;
        }
        
        _moveAction = _playerInput.actions["Move"];

        _jumpAction = _playerInput.actions["Jump"];

        _menuAction = _playerInput.actions["PauseMenu"];
        
        LastMoveInputsAvg = Vector2.zero;
    }

    private void Update()
    {
        UpdateInputMappings();
        UpdateLastInputs();
    }

    private void UpdateInputMappings()
    {
        MoveInput = _moveAction.ReadValue<Vector2>();

        JumpButtonPressedThisFrame = _jumpAction.WasPressedThisFrame();
        JumpButtonReleasedThisFrame = _jumpAction.WasReleasedThisFrame();
        JumpButtonHoldPerformed = _jumpAction.WasPerformedThisFrame();

        PauseMenuPressedThisFrame = _menuAction.WasPressedThisFrame();
        if (PauseMenuPressedThisFrame) Debug.Log("beep");
    }

    private void UpdateLastInputs()
    {
        _lastMoveInputsAvgTimeCur += Time.deltaTime;
        if (_lastMoveInputsAvgTimeCur < lastMoveInputsAvgTimeStep) return;
			
        _lastMoveInputsAvgTimeCur = 0;

        if (_lastMoveInputsAvgCurCount > lastMoveInputsAvgCount)
        {
            LastMoveInputsAvg += (MoveInput - LastMoveInputsAvg) / (lastMoveInputsAvgCount - 1);
        }
        else
        {
            _lastMoveInputsAvgCurCount++;
            LastMoveInputsAvg += MoveInput;

            if (_lastMoveInputsAvgCurCount == lastMoveInputsAvgCount)
            {
                LastMoveInputsAvg /= lastMoveInputsAvgCount;
            }
        }
    }
}
