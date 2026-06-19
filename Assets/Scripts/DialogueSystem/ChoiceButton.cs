using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChoiceButton : MonoBehaviour
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
    private System.Action<DialogueChoice> onSelected;

    public void Setup(DialogueChoice data, System.Action<DialogueChoice> callback)
    {
        choiceData = data;
        onSelected = callback;

        choiceText.text = data.text;
        ApplyState(data.state);
        SetSelected(false);
    }

    private void ApplyState(ChoiceState state)
    {
        bool locked = state != ChoiceState.Available;
        strikethrough.gameObject.SetActive(locked);
        choiceText.color = locked ? new Color(1f, 1f, 1f, 0.45f) : Color.white;

        if (!locked) return;

        switch (state)
        {
            case ChoiceState.LockedNeutral:
                strikethrough.color = strikeNeutral;
                break;
            case ChoiceState.LockedBad:
                strikethrough.color = strikeBad;
                break;
            case ChoiceState.LockedGood:
                strikethrough.color = strikeGood;
                break;
        }
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? selectedBg : normalBg;
        selectionOutline.gameObject.SetActive(selected);
    }

    public void OnClick()
    {
        if (choiceData.state != ChoiceState.Available) return;
        onSelected?.Invoke(choiceData);
    }

    public ChoiceState GetState() => choiceData.state;
}