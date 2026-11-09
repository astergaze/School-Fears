using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton persistente. Guarda estado entre cargas de escena (spawns, flags de notas levantadas).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [HideInInspector] public string pendingSpawnPointId;

    // IDs de las notas que el jugador ya recogió (evita que reaparezcan al volver al aula)
    private HashSet<string> collectedNoteIds = new HashSet<string>();

    private HashSet<string> flags = new HashSet<string>();
    public void SetFlag(string id) { if (!string.IsNullOrEmpty(id)) flags.Add(id); }
    public bool HasFlag(string id) => !string.IsNullOrEmpty(id) && flags.Contains(id);
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterNoteCollected(string noteId)
    {
        if (!string.IsNullOrEmpty(noteId))
            collectedNoteIds.Add(noteId);
    }

    public bool IsNoteCollected(string noteId)
    {
        return !string.IsNullOrEmpty(noteId) && collectedNoteIds.Contains(noteId);
    }

    public int CollectedNotesCount => collectedNoteIds.Count;

    public void ResetProgress()
    {
        collectedNoteIds.Clear();
        pendingSpawnPointId = null;
        flags.Clear();
    }
}