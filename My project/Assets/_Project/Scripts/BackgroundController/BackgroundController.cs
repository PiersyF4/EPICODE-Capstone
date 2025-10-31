using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundController : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        [Tooltip("Transform del layer (l'oggetto che contiene lo SpriteRenderer della tua immagine).")]
        public Transform layer;

        [Tooltip("Fattore di parallasse per X e Y. 0 = resta fermo, 1 = si muove come la camera.")]
        public Vector2 parallaxMultiplier = new Vector2(0.5f, 0.0f);

        [Tooltip("Abilita lo scorrimento infinito orizzontale (richiede sprite affiancabili).")]
        public bool repeatX = false;

        [Tooltip("Abilita lo scorrimento infinito verticale (richiede sprite affiancabili).")]
        public bool repeatY = false;

        [HideInInspector] public Vector3 initialPosition;
        [HideInInspector] public float spriteWidth;
        [HideInInspector] public float spriteHeight;
    }

    [Tooltip("Camera da seguire. Se vuoto, verrà usata Camera.main.")]
    public Camera cam;

    [Tooltip("Configura qui i tuoi layer di sfondo (aggiungi le 3 immagini qui).")]
    public List<ParallaxLayer> layers = new List<ParallaxLayer>();

    private Vector3 _prevCamPos;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        if (cam != null)
            _prevCamPos = cam.transform.position;

        // Inizializza info dei layer (dimensioni sprite e posizione iniziale)
        for (int i = 0; i < layers.Count; i++)
        {
            var l = layers[i];
            if (l.layer == null) continue;

            l.initialPosition = l.layer.position;

            // Prova a leggere le dimensioni dal Renderer (preferibilmente SpriteRenderer)
            float w = 0f, h = 0f;
            var sr = l.layer.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                var size = sr.bounds.size;
                w = size.x;
                h = size.y;
            }
            else
            {
                var r = l.layer.GetComponent<Renderer>();
                if (r != null)
                {
                    var size = r.bounds.size;
                    w = size.x;
                    h = size.y;
                }
            }

            l.spriteWidth = w;
            l.spriteHeight = h;
        }
    }

    void FixedUpdate()
    {
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        Vector3 delta = camPos - _prevCamPos;

        for (int i = 0; i < layers.Count; i++)
        {
            var l = layers[i];
            if (l.layer == null) continue;

            // Movimento di parallasse in funzione dello spostamento camera
            Vector3 move = new Vector3(delta.x * l.parallaxMultiplier.x, delta.y * l.parallaxMultiplier.y, 0f);
            l.layer.position += move;

            // Scorrimento infinito orizzontale (se abilitato e dimensione nota)
            if (l.repeatX && l.spriteWidth > 0f)
            {
                float camToLayerX = camPos.x - l.layer.position.x;
                if (Mathf.Abs(camToLayerX) >= l.spriteWidth)
                {
                    float offsetX = Mathf.Floor(camToLayerX / l.spriteWidth) * l.spriteWidth;
                    l.layer.position += new Vector3(offsetX, 0f, 0f);
                }
            }

            // Scorrimento infinito verticale (se abilitato e dimensione nota)
            if (l.repeatY && l.spriteHeight > 0f)
            {
                float camToLayerY = camPos.y - l.layer.position.y;
                if (Mathf.Abs(camToLayerY) >= l.spriteHeight)
                {
                    float offsetY = Mathf.Floor(camToLayerY / l.spriteHeight) * l.spriteHeight;
                    l.layer.position += new Vector3(0f, offsetY, 0f);
                }
            }
        }

        _prevCamPos = camPos;
    }
}
