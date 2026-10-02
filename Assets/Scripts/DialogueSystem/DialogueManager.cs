using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    private InputAction dialogueAdvanceAction;

    public event System.Action<DialogueChoice> ChoiceSelected;
    public event System.Action OnDialogueEnded;

    [Header("UI References")]
    [SerializeField] private GameObject dialogueSystem;
    [SerializeField] private Image dialogueBox;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Dialogue Box Sprites")]
    [SerializeField] private Sprite alexMascSprite;
    [SerializeField] private Sprite alexFemSprite;

    [Header("Character Colors")]
    [SerializeField] private Color alexMascColor = new Color(0.85f, 0.65f, 0.1f);
    [SerializeField] private Color alexFemColor = new Color(0.48f, 0.18f, 0.75f);
    [SerializeField] private Color charlotteColor = new Color(0.1f, 0.3f, 0.8f);
    [SerializeField] private Color neutralColor = Color.white;

    [Header("Typewriter")]
    [SerializeField] private float typewriterSpeed = 0.04f;

    private Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();
    private bool isTyping = false;
    private bool dialogueActive = false;
    private bool isWaitingForChoice = false; // <-- BLOQUEA EL AVANCE DE 'E' DURANTE OPCIONES

    public bool IsDialogueActive => dialogueActive;

    private Coroutine typewriterCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (InputSystem.actions != null)
            dialogueAdvanceAction = InputSystem.actions.FindAction("Interact");
    }

    private void Start()
    {
        dialogueSystem.SetActive(false);
    }

    private void Update()
    {
        if (!dialogueActive) return;

        // Si hay un panel de opciones abierto, DialogueManager NO debe procesar 'E'
        if (isWaitingForChoice) return;

        if (dialogueAdvanceAction != null && dialogueAdvanceAction.WasPressedThisFrame())
        {
            if (!isTyping)
                ShowNextLine();
        }
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        dialogueQueue.Clear();
        foreach (var line in lines)
            dialogueQueue.Enqueue(line);

        isWaitingForChoice = false;
        dialogueSystem.SetActive(true);
        dialogueActive = true;
        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();
        ApplySpeakerStyle(line.speaker);

        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);

        string lineText = LocalizationManager.Instance.GetText(line.textId);
        typewriterCoroutine = StartCoroutine(TypewriterEffect(lineText, () =>
        {
            if (line.choices != null && line.choices.Length > 0)
            {
                isWaitingForChoice = true;
                ChoiceManager.Instance.ShowChoices(line.choices, OnChoiceSelected);
            }
        }));
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        isWaitingForChoice = false;
        ChoiceSelected?.Invoke(choice);

        if (choice.responseLines != null && choice.responseLines.Length > 0)
            StartDialogue(choice.responseLines);
        else
            EndDialogue();
    }

    private void ApplySpeakerStyle(DialogueSpeaker speaker)
    {
        switch (speaker)
        {
            case DialogueSpeaker.AlexMasc:
                characterNameText.text = "Alex";
                characterNameText.color = alexMascColor;
                dialogueBox.sprite = alexMascSprite;
                break;
            case DialogueSpeaker.AlexFem:
                characterNameText.text = "Alex";
                characterNameText.color = alexFemColor;
                dialogueBox.sprite = alexFemSprite;
                break;
            case DialogueSpeaker.Charlotte:
                characterNameText.text = "Charlotte";
                characterNameText.color = charlotteColor;
                dialogueBox.sprite = alexFemSprite;
                break;
            case DialogueSpeaker.Narrator:
                characterNameText.text = "";
                characterNameText.color = neutralColor;
                dialogueBox.sprite = alexMascSprite;
                break;
        }
    }

    private IEnumerator TypewriterEffect(string text, System.Action onComplete = null)
    {
        isTyping = true;
        dialogueText.text = "";
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        isTyping = false;
        onComplete?.Invoke();
    }

    private void EndDialogue()
    {
        isWaitingForChoice = false;
        dialogueActive = false;
        dialogueSystem.SetActive(false);
        OnDialogueEnded?.Invoke();
    }
}