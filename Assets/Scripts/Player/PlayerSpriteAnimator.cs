using UnityEngine;

public class PlayerSpriteAnimator : MonoBehaviour
{
    [SerializeField] SpriteRenderer target;
    [SerializeField] Sprite[] idleFrames;
    [SerializeField] Sprite[] runFrames;
    [SerializeField] Sprite[] getupFrames;
    [SerializeField] float idleFps = 3f;
    [SerializeField] float runFps = 12f;
    [SerializeField] float getupFps = 6f;
    [SerializeField] float runEnterDelay = 0.05f;
    [SerializeField] float runExitDelay = 0.12f;
    [SerializeField] [Tooltip("Run-sheet frame held while airborne (0-based). For an 8-frame cycle, mid-stride hang is usually 3.")]
    int airHangFrame = 3;

    enum Pose
    {
        Idle,
        Run,
        Air,
        Getup
    }

    Pose _pose = Pose.Idle;
    int _frame;
    float _frameTime;
    float _runHold;
    float _idleHold;
    bool _grounded = true;
    bool _wasGrounded = true;
    bool _moving;

    public void Bind(SpriteRenderer spriteRenderer)
    {
        target = spriteRenderer;
    }

    public bool HasRunCycle => runFrames != null && runFrames.Length > 1;

    public bool HasGetup => Has(getupFrames);

    /// <summary>True while a one-shot getup (or similar) blocks locomotion.</summary>
    public bool IsBusy => _pose == Pose.Getup;

    public void ConfigureIdleTiming(float fps)
    {
        idleFps = Mathf.Max(0.1f, fps);
    }

    public void ConfigureGetupTiming(float fps)
    {
        getupFps = Mathf.Max(0.1f, fps);
    }

    public void SetFrames(Sprite[] idle, Sprite[] run)
    {
        idleFrames = idle;
        runFrames = run;
        if (_pose != Pose.Getup)
        {
            _pose = Pose.Idle;
            _frame = 0;
            _frameTime = 0f;
            _runHold = 0f;
            _idleHold = 0f;
            Apply();
        }
    }

    public void SetGetupFrames(Sprite[] getup)
    {
        getupFrames = getup;
    }

    /// <summary>Play getup once from frame 0. Returns false if no getup frames.</summary>
    public bool PlayGetup()
    {
        if (!Has(getupFrames))
            return false;

        _pose = Pose.Getup;
        _frame = 0;
        _frameTime = 0f;
        _runHold = 0f;
        _idleHold = 0f;
        Apply();
        return true;
    }

    public void SetLocomotion(bool grounded, bool moving)
    {
        if (_pose == Pose.Getup)
            return;
        _grounded = grounded;
        _moving = moving;
    }

    void Update()
    {
        if (_pose == Pose.Getup)
        {
            AdvanceGetup();
            return;
        }

        if (!_grounded)
        {
            if (_wasGrounded)
                EnterAir();
            _wasGrounded = false;
            _runHold = 0f;
            _idleHold = 0f;
            Advance();
            return;
        }

        if (!_wasGrounded)
            Land();
        _wasGrounded = true;

        bool wantRun = _moving && Has(runFrames);
        if (wantRun)
        {
            _runHold += Time.deltaTime;
            _idleHold = 0f;
        }
        else
        {
            _idleHold += Time.deltaTime;
            _runHold = 0f;
        }

        if (_pose != Pose.Run && wantRun && _runHold >= runEnterDelay)
            Switch(Pose.Run);
        else if (_pose == Pose.Run && !wantRun && _idleHold >= runExitDelay)
            Switch(Pose.Idle);

        Advance();
    }

    void EnterAir()
    {
        // Moving / mid-run jump: freeze a hang frame from the run sheet.
        // Standing jump from idle: keep idle (breathing continues).
        if (Has(runFrames) && (_pose == Pose.Run || _moving))
        {
            _pose = Pose.Air;
            _frame = ResolveAirHangFrame();
            _frameTime = 0f;
            Apply();
        }
    }

    void Land()
    {
        _runHold = 0f;
        _idleHold = 0f;
        if (_moving && Has(runFrames))
        {
            if (_pose != Pose.Run)
                Switch(Pose.Run);
            return;
        }

        if (_pose != Pose.Idle)
            Switch(Pose.Idle);
    }

    int ResolveAirHangFrame()
    {
        if (!Has(runFrames))
            return 0;
        if (runFrames.Length == 1)
            return 0;
        return Mathf.Clamp(airHangFrame, 0, runFrames.Length - 1);
    }

    void Switch(Pose next)
    {
        _pose = next;
        _frame = 0;
        _frameTime = 0f;
        Apply();
    }

    void AdvanceGetup()
    {
        if (!Has(getupFrames) || getupFps <= 0f)
        {
            FinishGetup();
            return;
        }

        _frameTime += Time.deltaTime * getupFps;
        if (_frameTime < 1f)
        {
            Apply();
            return;
        }

        int steps = Mathf.FloorToInt(_frameTime);
        _frameTime -= steps;
        _frame += steps;

        if (_frame >= getupFrames.Length)
        {
            FinishGetup();
            return;
        }

        Apply();
    }

    void FinishGetup()
    {
        // Land on idle without re-Apply flash if last getup cell already is idle_0.
        _pose = Pose.Idle;
        _frame = 0;
        _frameTime = 0f;
        _runHold = 0f;
        _idleHold = 0f;
        Apply();
    }

    void Advance()
    {
        if (_pose == Pose.Air)
            return;

        var frames = CurrentFrames();
        if (frames == null || frames.Length == 0)
            return;

        float fps = _pose == Pose.Run ? runFps : idleFps;
        if (fps <= 0f)
            return;

        _frameTime += Time.deltaTime * fps;
        if (_frameTime < 1f)
            return;

        int steps = Mathf.FloorToInt(_frameTime);
        _frameTime -= steps;
        _frame = (_frame + steps) % frames.Length;
        Apply();
    }

    Sprite[] CurrentFrames()
    {
        if (_pose == Pose.Getup && Has(getupFrames))
            return getupFrames;
        if ((_pose == Pose.Run || _pose == Pose.Air) && Has(runFrames))
            return runFrames;
        return idleFrames;
    }

    void Apply()
    {
        if (target == null)
            target = GetComponentInChildren<SpriteRenderer>();
        var frames = CurrentFrames();
        if (target == null || frames == null || frames.Length == 0)
            return;
        int index = Mathf.Clamp(_frame, 0, frames.Length - 1);
        if (frames[index] != null)
            target.sprite = frames[index];
    }

    static bool Has(Sprite[] frames)
    {
        return frames != null && frames.Length > 0 && frames[0] != null;
    }
}
