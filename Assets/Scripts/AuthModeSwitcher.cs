using UnityEngine;

/// <summary>
/// Alterna entre la vista de Login y la de Registro dentro de LoggedOutGroup.
/// No toca la lógica de ContinuePanelUI: solo muestra/oculta campos, botones y títulos.
///
/// Login    -> usuario + clave           (título LOGIN, botón LOGIN)
/// Registro -> usuario + correo + clave  (título REGISTER, botón REGISTER)
///
/// Ponelo en el GameObject LoggedOutGroup. Cada vez que ese grupo se activa
/// (al abrir el panel o al cerrar sesión) arranca en la vista de Login.
/// </summary>
public class AuthModeSwitcher : MonoBehaviour
{
    [Header("Solo en Registro")]
    [SerializeField] private GameObject registerTitle;
    [SerializeField] private GameObject emailInput;
    [SerializeField] private GameObject registerButton;
    [SerializeField] private GameObject showLoginButton;

    [Header("Solo en Login")]
    [SerializeField] private GameObject loginTitle;
    [SerializeField] private GameObject loginButton;
    [SerializeField] private GameObject showRegisterButton;

    private void OnEnable() => ShowLogin();

    // Se enlazan desde el On Click de los botones "Registrate" / "Ya tengo cuenta"
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