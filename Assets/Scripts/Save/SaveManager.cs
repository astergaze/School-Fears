using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SlotState { Empty, LocalOnly, CloudOnly, Synced, LocalNewer, CloudNewer, Conflict }

/// <summary>Resumen de un slot para mostrar en el menú.</summary>
public class SlotView
{
    public int slot;
    public SlotState state;
    public bool cloudChecked;          // false si no se pudo consultar la nube (offline / sin sesión)

    public string localScene, cloudScene;
    public int localPlaytime, cloudPlaytime;
    public string localSavedAt, cloudSavedAt;

    // Datos "a mostrar": la nube si es más nueva, si no la copia local
    public string Scene => state == SlotState.CloudOnly || state == SlotState.CloudNewer ? cloudScene : localScene;
    public int Playtime => state == SlotState.CloudOnly || state == SlotState.CloudNewer ? cloudPlaytime : localPlaytime;
    public string SavedAt => state == SlotState.CloudOnly || state == SlotState.CloudNewer ? cloudSavedAt : localSavedAt;
}

/// <summary>
/// Singleton persistente que se crea solo (no hay que ponerlo en ninguna escena).
/// - Guarda SIEMPRE en el equipo (persistentDataPath) y, si hay sesión, sube a la nube.
/// - Si no hay internet, la partida queda "pendiente" y se sube después.
/// - Si dos dispositivos guardaron distinto, avisa (conflicto) y el jugador elige cuál conservar.
/// </summary>
public class SaveManager : MonoBehaviour
{
    // ================= CONFIGURACIÓN =================
    private const string BaseUrl = "https://morkcloud.onrender.com/";   // <-- cambiar por la URL de Render
    private const string MenuScene = "menu";
    private const string FirstGameScene = "SafeRoomChp1";           // escena que crea Player/GameManager
    private static readonly string[] NoSaveScenes = { "menu", FirstGameScene };
    private const int SlotCount = 3;
    private const string PrefToken = "mork_token";
    private const string PrefUser = "mork_user";
    // =================================================

    public static SaveManager Instance { get; private set; }

    public int ActiveSlot { get; private set; } = -1;
    public bool IsBusy { get; private set; }
    public bool IsLoggedIn => !string.IsNullOrEmpty(token);
    public string Username { get; private set; } = "";
    public string LastStatus { get; private set; } = "";

    public event Action OnAuthChanged;
    public event Action<string> OnStatus;
    public event Action<int, CloudMeta> OnConflict;

