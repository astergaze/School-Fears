using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance { get; private set; }

    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private List<ChoiceButton> choiceButtons;

    private int selectedIndex = 0;
    private List<DialogueChoice> currentChoices;
    private System.Action<DialogueChoice> onChoiceSelected;
    private int openedAtFrame = -1;

    private InputAction choice1Action;
    private InputAction choice2Action;
    private InputAction choice3Action;
    private InputAction choice4Action;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (InputSystem.actions != null)
        {
            var uiMap = InputSystem.actions.FindActionMap("UI");
            if (uiMap != null)
            {
                choice1Action = uiMap.FindAction("Choice_1");
                choice2Action = uiMap.FindAction("Choice_2");
                choice3Action = uiMap.FindAction("Choice_3");
                choice4Action = uiMap.FindAction("Choice_4");
                uiMap.Enable();
            }
        }
    }

    private void Update()
    {
        if (!choicesPanel.activeSelf) return;
        if (currentChoices == null || currentChoices.Count == 0) return;

        // Evita consumir la pulsación de 'E' que correspondía a abrir el diálogo en este mismo frame
        if (Time.frameCount == openedAtFrame) return;

        HandleKeyboardNavigation();
        HandleDirectShortcuts();
    }

    private void HandleKeyboardNavigation()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // Navegación vertical
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)
        {
            Navigate(1);
        }
        else if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
        {
            Navigate(-1);
        }

        // Confirmar con Enter, Espacio, NumpadEnter o E
        if (kb.enterKey.wasPressedThisFrame || 
            kb.spaceKey.wasPressedThisFrame || 
            kb.numpadEnterKey.wasPressedThisFrame || 
            kb.eKey.wasPressedThisFrame)
        {
            ConfirmCurrentSelection();
        }
    }

    private void HandleDirectShortcuts()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) { TrySelectDirect(0); return; }
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) { TrySelectDirect(1); return; }
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) { TrySelectDirect(2); return; }
            if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) { TrySelectDirect(3); return; }
        }

        if (choice1Action != null && choice1Action.WasPressedThisFrame()) TrySelectDirect(0);
        if (choice2Action != null && choice2Action.WasPressedThisFrame()) TrySelectDirect(1);
        if (choice3Action != null && choice3Action.WasPressedThisFrame()) TrySelectDirect(2);
        if (choice4Action != null && choice4Action.WasPressedThisFrame()) TrySelectDirect(3);
    }

    public void ShowChoices(DialogueChoice[] choices, System.Action<DialogueChoice> callback)
    {
        if (choices == null || choices.Length == 0) return;

        currentChoices = new List<DialogueChoice>(choices);
        onChoiceSelected = callback;
        openedAtFrame = Time.frameCount;

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].Setup(choices[i], i, OnChoiceClicked, OnButtonHovered);
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        selectedIndex = GetFirstAvailableIndex();
        UpdateSelection();
        choicesPanel.SetActive(true);
    }

    private void Navigate(int direction)
    {
        int count = currentChoices.Count;
        int nextIndex = selectedIndex;

        for (int i = 0; i < count; i++)
        {
            nextIndex = (nextIndex + direction + count) % count;
            if (currentChoices[nextIndex].state == ChoiceState.Available)
            {
                selectedIndex = nextIndex;
                UpdateSelection();
                return;
            }
        }
    }

    private void OnButtonHovered(int index)
    {
        if (index < 0 || index >= currentChoices.Count) return;
        selectedIndex = index;
        UpdateSelection();
    }

    private void ConfirmCurrentSelection()
    {
        if (selectedIndex < 0 || selectedIndex >= currentChoices.Count) return;
        if (currentChoices[selectedIndex].state != ChoiceState.Available) return;

        OnChoiceClicked(currentChoices[selectedIndex]);
    }

    private void TrySelectDirect(int index)
    {
        if (index >= currentChoices.Count) return;
        if (currentChoices[index].state != ChoiceState.Available) return;

        selectedIndex = index;
        UpdateSelection();
        OnChoiceClicked(currentChoices[selectedIndex]);
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            choiceButtons[i].SetSelected(i == selectedIndex);
        }
    }

    private void OnChoiceClicked(DialogueChoice choice)
    {
        if (choice.state != ChoiceState.Available) return;

        choicesPanel.SetActive(false);
        currentChoices = null;
        onChoiceSelected?.Invoke(choice);
    }

    public void OnChoiceButtonClicked(int index)
    {
        if (index >= currentChoices.Count) return;
        TrySelectDirect(index);
    }

    public void HideChoices()
    {
        choicesPanel.SetActive(false);
        currentChoices = null;
    }

    private int GetFirstAvailableIndex()
    {
        for (int i = 0; i < currentChoices.Count; i++)
        {
            if (currentChoices[i].state == ChoiceState.Available)
                return i;
        }
        return 0;
    }
}