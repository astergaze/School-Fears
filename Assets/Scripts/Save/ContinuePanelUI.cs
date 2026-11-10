using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de "Jugar / Continuar": cuenta (login/registro) + 3 slots + resolución de conflictos.
/// Ponelo en el GameObject del continuePanel del menú y asigná las referencias en el Inspector.
/// Slot vacío => nueva partida. Slot con partida => continuar.
/// </summary>
public class ContinuePanelUI : MonoBehaviour
{
    [Serializable]
    public class SlotWidget
    {
        public Button button;
        public TMP_Text label;
        public Button deleteButton;      // opcional
        public TMP_Text deleteLabel;     // opcional (texto del botón borrar)
    }

    [Header("Cuenta")]
    public GameObject loggedOutGroup;
    public GameObject loggedInGroup;
    public TMP_InputField userInput;
    public TMP_InputField emailInput;    // solo para registrarse
    public TMP_InputField passInput;     // ponerlo con Content Type = Password
    public Button loginButton;
    public Button registerButton;
    public Button logoutButton;
    public TMP_Text accountLabel;
    public TMP_Text statusLabel;

    [Header("Slots (3)")]
    public SlotWidget[] slots = new SlotWidget[3];

    [Header("Conflicto (panel emergente)")]
    public GameObject conflictPanel;
    public TMP_Text conflictText;
    public Button useLocalButton;
    public Button useCloudButton;
    public Button cancelConflictButton;

    private SlotView[] views;
    private int conflictSlot = -1;
    private int pendingDeleteSlot = -1;
    private Coroutine deleteResetRoutine;

    private void Awake()
    {
        if (loginButton) loginButton.onClick.AddListener(OnLogin);
        if (registerButton) registerButton.onClick.AddListener(OnRegister);
        if (logoutButton) logoutButton.onClick.AddListener(OnLogout);
        if (useLocalButton) useLocalButton.onClick.AddListener(() => Resolve(true));
        if (useCloudButton) useCloudButton.onClick.AddListener(() => Resolve(false));
        if (cancelConflictButton) cancelConflictButton.onClick.AddListener(() => conflictPanel.SetActive(false));

        for (int i = 0; i < slots.Length; i++)
        {
            int index = i;   // captura correcta para el lambda
            if (slots[i].button) slots[i].button.onClick.AddListener(() => OnSlotClicked(index));
            if (slots[i].deleteButton) slots[i].deleteButton.onClick.AddListener(() => OnDeleteClicked(index));
        }
        if (conflictPanel) conflictPanel.SetActive(false);
    }

    private void OnEnable()
    {
        var sm = SaveManager.Instance;
        if (sm != null)
        {
            sm.OnStatus += SetStatus;
            sm.OnAuthChanged += UpdateAccountUI;
        }
        if (conflictPanel) conflictPanel.SetActive(false);
        UpdateAccountUI();
        Refresh();
    }

    private void OnDisable()
    {
        var sm = SaveManager.Instance;
        if (sm != null)
        {
            sm.OnStatus -= SetStatus;
            sm.OnAuthChanged -= UpdateAccountUI;
        }
    }

    private void Update()
    {
        // Mientras el SaveManager trabaja (descargando, cargando escena...) no se puede tocar nada
        var sm = SaveManager.Instance;
        bool busy = sm != null && sm.IsBusy;
        foreach (var s in slots)
        {
            if (s.button) s.button.interactable = !busy;
            if (s.deleteButton) s.deleteButton.interactable = !busy;
        }
        if (loginButton) loginButton.interactable = !busy;
        if (registerButton) registerButton.interactable = !busy;
    }

    // ---------------- Cuenta ----------------

    private void UpdateAccountUI()
    {
        var sm = SaveManager.Instance;
        if (sm == null) return;
        bool logged = sm.IsLoggedIn;
        if (loggedOutGroup) loggedOutGroup.SetActive(!logged);
        if (loggedInGroup) loggedInGroup.SetActive(logged);
        if (accountLabel)
            accountLabel.text = logged ? "Cuenta: " + sm.Username
                              : (string.IsNullOrEmpty(sm.Username) ? "Sin cuenta (partidas solo en este equipo)" : "Sesión vencida: " + sm.Username);
    }

    private void OnLogin()
    {
        string u = userInput ? userInput.text.Trim() : "";
        string p = passInput ? passInput.text : "";
        if (u.Length == 0 || p.Length == 0) { SetStatus("Ingresá usuario y contraseña."); return; }
        SaveManager.Instance.Login(u, p, AfterAuth);
    }

    private void OnRegister()
    {
        string u = userInput ? userInput.text.Trim() : "";
        string e = emailInput ? emailInput.text.Trim() : "";
        string p = passInput ? passInput.text : "";
        if (u.Length == 0 || e.Length == 0 || p.Length == 0) { SetStatus("Completá usuario, correo y contraseña."); return; }
        SaveManager.Instance.Register(u, e, p, AfterAuth);
    }

    private void AfterAuth(bool ok, string msg)
    {
        if (this == null) return;
        SetStatus(msg);
        if (ok)
        {
            if (passInput) passInput.text = "";
            UpdateAccountUI();
            Refresh();
        }
    }

