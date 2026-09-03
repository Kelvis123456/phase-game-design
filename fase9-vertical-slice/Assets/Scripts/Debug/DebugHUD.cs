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
    private string _lastAction = "(none)";

    private void Update()
    {
        _frameCount++;
        if (Input.GetKeyDown(KeyCode.F1)) _visible = !_visible;

        if (Input.GetKeyDown(KeyCode.F2) && Services.TryGet<ProgressionSystem>(out var prog1))
        {
            prog1.EarnCrystals(ProgressionSystem.EarnSource.RunZone1);
            _lastAction = $"F2: EarnCrystals(RunZone1) -> balance={prog1.PhaseCrystalBalance}";
        }
        if (Input.GetKeyDown(KeyCode.F3) && Services.TryGet<ProgressionSystem>(out var prog2))
        {
            bool ok = prog2.TryUnlock("A2");
            _lastAction = $"F3: TryUnlock(A2) -> {ok} balance={prog2.PhaseCrystalBalance}";
        }
        if (Input.GetKeyDown(KeyCode.F4) && Services.TryGet<RunManager>(out var run1))
        {
            run1.StartRun();
            _lastAction = $"F4: StartRun() -> state={run1.CurrentState}";
        }
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

        string crystalsText = Services.TryGet<ProgressionSystem>(out var prog) ? prog.PhaseCrystalBalance.ToString() : "NULL";
        string a2Text = Services.TryGet<ProgressionSystem>(out var prog3) ? prog3.IsNodeUnlocked("A2").ToString() : "?";
        string runStateText = Services.TryGet<RunManager>(out var run) ? run.CurrentState.ToString() : "NULL";
        string echoActiveText = Services.TryGet<EchoManager>(out var em) ? em.ActiveCount.ToString() : "NULL";

        GUI.Box(new Rect(5, 5, 700, 300), "");
        GUI.Label(new Rect(15, 10, 680, 24), $"frame={_frameCount} t={Time.time:F2} dt={Time.deltaTime:F4}", style);
        GUI.Label(new Rect(15, 34, 680, 24), $"player.pos={posText}", style);
        GUI.Label(new Rect(15, 58, 680, 24), $"rb.pos={rbText}", style);
        GUI.Label(new Rect(15, 82, 680, 24), $"state={stateText} facing={facingText}", style);
        GUI.Label(new Rect(15, 106, 680, 24), $"timeScale={Time.timeScale} fixedDT={Time.fixedDeltaTime}", style);
        GUI.Label(new Rect(15, 130, 680, 24), $"OverlapPoint(1,0.5,groundMask)={(hitAtFloor != null ? hitAtFloor.name : "NULL")}", style);
        GUI.Label(new Rect(15, 154, 680, 24), $"OverlapPoint(1,0.5,anyLayer)={(hitAtFloorAnyLayer != null ? hitAtFloorAnyLayer.name + " layer=" + hitAtFloorAnyLayer.gameObject.layer : "NULL")}", style);
        GUI.Label(new Rect(15, 178, 680, 24), $"groundCollider bounds={boundsText} enabled={enabledText}{compositePathCount}", style);
        GUI.Label(new Rect(15, 202, 680, 24), $"groundMask={groundMask.value}", style);
        GUI.Label(new Rect(15, 226, 680, 24), $"PhaseCrystals={crystalsText}  A2unlocked={a2Text}  echoActive={echoActiveText}", style);
        GUI.Label(new Rect(15, 250, 680, 24), $"RunState={runStateText}", style);
        GUI.Label(new Rect(15, 274, 680, 24), $"[F2 earn][F3 unlock A2][F4 start run] last: {_lastAction}", style);
    }
}
