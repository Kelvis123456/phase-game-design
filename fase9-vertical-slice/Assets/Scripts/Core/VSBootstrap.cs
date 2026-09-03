using UnityEngine;

// Registra todos los servicios del VS en orden correcto.
// Poner este componente en el GameObject "Bootstrap" de la escena.
// Script Execution Order: -100 (antes que todo lo demás)
[DefaultExecutionOrder(-100)]
public class VSBootstrap : MonoBehaviour
{
    [Header("Referenciar los GameObjects que tienen los servicios")]
    [SerializeField] private TimeManager timeManager;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private VFXPool vfxPool;

    // El ground tilemap usa un CompositeCollider2D construido por VSSceneBuilder (editor-time).
    // GenerateGeometry() llamado en tiempo de edición no sobrevive de forma fiable a la
    // serialización de la escena hacia un build standalone (el collider carga con bounds
    // vacíos — confirmado con OverlapPoint devolviendo null en runtime).
    [SerializeField] private CompositeCollider2D groundComposite;

    private void Awake()
    {
        // Limpiar registros anteriores (útil al hacer Play desde Editor)
        Services.Clear();

        // Registrar en orden: primero los que no dependen de nada
        Services.Register(timeManager);
        Services.Register(inputReader);
        Services.Register(vfxPool);

        // PlayerController, EchoManager, etc. se registran en sus propios Awake()
        // porque viven en la escena junto al nivel, no aquí

        SetupEchoCollisionMatrix();
    }

    // Regenerar aquí, no en Awake(): Unity garantiza que TODOS los Awake() de la escena
    // (incluyendo el de TilemapCollider2D, que popula su geometría de tiles) terminan
    // antes de que CUALQUIER Start() corra — sin importar el execution order individual.
    // VSBootstrap tiene order -100 (el más temprano), así que si esto se llamara en su
    // Awake(), TilemapCollider2D todavía no habría calculado la forma que el composite
    // necesita fusionar, y el bake seguiría produciendo geometría vacía.
    private void Start()
    {
        if (groundComposite != null)
            groundComposite.GenerateGeometry();
    }

    // Fase 8 §17.4: los ecos son cinemáticos (sin física real) — no colisionan con
    // nada del mundo salvo sus propios Trigger Points, que se resuelven por código,
    // no por colisión física. Se aplica en runtime en vez de editar el asset de
    // Physics2D directamente, para no arriesgar corromper el bitmask serializado.
    private void SetupEchoCollisionMatrix()
    {
        int echo = LayerMask.NameToLayer("Echo");
        int ground = LayerMask.NameToLayer("Ground");
        int player = LayerMask.NameToLayer("Player");
        int hazard = LayerMask.NameToLayer("Hazard");
        int platform = LayerMask.NameToLayer("Platform");

        Physics2D.IgnoreLayerCollision(echo, ground, true);
        Physics2D.IgnoreLayerCollision(echo, player, true);
        Physics2D.IgnoreLayerCollision(echo, hazard, true);
        Physics2D.IgnoreLayerCollision(echo, platform, true);
        Physics2D.IgnoreLayerCollision(echo, echo, true);
    }
}
