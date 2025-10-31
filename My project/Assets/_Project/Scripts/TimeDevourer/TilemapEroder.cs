using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapEroder : MonoBehaviour
{
    [Header("Tilemap da erodere (in ordine: collisioni, decor, ecc.)")]
    public Tilemap[] tilemaps;

    [Header("Margini e limiti")]
    public int safetyMarginColumns = 2;     // lascia qualche colonna dietro il fronte
    public int maxColumnsPerFrame = 2;      // per non freezare
    public int minY = -50;                  // limiti verticali in coordinate cella
    public int maxY = 50;

    [Header("Visual feedback (opzionale)")]
    public ParticleSystem crumbleFX;        // istanziato su ogni tile rimossa (facoltativo)

    int erodedUpToX;            // ultima colonna già mangiata
    bool initialized;
    Tilemap refMap;

    void Start()
    {
        if (tilemaps == null || tilemaps.Length == 0)
        {
            Debug.LogWarning("[TilemapEroder] Nessuna tilemap assegnata.");
            enabled = false;
            return;
        }

        refMap = tilemaps[0];
        // inizializza il contatore in base alla posizione iniziale del fronte
        int frontX = refMap.WorldToCell(transform.position).x;
        erodedUpToX = frontX - safetyMarginColumns - 1;

        // se non vuoi impostare minY/maxY a mano, usa i bounds della prima tilemap
        var bounds = refMap.cellBounds;
        if (minY == -50 && maxY == 50)
        {
            minY = bounds.yMin;
            maxY = bounds.yMax;
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        int frontX = refMap.WorldToCell(transform.position).x;
        int targetX = frontX - safetyMarginColumns;

        int processed = 0;
        while (erodedUpToX < targetX && processed < maxColumnsPerFrame)
        {
            ErodeColumn(erodedUpToX);
            erodedUpToX++;
            processed++;
        }
    }

    void ErodeColumn(int x)
    {
        for (int y = minY; y <= maxY; y++)
        {
            Vector3Int cell = new Vector3Int(x, y, 0);

            for (int i = 0; i < tilemaps.Length; i++)
            {
                var tm = tilemaps[i];
                if (!tm) continue;
                if (!tm.HasTile(cell)) continue;

                // FX prima di rimuovere
                if (crumbleFX)
                {
                    Vector3 pos = tm.GetCellCenterWorld(cell);
                    Instantiate(crumbleFX, pos, Quaternion.identity)?.Play();
                }

                tm.SetTile(cell, null);
            }
        }
    }

    // Gizmo: verde = ultima colonna erosa
    void OnDrawGizmos()
    {
        if (!refMap) return;
        Gizmos.color = Color.green;
        Vector3 a = refMap.CellToWorld(new Vector3Int(erodedUpToX, 0, 0)) + Vector3.up * 20f;
        Vector3 b = refMap.CellToWorld(new Vector3Int(erodedUpToX, 0, 0)) + Vector3.down * 20f;
        Gizmos.DrawLine(a, b);
    }
}
