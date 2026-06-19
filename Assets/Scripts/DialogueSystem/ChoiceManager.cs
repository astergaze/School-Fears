using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class ChoiceManager : MonoBehaviour
{
    public static ChoiceManager Instance { get; private set; }

    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private List<ChoiceButton> choiceButtons;

    private int selectedIndex = 0;
    private List<DialogueChoice> currentChoices;
    private System.Action<DialogueChoice> onChoiceSelected;

    private InputAction choice1Action;
    private InputAction choice2Action;
    private InputAction choice3Action;
    private InputAction choice4Action;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        var uiMap = InputSystem.actions.FindActionMap("UI");
        choice1Action = uiMap.FindAction("Choice_1");
        choice2Action = uiMap.FindAction("Choice_2");
        choice3Action = uiMap.FindAction("Choice_3");
        choice4Action = uiMap.FindAction("Choice_4");
        
        uiMap.Enable();
    }

    private void Update()
    {
        if (!choicesPanel.activeSelf) return;
        if (currentChoices == null || currentChoices.Count == 0) return;

        if (choice1Action != null && choice1Action.WasPressedThisFrame()) TrySelectIndex(0);
        if (choice2Action != null && choice2Action.WasPressedThisFrame()) TrySelectIndex(1);
        if (choice3Action != null && choice3Action.WasPressedThisFrame()) TrySelectIndex(2);
        if (choice4Action != null && choice4Action.WasPressedThisFrame()) TrySelectIndex(3);
    }

    public void ShowChoices(DialogueChoice[] choices, System.Action<DialogueChoice> callback)
    {
        if (choices == null || choices.Length == 0) return;

        currentChoices = new List<DialogueChoice>(choices);
        onChoiceSelected = callback;

        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);
                choiceButtons[i].Setup(choices[i], OnChoiceClicked);
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }

        selectedIndex = 0;
        UpdateSelection();
        choicesPanel.SetActive(true);
    }

    private void TrySelectIndex(int index)
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
            choiceButtons[i].SetSelected(i == selectedIndex);
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
        TrySelectIndex(index);
    }

    public void HideChoices()
    {
        choicesPanel.SetActive(false);
        currentChoices = null;
    }
}