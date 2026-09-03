using UnityEngine;

// Temporary on-screen diagnostic overlay for the vertical slice bring-up.
// Not part of the PHASE design — remove once gameplay is confirmed working.
public class DebugHUD : MonoBehaviour
{
    public Transform player;
    public PlayerController controller;
    public Rigidbody2D playerRb;
    public Collider2D groundCollider;
    public LayerMask groundMask;

    private int _frameCount;
    private bool _visible = false;

    private void Update()
    {
        _frameCount++;
        if (Input.GetKeyDown(KeyCode.F1)) _visible = !_visible;
    }

    private void OnGUI()
    {
        if (!_visible) return;
        GUI.skin.label.fontSize = 22;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.normal.textColor = Color.yellow;

        string posText = player != null ? player.position.ToString("F3") : "player=NULL";
        string rbText = playerRb != null ? playerRb.position.ToString("F3") : "rb=NULL";
        string stateText = controller != null ? controller.CurrentState.ToString() : "controller=NULL";
        string facingText = controller != null ? controller.FacingRight.ToString() : "?";

        // Physics diagnostics: is there ANY collider geometry actually present where the floor should be?
        Collider2D hitAtFloor = Physics2D.OverlapPoint(new Vector2(1f, 0.5f), groundMask);
        Collider2D hitAtFloorAnyLayer = Physics2D.OverlapPoint(new Vector2(1f, 0.5f));
        string boundsText = groundCollider != null ? groundCollider.bounds.ToString() : "groundCollider=NULL";
        string enabledText = groundCollider != null ? groundCollider.enabled.ToString() : "?";
        string compositePathCount = "";
        if (groundCollider is CompositeCollider2D cc) compositePathCount = $" pathCount={cc.pathCount}";

        GUI.Box(new Rect(5, 5, 700, 230), "");
        GUI.Label(new Rect(15, 10, 680, 24), $"frame={_frameCount} t={Time.time:F2} dt={Time.deltaTime:F4}", style);
        GUI.Label(new Rect(15, 34, 680, 24), $"player.pos={posText}", style);
        GUI.Label(new Rect(15, 58, 680, 24), $"rb.pos={rbText}", style);
        GUI.Label(new Rect(15, 82, 680, 24), $"state={stateText} facing={facingText}", style);
        GUI.Label(new Rect(15, 106, 680, 24), $"timeScale={Time.timeScale} fixedDT={Time.fixedDeltaTime}", style);
        GUI.Label(new Rect(15, 130, 680, 24), $"OverlapPoint(1,0.5,groundMask)={(hitAtFloor != null ? hitAtFloor.name : "NULL")}", style);
        GUI.Label(new Rect(15, 154, 680, 24), $"OverlapPoint(1,0.5,anyLayer)={(hitAtFloorAnyLayer != null ? hitAtFloorAnyLayer.name + " layer=" + hitAtFloorAnyLayer.gameObject.layer : "NULL")}", style);
        GUI.Label(new Rect(15, 178, 680, 24), $"groundCollider bounds={boundsText} enabled={enabledText}{compositePathCount}", style);
        GUI.Label(new Rect(15, 202, 680, 24), $"groundMask={groundMask.value}", style);
    }
}
