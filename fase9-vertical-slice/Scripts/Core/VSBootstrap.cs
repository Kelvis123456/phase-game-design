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
    }
}