    private void OnLogout()
    {
        SaveManager.Instance.Logout();
        UpdateAccountUI();
        Refresh();
    }

    // ---------------- Slots ----------------

    public void Refresh()
    {
        if (SaveManager.Instance == null) return;
        SetStatus("Buscando partidas...");
        SaveManager.Instance.RefreshSlots(v =>
        {
            if (this == null) return;
            views = v;
            Render();
            if (SaveManager.Instance.IsLoggedIn) SetStatus("");
        });
    }

    private void Render()
    {
        for (int i = 0; i < slots.Length && i < views.Length; i++)
        {
            var v = views[i];
            if (slots[i].label) slots[i].label.text = DescribeSlot(v);
            if (slots[i].deleteButton) slots[i].deleteButton.gameObject.SetActive(v.state != SlotState.Empty);
            if (slots[i].deleteLabel) slots[i].deleteLabel.text = "Borrar";
        }
        pendingDeleteSlot = -1;
    }

    private string DescribeSlot(SlotView v)
    {
        if (v.state == SlotState.Empty) return "Partida " + v.slot + "\nVacía - empezar nueva partida";

        string badge;
        switch (v.state)
        {
            case SlotState.Synced: badge = "Sincronizada"; break;
            case SlotState.LocalOnly: badge = v.cloudChecked ? "Pendiente de subir" : "Solo en este equipo"; break;
            case SlotState.LocalNewer: badge = "Pendiente de subir"; break;
            case SlotState.CloudOnly: badge = "En la nube (se descarga al continuar)"; break;
            case SlotState.CloudNewer: badge = "Hay una versión más nueva en la nube"; break;
            case SlotState.Conflict: badge = "CONFLICTO: tocá para elegir"; break;
            default: badge = ""; break;
        }
        return "Partida " + v.slot + " - " + (string.IsNullOrEmpty(v.Scene) ? "?" : v.Scene) + " - " + FormatTime(v.Playtime)
             + "\n" + FormatDate(v.SavedAt) + " - " + badge;
    }

    private void OnSlotClicked(int index)
    {
        if (views == null || index >= views.Length) return;
        var v = views[index];
        var sm = SaveManager.Instance;

        switch (v.state)
        {
            case SlotState.Empty:
                sm.StartNewGame(v.slot);
                break;
            case SlotState.Conflict:
                ShowConflict(v);
                break;
            default:
                sm.LoadSlot(v, msg => SetStatus(msg));
                break;
        }
    }

    // Borrar pide doble click (el primero cambia el texto a "¿Seguro?")
    private void OnDeleteClicked(int index)
    {
        if (views == null || index >= views.Length) return;
        if (pendingDeleteSlot != index)
        {
            pendingDeleteSlot = index;
            if (slots[index].deleteLabel) slots[index].deleteLabel.text = "¿Seguro?";
            if (deleteResetRoutine != null) StopCoroutine(deleteResetRoutine);
            deleteResetRoutine = StartCoroutine(ResetDeleteLabel(index));
            return;
        }
        pendingDeleteSlot = -1;
        SaveManager.Instance.DeleteSlot(views[index].slot, Refresh);
    }

    private IEnumerator ResetDeleteLabel(int index)
    {
        yield return new WaitForSecondsRealtime(3f);
        if (pendingDeleteSlot == index)
        {
            pendingDeleteSlot = -1;
            if (slots[index].deleteLabel) slots[index].deleteLabel.text = "Borrar";
        }
    }

    // ---------------- Conflicto ----------------

    private void ShowConflict(SlotView v)
    {
        conflictSlot = v.slot;
        if (conflictText)
            conflictText.text = "Partida " + v.slot + ": guardaste en otro dispositivo.\n\n"
                + "Este equipo: " + v.localScene + " - " + FormatTime(v.localPlaytime) + " - " + FormatDate(v.localSavedAt) + "\n"
                + "Nube: " + v.cloudScene + " - " + FormatTime(v.cloudPlaytime) + " - " + FormatDate(v.cloudSavedAt) + "\n\n"
                + "¿Cuál querés conservar? La otra se pierde.";
        if (conflictPanel) conflictPanel.SetActive(true);
    }

    private void Resolve(bool useLocal)
    {
        if (conflictSlot < 1) return;
        if (conflictPanel) conflictPanel.SetActive(false);
        SaveManager.Instance.ResolveConflict(conflictSlot, useLocal, (ok, msg) =>
        {
            if (this == null) return;
            SetStatus(msg);
            Refresh();
        });
    }

    // ---------------- Helpers ----------------

    private void SetStatus(string msg)
    {
        if (statusLabel) statusLabel.text = msg;
    }

    private static string FormatTime(int seconds)
    {
        int h = seconds / 3600;
        int m = (seconds % 3600) / 60;
        return h > 0 ? h + " h " + m + " min" : m + " min";
    }

    private static string FormatDate(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "";
        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
            return dt.ToLocalTime().ToString("dd/MM HH:mm");
        return "";
    }
}
