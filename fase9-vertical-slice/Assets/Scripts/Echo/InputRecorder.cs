using UnityEngine;

// Graba posiciones del jugador a 24fps en un ring buffer.
// EchoManager llama GetRecording() al final de cada loop.
[RequireComponent(typeof(PlayerController))]
public class InputRecorder : MonoBehaviour
{
    private const int SAMPLE_RATE = 24;
    private const float MAX_DURATION = 10f;
    private const int BUFFER_SIZE = (int)(SAMPLE_RATE * MAX_DURATION); // 240 frames

    [System.Serializable]
    public struct Snapshot
    {
        public Vector2 position;
        public bool facingRight;
        public PlayerController.PlayerState state;
    }

    private Snapshot[] _buffer = new Snapshot[BUFFER_SIZE];
    private int _writeIndex;
    private float _sampleTimer;
    private PlayerController _player;

    private void Awake() => _player = GetComponent<PlayerController>();

    private void Update()
    {
        _sampleTimer += Time.deltaTime;
        if (_sampleTimer < 1f / SAMPLE_RATE) return;
        _sampleTimer -= 1f / SAMPLE_RATE;

        _buffer[_writeIndex % BUFFER_SIZE] = new Snapshot
        {
            position = transform.position,
            facingRight = _player.FacingRight,
            state = _player.CurrentState
        };
        _writeIndex++;
    }

    // Devuelve los últimos 'duration' segundos de grabación.
    public Snapshot[] GetRecording(float duration)
    {
        int frames = Mathf.Clamp((int)(duration * SAMPLE_RATE), 1, BUFFER_SIZE);
        var result = new Snapshot[frames];
        for (int i = 0; i < frames; i++)
        {
            int idx = (_writeIndex - frames + i + BUFFER_SIZE * 2) % BUFFER_SIZE;
            result[i] = _buffer[idx];
        }
        return result;
    }

    public void ResetBuffer()
    {
        _writeIndex = 0;
        _sampleTimer = 0f;
        System.Array.Clear(_buffer, 0, _buffer.Length);
    }
}
