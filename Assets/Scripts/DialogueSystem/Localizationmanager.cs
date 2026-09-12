using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum Language { ES, EN }

/// <summary>
/// Carga un CSV (id;es;en) y expone el texto correspondiente al idioma activo.
/// El CSV va como TextAsset dentro de una carpeta "Resources" (ej: Resources/Localization/dialogue.csv).
/// Usa ';' como separador para no romperse con las comas normales del español/inglés.
/// Si un campo necesita tener ';' o saltos de línea, se lo puede envolver entre comillas dobles.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Header("CSV Source")]
    [Tooltip("TextAsset ubicado en Resources, ej: Resources/Localization/dialogue.csv")]
    [SerializeField] private TextAsset csvFile;

    [Header("Idioma")]
    [SerializeField] private Language currentLanguage = Language.ES;

    public event Action OnLanguageChanged;

    private readonly Dictionary<string, (string es, string en)> table = new Dictionary<string, (string, string)>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        ParseCsv();
    }

    private void ParseCsv()
    {
        table.Clear();

        if (csvFile == null)
        {
            Debug.LogError("LocalizationManager: no hay CSV asignado en el inspector.");
            return;
        }

        string[] rows = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // rows[0] es el header (id;es;en) y se ignora
        for (int i = 1; i < rows.Length; i++)
        {
            string[] columns = SplitCsvRow(rows[i]);
            if (columns.Length < 3) continue;

            string id = columns[0].Trim();
            if (string.IsNullOrEmpty(id)) continue;

            string es = columns[1].Trim().Replace("\\n", "\n");
            string en = columns[2].Trim().Replace("\\n", "\n");

            if (table.ContainsKey(id))
                Debug.LogWarning($"LocalizationManager: id duplicado '{id}' en la fila {i + 1}, se sobreescribe.");

            table[id] = (es, en);
        }
    }

    // Parser simple que respeta campos entre comillas dobles (soporta ';' y saltos de línea dentro del texto)
    private string[] SplitCsvRow(string row)
    {
        List<string> fields = new List<string>();
        bool inQuotes = false;
        StringBuilder current = new StringBuilder();

        for (int i = 0; i < row.Length; i++)
        {
            char c = row[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < row.Length && row[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // salta la comilla escapada
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ';' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }

    public string GetText(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";

        if (!table.TryGetValue(id, out var entry))
        {
            Debug.LogWarning($"LocalizationManager: id no encontrado '{id}'.");
            return $"[{id}]";
        }

        return currentLanguage == Language.ES ? entry.es : entry.en;
    }

    public void SetLanguage(Language lang)
    {
        if (currentLanguage == lang) return;
        currentLanguage = lang;
        OnLanguageChanged?.Invoke();
    }

    public Language CurrentLanguage => currentLanguage;
}