    private readonly CloudApi api = new CloudApi();
    private string token = "";
    private bool isLoadingSave;
    private bool inMenu = true;
    private bool uploading;
    private bool flushingPlaytime;
    private float slotPlaytime;
    private float unsyncedPlaytime;
    private float flushTimer;
    private readonly HashSet<int> conflicts = new HashSet<int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SaveManager");
        DontDestroyOnLoad(go);
        go.AddComponent<SaveManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        api.BaseUrl = BaseUrl;
        token = PlayerPrefs.GetString(PrefToken, "");
        Username = PlayerPrefs.GetString(PrefUser, "");
        api.Token = token;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // "Despierta" al servidor (Render se duerme) y sube lo que haya quedado pendiente
        StartCoroutine(api.Send("GET", "/api/health", null, _ => { }));
        if (IsLoggedIn) StartCoroutine(SyncPendingRoutine());
    }

    private void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // Tiempo jugado de la partida activa (Time.deltaTime = 0 en pausa)
        if (ActiveSlot > 0 && !isLoadingSave && !inMenu)
        {
            slotPlaytime += Time.deltaTime;
            unsyncedPlaytime += Time.deltaTime;
            flushTimer += Time.deltaTime;
            if (flushTimer >= 60f)
            {
                flushTimer = 0f;
                if (IsLoggedIn && !flushingPlaytime) StartCoroutine(FlushPlaytimeRoutine());
            }
        }
    }

    private void OnApplicationQuit()
    {
        // Al cerrar no da tiempo a subir: guarda local y queda pendiente para la próxima vez
        if (ActiveSlot > 0 && !isLoadingSave) SaveNow(false);
    }

    // ================= Utilidades =================

    private static T Parse<T>(string json) where T : new()
    {
        try
        {
            var o = JsonUtility.FromJson<T>(json);
            return o != null ? o : new T();
        }
        catch { return new T(); }
    }

    private void Status(string msg)
    {
        LastStatus = msg;
        Debug.Log("[Save] " + msg);
        OnStatus?.Invoke(msg);
    }

    private string AccountKey => string.IsNullOrEmpty(Username) ? "guest" : Username.ToLowerInvariant();
    private string PathFor(int slot) => Path.Combine(Application.persistentDataPath, $"save_{AccountKey}_slot{slot}.json");

    private LocalSlotFile ReadLocal(int slot)
    {
        string p = PathFor(slot);
        if (!File.Exists(p)) return null;
        try
        {
            var f = JsonUtility.FromJson<LocalSlotFile>(File.ReadAllText(p));
            return (f != null && f.data != null) ? f : null;
        }
        catch { return null; }
    }

    private void WriteLocal(int slot, LocalSlotFile f)
    {
        string p = PathFor(slot);
        string tmp = p + ".tmp";
        File.WriteAllText(tmp, JsonUtility.ToJson(f));
        if (File.Exists(p)) File.Delete(p);
        File.Move(tmp, p);
    }

    private bool InNoSaveScene() => Array.IndexOf(NoSaveScenes, SceneManager.GetActiveScene().name) >= 0;

    // ================= Cuenta =================

    private void SetAuth(string newToken, string newUser)
    {
        token = newToken ?? "";
        Username = newUser ?? "";
        api.Token = token;
        PlayerPrefs.SetString(PrefToken, token);
        PlayerPrefs.SetString(PrefUser, Username);
        PlayerPrefs.Save();
        OnAuthChanged?.Invoke();
    }

    private void ExpireSession()
    {
        // Se conserva el nombre de usuario para no mezclar las copias locales con las de "guest"
        token = "";
        api.Token = "";
        PlayerPrefs.DeleteKey(PrefToken);
        OnAuthChanged?.Invoke();
        Status("Tu sesión venció. Iniciá sesión de nuevo para sincronizar.");
    }

    public void Logout()
    {
        if (ActiveSlot > 0) return;   // solo desde el menú
        SetAuth("", "");
    }

    public void Login(string user, string pass, Action<bool, string> done)
    {
        if (IsBusy) return;
        var body = JsonUtility.ToJson(new LoginRequest { mUser = user, mPass = pass });
        StartCoroutine(AuthRoutine("/api/login", body, done));
    }

    public void Register(string user, string email, string pass, Action<bool, string> done)
    {
        if (IsBusy) return;
        var body = JsonUtility.ToJson(new RegisterRequest { usuario = user, email = email, password = pass });
        StartCoroutine(AuthRoutine("/api/register", body, done));
    }

    private IEnumerator AuthRoutine(string path, string body, Action<bool, string> done)
    {
        IsBusy = true;
        Status("Conectando... (la primera vez puede tardar hasta 1 minuto)");
        ApiResult r = null;
        yield return api.Send("POST", path, body, x => r = x);
        IsBusy = false;

        if (r.networkError) { done(false, "No se pudo conectar con el servidor."); yield break; }

        var resp = Parse<AuthResponse>(r.text);
        if (r.Ok && !string.IsNullOrEmpty(resp.token))
        {
            SetAuth(resp.token, resp.usuario);
            done(true, "Sesión iniciada como " + resp.usuario + ".");
            StartCoroutine(SyncPendingRoutine());
        }
        else
        {
            done(false, string.IsNullOrEmpty(resp.error) ? "Error del servidor (" + r.status + ")." : resp.error);
        }
    }

    // ================= Capturar / aplicar estado =================

    private SaveData Capture()
    {
        var gm = GameManager.Instance;
        var pl = Player.Instance;
        if (gm == null || pl == null) return null;

        var d = new SaveData
        {
            scene = SceneManager.GetActiveScene().name,
            playtimeSeconds = Mathf.FloorToInt(slotPlaytime)
        };
        d.flags = gm.ExportFlags();
        d.notes = gm.ExportNotes();

        if (pl.Inventory != null)
            foreach (var it in pl.Inventory.GetAllItems())
                if (it != null) d.items.Add(it.name);

        Vector3 p = pl.transform.position;
        d.hasPosition = true;
        d.posX = p.x; d.posY = p.y; d.posZ = p.z;

        var fl = FlashlightController.Instance;
        if (fl != null) { d.flashlightUnlocked = fl.Unlocked; d.flashlightOn = fl.IsOn; }
        return d;
    }

    private void ApplyState(SaveData d)
    {
        GameManager.Instance.ImportState(d.flags, d.notes);

        var inv = Player.Instance.Inventory;
        inv.Clear();
        var db = ItemDatabase.Instance;
        foreach (var name in d.items)
        {
            var it = db != null ? db.Find(name) : null;
            if (it != null) inv.AddItem(it);
            else Debug.LogWarning("[Save] Ítem no encontrado en ItemDatabase: " + name);
        }

        var fl = FlashlightController.Instance;
        if (fl != null) fl.RestoreState(d.flashlightUnlocked, d.flashlightOn);
    }

    // ================= Guardar =================

    /// <summary>Guarda la partida activa (local siempre; nube si hay sesión). Devuelve false si no se pudo.</summary>
    public bool SaveNow(bool upload = true)
    {
        if (ActiveSlot < 1 || isLoadingSave || InNoSaveScene()) return false;
        var d = Capture();
        if (d == null) return false;

        var f = ReadLocal(ActiveSlot) ?? new LocalSlotFile();
        f.data = d;
        f.savedAtUtc = DateTime.UtcNow.ToString("o");
        f.pendingUpload = true;
        WriteLocal(ActiveSlot, f);

        if (upload && IsLoggedIn) StartCoroutine(UploadRoutine(ActiveSlot, false));
        return true;
    }

    private IEnumerator UploadRoutine(int slot, bool force)
    {
        if (uploading || !IsLoggedIn) yield break;
        if (!force && conflicts.Contains(slot)) yield break;

        var f = ReadLocal(slot);
        if (f == null || !f.pendingUpload) yield break;

        uploading = true;
        var req = new PutRequest
        {
            data = f.data,
            baseVersion = f.cloudVersion,
            scene = f.data.scene,
            playtimeSeconds = f.data.playtimeSeconds,
            force = force
        };
        ApiResult r = null;
        yield return api.Send("PUT", "/api/saves/" + slot, JsonUtility.ToJson(req), x => r = x);
        uploading = false;

        if (r.networkError)
        {
            Status("Sin conexión: la partida quedó guardada en este equipo y se subirá más tarde.");
            yield break;
        }
        if (r.status == 401) { ExpireSession(); yield break; }
        if (r.status == 409)
        {
            conflicts.Add(slot);
            var resp409 = Parse<PutResponse>(r.text);
            Status("Hay una partida más nueva en la nube para el slot " + slot + ". Resolvelo desde el menú.");
            OnConflict?.Invoke(slot, resp409.cloud);
            yield break;
        }
        if (!r.Ok)
        {
            Status("No se pudo subir la partida (error " + r.status + ").");
            yield break;
        }

        var ok = Parse<PutResponse>(r.text);
        var cur = ReadLocal(slot);
        bool changedMeanwhile = false;
        if (cur != null)
        {
            cur.cloudVersion = ok.version;
            changedMeanwhile = cur.savedAtUtc != f.savedAtUtc;   // se guardó otra vez mientras subía
            cur.pendingUpload = changedMeanwhile;
            WriteLocal(slot, cur);
        }
        Status("Partida sincronizada con la nube.");
        if (changedMeanwhile) StartCoroutine(UploadRoutine(slot, false));
    }

    private IEnumerator SyncPendingRoutine()
    {
        for (int i = 1; i <= SlotCount; i++)
        {
            var f = ReadLocal(i);
            if (f != null && f.pendingUpload && i != ActiveSlot)
                yield return StartCoroutine(UploadRoutine(i, false));
        }
        if (unsyncedPlaytime >= 30f && !flushingPlaytime) yield return StartCoroutine(FlushPlaytimeRoutine());
    }

    private IEnumerator FlushPlaytimeRoutine()
    {
        int seconds = Mathf.Min(3600, Mathf.FloorToInt(unsyncedPlaytime));
        if (seconds < 1 || !IsLoggedIn) yield break;
        flushingPlaytime = true;
        ApiResult r = null;
        yield return api.Send("POST", "/api/playtime", JsonUtility.ToJson(new PlaytimeRequest { seconds = seconds }), x => r = x);
        flushingPlaytime = false;
        if (r.Ok) unsyncedPlaytime -= seconds;
        else if (r.status == 401) ExpireSession();
    }

    // ================= Listar slots (menú) =================

    public void RefreshSlots(Action<SlotView[]> done) { StartCoroutine(RefreshRoutine(done)); }

    private IEnumerator RefreshRoutine(Action<SlotView[]> done)
    {
        SlotInfo[] cloud = null;
        bool cloudOk = false;

        if (IsLoggedIn)
        {
            ApiResult r = null;
            yield return api.Send("GET", "/api/saves", null, x => r = x);
            if (r.Ok)
            {
                cloud = Parse<SlotList>("{\"items\":" + r.text + "}").items;
                cloudOk = true;
            }
            else if (r.status == 401) ExpireSession();
            else Status("No se pudo consultar la nube. Se muestran las partidas de este equipo.");
        }

        var views = new SlotView[SlotCount];
        for (int i = 1; i <= SlotCount; i++)
        {
            SlotInfo c = null;
            if (cloud != null)
                foreach (var s in cloud) if (s.slot == i) c = s;
            views[i - 1] = BuildView(i, ReadLocal(i), c, cloudOk);
            if (views[i - 1].state == SlotState.Conflict) conflicts.Add(i); else conflicts.Remove(i);
        }
        done?.Invoke(views);
    }

    private SlotView BuildView(int slot, LocalSlotFile local, SlotInfo c, bool cloudOk)
    {
        var v = new SlotView { slot = slot, cloudChecked = cloudOk };
        bool hasLocal = local != null;
        bool hasCloud = c != null;

        if (hasLocal) { v.localScene = local.data.scene; v.localPlaytime = local.data.playtimeSeconds; v.localSavedAt = local.savedAtUtc; }
        if (hasCloud) { v.cloudScene = c.scene; v.cloudPlaytime = c.playtime_seconds; v.cloudSavedAt = c.updated_at; }

        if (!hasLocal && !hasCloud) v.state = SlotState.Empty;
        else if (hasLocal && !hasCloud) v.state = SlotState.LocalOnly;
        else if (!hasLocal) v.state = SlotState.CloudOnly;
        else
        {
            bool cloudAhead = c.version > local.cloudVersion;
            if (cloudAhead && local.pendingUpload) v.state = SlotState.Conflict;
            else if (cloudAhead) v.state = SlotState.CloudNewer;
            else if (local.pendingUpload) v.state = SlotState.LocalNewer;
            else v.state = SlotState.Synced;
        }
        return v;
    }

    // ================= Empezar / continuar =================

    /// <summary>Nueva partida en un slot VACÍO.</summary>
    public void StartNewGame(int slot)
    {
        if (IsBusy) return;
        StartCoroutine(EnterGameRoutine(slot, null));
    }

    /// <summary>Continúa un slot (descarga de la nube si hace falta). onError recibe un mensaje.</summary>
    public void LoadSlot(SlotView v, Action<string> onError)
    {
        if (IsBusy) return;
        StartCoroutine(LoadSlotRoutine(v, onError));
    }

    private IEnumerator LoadSlotRoutine(SlotView v, Action<string> onError)
    {
        IsBusy = true;
        if (v.state == SlotState.Conflict)
        {
            IsBusy = false;
            onError?.Invoke("Este slot tiene un conflicto: elegí qué versión conservar.");
            yield break;
        }

        if (v.state == SlotState.CloudOnly || v.state == SlotState.CloudNewer)
        {
            Status("Descargando partida de la nube...");
            bool ok = false;
            yield return StartCoroutine(DownloadRoutine(v.slot, x => ok = x));
            if (!ok) { IsBusy = false; onError?.Invoke("No se pudo descargar la partida."); yield break; }
        }

        var local = ReadLocal(v.slot);
        if (local == null) { IsBusy = false; onError?.Invoke("No se encontró la partida."); yield break; }

        yield return StartCoroutine(EnterGameRoutine(v.slot, local.data));
    }

    private IEnumerator DownloadRoutine(int slot, Action<bool> done)
    {
        ApiResult r = null;
        yield return api.Send("GET", "/api/saves/" + slot, null, x => r = x);
        if (!r.Ok) { done(false); yield break; }

        var resp = Parse<GetResponse>(r.text);
        if (resp.data == null || string.IsNullOrEmpty(resp.data.scene)) { done(false); yield break; }

        WriteLocal(slot, new LocalSlotFile
        {
            cloudVersion = resp.version,
            pendingUpload = false,
            savedAtUtc = resp.updated_at,
            data = resp.data
        });
        conflicts.Remove(slot);
        done(true);
    }

    private IEnumerator EnterGameRoutine(int slot, SaveData data)
    {
        IsBusy = true;
        isLoadingSave = true;
        ActiveSlot = slot;
        slotPlaytime = data != null ? data.playtimeSeconds : 0f;
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // Arrancar de cero, igual que hace GameOverController al volver al menú
        if (Player.Instance != null) Destroy(Player.Instance.gameObject);
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        if (DialogueManager.Instance != null) Destroy(DialogueManager.Instance.gameObject);
        yield return null;

        SceneManager.LoadScene(FirstGameScene);

        float t = 0f;
        while ((Player.Instance == null || GameManager.Instance == null) && t < 8f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        yield return null;
        yield return null;   // que corran los Start() de la escena inicial

        if (data != null && Player.Instance != null && GameManager.Instance != null)
        {
            ApplyState(data);

            if (data.scene != SceneManager.GetActiveScene().name && SceneTransitioner.Instance != null)
            {
                SceneTransitioner.Instance.GoToScene(data.scene, null, null);
                float t2 = 0f;
                while (SceneManager.GetActiveScene().name != data.scene && t2 < 20f)
                {
                    t2 += Time.unscaledDeltaTime;
                    yield return null;
                }
                yield return null;
                yield return null;   // que los spawners de la escena terminen antes de reposicionar
            }

            if (data.hasPosition && Player.Instance != null)
                Player.Instance.TeleportTo(new Vector3(data.posX, data.posY + 0.05f, data.posZ), Player.Instance.transform.rotation);
        }

        isLoadingSave = false;
        IsBusy = false;
        SaveNow();   // crea/actualiza el slot enseguida
    }

    // ================= Borrar y resolver conflictos =================

    public void DeleteSlot(int slot, Action done)
    {
        if (IsBusy) return;
        StartCoroutine(DeleteRoutine(slot, done));
    }

    private IEnumerator DeleteRoutine(int slot, Action done)
    {
        IsBusy = true;
        string p = PathFor(slot);
        if (File.Exists(p)) File.Delete(p);
        conflicts.Remove(slot);

        if (IsLoggedIn)
        {
            ApiResult r = null;
            yield return api.Send("DELETE", "/api/saves/" + slot, null, x => r = x);
            if (r.networkError) Status("Sin conexión: se borró solo en este equipo; en la nube sigue guardada.");
            else if (r.status == 401) ExpireSession();
        }
        IsBusy = false;
        done?.Invoke();
    }

    /// <summary>useLocal=true: sube la copia de este equipo y pisa la nube. false: baja la de la nube y pisa la local.</summary>
    public void ResolveConflict(int slot, bool useLocal, Action<bool, string> done)
    {
        if (IsBusy) return;
        StartCoroutine(ResolveRoutine(slot, useLocal, done));
    }

    private IEnumerator ResolveRoutine(int slot, bool useLocal, Action<bool, string> done)
    {
        IsBusy = true;
        conflicts.Remove(slot);
        bool ok;
        if (useLocal)
        {
            yield return StartCoroutine(UploadRoutine(slot, true));
            var f = ReadLocal(slot);
            ok = f != null && !f.pendingUpload;
            done?.Invoke(ok, ok ? "Se subió la partida de este equipo." : "No se pudo subir la partida.");
        }
        else
        {
            ok = false;
            yield return StartCoroutine(DownloadRoutine(slot, x => ok = x));
            done?.Invoke(ok, ok ? "Se usó la partida de la nube." : "No se pudo descargar la partida.");
        }
        IsBusy = false;
    }

    // ================= Autoguardado =================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        inMenu = scene.name == MenuScene;
        if (inMenu)
        {
            ActiveSlot = -1;
            isLoadingSave = false;
            return;
        }
        if (isLoadingSave || ActiveSlot < 1) return;
        StartCoroutine(AutoSaveAfterLoad());
    }

    private IEnumerator AutoSaveAfterLoad()
    {
        // Esperar a que los spawners ubiquen al jugador antes de capturar la posición
        yield return null;
        yield return null;
        yield return new WaitForSeconds(0.5f);
        SaveNow();
    }
}
