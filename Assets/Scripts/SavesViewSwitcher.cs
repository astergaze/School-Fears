using TMPro;
using UnityEngine;

/// <summary>
/// Controlador único del panel de cuenta. Va en el GameObject ContinuePanel.
///
/// - Sin sesión: oculta los slots y el título de guardados, y deja solo el formulario.
/// - Con sesión: muestra los slots y el título de guardados.
/// - Alterna el formulario entre Login (usuario + clave) y Registro (usuario + correo + clave).
/// - Borra el texto "Buscando partidas..." mientras no haya sesión.
///
/// Reemplaza a AuthModeSwitcher y SavesViewSwitcher. No modifica ContinuePanelUI:
/// el cuadro de cuenta (LoggedInGroup) y el formulario (LoggedOutGroup) siguen
/// siendo controlados por ese script.
/// </summary>
public class AccountScreenController : MonoBehaviour
{
    private const string SearchingText = "Buscando partidas...";

    [Header("Solo con sesión iniciada")]
    [Tooltip("SlotsGroup, título 'Estados de guardados', etc.")]
    [SerializeField] private GameObject[] showWhenLoggedIn;

    [Header("Formulario: solo en Registro")]
    [SerializeField] private GameObject registerTitle;
    [SerializeField] private GameObject emailInput;
    [SerializeField] private GameObject registerButton;
    [SerializeField] private GameObject showLoginButton;

    [Header("Formulario: solo en Login")]
    [SerializeField] private GameObject loginTitle;
    [SerializeField] private GameObject loginButton;
    [SerializeField] private GameObject showRegisterButton;

    [Header("Estado")]
    [SerializeField] private TMP_Text statusLabel;

    private bool? lastLogged;

    private bool IsLogged => SaveManager.Instance != null && SaveManager.Instance.IsLoggedIn;

    private void OnEnable()
    {
        lastLogged = null;   // fuerza a re-aplicar el estado al abrir el panel
        Evaluate();
    }

    private void Update()
    {
        Evaluate();

        // ContinuePanelUI escribe "Buscando partidas..." al abrir el panel aunque no haya
        // sesión; mientras no la haya, lo borramos (los mensajes de error no se tocan).
        if (!IsLogged && statusLabel != null && statusLabel.text == SearchingText)
            statusLabel.text = "";
    }

    private void Evaluate()
    {
        bool logged = IsLogged;
        if (lastLogged == logged) return;
        lastLogged = logged;

        foreach (var go in showWhenLoggedIn)
            if (go != null) go.SetActive(logged);

        // Al quedar sin sesión (arranque o Log Out) el formulario vuelve a Login
        if (!logged) SetMode(false);
    }

    // Se enlazan desde el On Click de "¿No tenés cuenta? Registrate" / "Ya tengo cuenta"
    public void ShowLogin() => SetMode(false);
    public void ShowRegister() => SetMode(true);

    private void SetMode(bool register)
    {
        if (registerTitle != null) registerTitle.SetActive(register);
        if (emailInput != null) emailInput.SetActive(register);
        if (registerButton != null) registerButton.SetActive(register);
        if (showLoginButton != null) showLoginButton.SetActive(register);

        if (loginTitle != null) loginTitle.SetActive(!register);
        if (loginButton != null) loginButton.SetActive(!register);
        if (showRegisterButton != null) showRegisterButton.SetActive(!register);
    }
}