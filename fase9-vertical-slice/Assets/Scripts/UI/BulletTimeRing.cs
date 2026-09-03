using UnityEngine;
using UnityEngine.UI;

// Ring visual alrededor del jugador que aparece durante bullet-time.
// Poner en un Canvas WorldSpace hijo del jugador, o en Screen Space con
// posición calculada por Camera.WorldToScreenPoint.
public class BulletTimeRing : MonoBehaviour
{
    [SerializeField] private Image _ring;
    [SerializeField] private float _scaleIn = 10f;
    [SerializeField] private float _scaleOut = 15f;
    [SerializeField] private Color _ringColor = new Color(0.227f, 1f, 0.831f, 0.6f); // #3AFFD4

    private float _targetAlpha;
    private float _currentAlpha;

    private void Start()
    {
        if (_ring) _ring.color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, 0f);
        Services.Get<InputReader>().OnBulletTimeChanged += OnBT;
    }

    private void OnDestroy()
    {
        if (Services.TryGet<InputReader>(out var ir))
            ir.OnBulletTimeChanged -= OnBT;
    }

    private void OnBT(bool active) => _targetAlpha = active ? _ringColor.a : 0f;

    private void Update()
    {
        if (_ring == null) return;
        float speed = _targetAlpha > _currentAlpha ? _scaleIn : _scaleOut;
        _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, speed * Time.deltaTime);
        _ring.color = new Color(_ringColor.r, _ringColor.g, _ringColor.b, _currentAlpha);
    }
}
