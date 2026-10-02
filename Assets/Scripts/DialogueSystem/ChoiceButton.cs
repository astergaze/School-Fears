using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(Button))]
public class ChoiceButton : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI choiceText;
    [SerializeField] private Image strikethrough;
    [SerializeField] private Image selectionOutline;

    private Color normalBg = new Color(1f, 1f, 1f, 0.04f);
    private Color selectedBg = new Color(1f, 1f, 1f, 0.08f);
    private Color strikeNeutral = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    private Color strikeBad = new Color(0.47f, 0.08f, 0.08f, 0.85f);
    private Color strikeGood = new Color(0.39f, 0.24f, 0.7f, 0.85f);

    private DialogueChoice choiceData;
    private int buttonIndex;
    private System.Action<DialogueChoice> onSelected;
    private System.Action<int> onHovered;
    private Button buttonComponent;

    private void Awake()
    {
        buttonComponent = GetComponent<Button>();
        buttonComponent.onClick.AddListener(OnClickButton);
    }

    public void Setup(DialogueChoice data, int index, System.Action<DialogueChoice> selectCallback, System.Action<int> hoverCallback)
    {
        choiceData = data;
        buttonIndex = index;
        onSelected = selectCallback;
        onHovered = hoverCallback;

        if (choiceText != null)
            choiceText.text = LocalizationManager.Instance.GetText(data.textId);

        ApplyState(data.state);
        SetSelected(false);
    }

    private void ApplyState(ChoiceState state)
    {
        bool locked = state != ChoiceState.Available;
        if (strikethrough != null)
            strikethrough.gameObject.SetActive(locked);

        if (choiceText != null)
            choiceText.color = locked ? new Color(1f, 1f, 1f, 0.45f) : Color.white;

        if (buttonComponent != null)
            buttonComponent.interactable = !locked;

        if (!locked) return;

        switch (state)
        {
            case ChoiceState.LockedNeutral:
                if (strikethrough != null) strikethrough.color = strikeNeutral;
                break;
            case ChoiceState.LockedBad:
                if (strikethrough != null) strikethrough.color = strikeBad;
                break;
            case ChoiceState.LockedGood:
                if (strikethrough != null) strikethrough.color = strikeGood;
                break;
        }
    }

    public void SetSelected(bool selected)
    {
        if (background != null)
            background.color = selected ? selectedBg : normalBg;

        if (selectionOutline != null)
            selectionOutline.gameObject.SetActive(selected);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onHovered?.Invoke(buttonIndex);
    }

    private void OnClickButton()
    {
        Submit();
    }

    public void Submit()
    {
        if (choiceData == null || choiceData.state != ChoiceState.Available) return;
        onSelected?.Invoke(choiceData);
    }

    public ChoiceState GetState() => choiceData != null ? choiceData.state : ChoiceState.LockedNeutral;
}