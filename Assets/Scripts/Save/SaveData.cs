using System;
using System.Collections.Generic;

/// <summary>
/// Lo que se guarda de una partida. Todo son tipos que JsonUtility entiende.
/// Si más adelante querés guardar algo nuevo, agregá un campo acá y llenalo en
/// SaveManager.Capture() / aplicalo en SaveManager.ApplyState().
/// </summary>
[Serializable]
public class SaveData
{
    public int schemaVersion = 1;
    public string scene;
    public int playtimeSeconds;

    public List<string> flags = new List<string>();   // GameManager.flags
    public List<string> notes = new List<string>();   // GameManager.collectedNoteIds
    public List<string> items = new List<string>();   // nombres de los ItemData del inventario

    public bool flashlightUnlocked;
    public bool flashlightOn;

    public bool hasPosition;
    public float posX, posY, posZ;
}

/// <summary>Archivo local de un slot: la partida + datos de sincronización con la nube.</summary>
[Serializable]
public class LocalSlotFile
{
    public int cloudVersion;      // versión de la nube con la que está basada esta copia (0 = nunca subida)
    public bool pendingUpload;    // true = hay cambios locales que todavía no se subieron
    public string savedAtUtc;     // ISO 8601
    public SaveData data;
}

// ---------- DTOs de la API (los nombres de campo deben coincidir con el JSON del servidor) ----------

[Serializable] public class LoginRequest { public string mUser; public string mPass; }
[Serializable] public class RegisterRequest { public string usuario; public string email; public string password; }
[Serializable] public class AuthResponse { public bool success; public string usuario; public string token; public string error; }

[Serializable]
public class PutRequest
{
    public SaveData data;
    public int baseVersion;
    public string scene;
    public int playtimeSeconds;
    public bool force;
}

[Serializable] public class CloudMeta { public int version; public string scene; public int playtime_seconds; public string updated_at; }
[Serializable] public class PutResponse { public bool success; public int version; public string updated_at; public string error; public CloudMeta cloud; }

[Serializable] public class SlotInfo { public int slot; public int version; public string scene; public int playtime_seconds; public string updated_at; }
[Serializable] public class SlotList { public SlotInfo[] items; }   // JsonUtility no lee arrays "sueltos": se envuelven

[Serializable] public class GetResponse { public int slot; public SaveData data; public int version; public string scene; public int playtime_seconds; public string updated_at; public string error; }

[Serializable] public class PlaytimeRequest { public int seconds; }
