using UnityEngine;

/// <summary>
/// Ceiling charge → fall → impact/ripple loop. Pulses the burn puddle on hit.
/// </summary>
public class HolyWaterDripFx : MonoBehaviour
{
    [SerializeField] Vector3 spawnLocal = Vector3.zero;
    [SerializeField] float fallDistance = 4f;
    [SerializeField] float fallSpeed = 2.8f;
    [SerializeField] float fallAccel = 4.5f;
    [SerializeField] float intervalMin = 1.15f;
    [SerializeField] float intervalMax = 1.65f;
    [SerializeField] float chargeDuration = 0.32f;
    [SerializeField] float dripWorldHeight = 0.42f;
    [SerializeField] Color dripColor = Color.white;

    Sprite[] _frames;
    Sprite _impact;
    Sprite _ripple;
    Material _material;
    SpriteRenderer _puddle;
    Vector3 _puddleBaseScale = Vector3.one;
    Color _puddleBaseColor = Color.white;
    float _timer;
    float _puddlePulse;

    public void Configure(
        Vector3 localSpawn,
        float distance,
        Sprite[] dripFrames,
        Sprite impact = null,
        Sprite ripple = null,
        Material material = null,
        float worldHeight = 0.42f,
        SpriteRenderer puddle = null,
        float intervalLo = 1.15f,
        float intervalHi = 1.65f)
    {
        spawnLocal = localSpawn;
        fallDistance = distance;
        dripWorldHeight = worldHeight;
        dripColor = Color.white;
        intervalMin = intervalLo;
        intervalMax = Mathf.Max(intervalLo, intervalHi);
        RebindArt(dripFrames, impact, ripple, material, puddle);
    }

    /// <summary>
    /// Re-attach sprites after domain reload without moving drip spacing / spawn.
    /// </summary>
    public void RebindArt(
        Sprite[] dripFrames,
        Sprite impact = null,
        Sprite ripple = null,
        Material material = null,
        SpriteRenderer puddle = null)
    {
        _frames = dripFrames;
        _impact = impact;
        _ripple = ripple;
        _material = material;
        _puddle = puddle;
        if (_puddle != null)
        {
            _puddleBaseScale = _puddle.transform.localScale;
            _puddleBaseColor = _puddle.color;
        }
    }

    void OnEnable()
    {
        _timer = 0.35f;
        _puddlePulse = 0f;
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        TickPuddlePulse();

        _timer -= Time.deltaTime;
        if (_timer > 0f)
            return;

        _timer = Random.Range(intervalMin, intervalMax);
        SpawnChargingDroplet();
    }

    void TickPuddlePulse()
    {
        if (_puddle == null || _puddlePulse <= 0f)
            return;

        _puddlePulse -= Time.deltaTime;
        float u = 1f - Mathf.Clamp01(_puddlePulse / 0.22f);
        // Ease out: pop then settle.
        float pop = 1f + (1f - u) * (1f - u) * 0.12f;
        _puddle.transform.localScale = _puddleBaseScale * pop;

        var c = _puddleBaseColor;
        c.a = Mathf.Clamp01(_puddleBaseColor.a + (1f - u) * 0.2f);
        _puddle.color = c;

        if (_puddlePulse <= 0f)
        {
            _puddle.transform.localScale = _puddleBaseScale;
            _puddle.color = _puddleBaseColor;
        }
    }

    public void PulsePuddle()
    {
        if (_puddle == null)
            return;
        _puddlePulse = 0.22f;
    }

    void SpawnChargingDroplet()
    {
        if (_frames == null || _frames.Length == 0 || _frames[0] == null)
            return;

        var go = new GameObject("HolyWaterDrop");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = spawnLocal + new Vector3(Random.Range(-0.08f, 0.08f), 0f, 0f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _frames[0];
        sr.color = dripColor;
        sr.sortingLayerName = "Hazards";
        sr.sortingOrder = 4;
        if (_material != null)
            sr.sharedMaterial = _material;

        float srcH = Mathf.Max(0.01f, sr.sprite.bounds.size.y);
        float scale = dripWorldHeight / srcH;
        go.transform.localScale = Vector3.one * scale * 0.55f;

        var drop = go.AddComponent<FallingDrop>();
        drop.Init(
            this,
            fallSpeed,
            fallAccel,
            fallDistance,
            chargeDuration,
            scale,
            _frames,
            _impact,
            _ripple,
            _material);
    }

    sealed class FallingDrop : MonoBehaviour
    {
        HolyWaterDripFx _owner;
        float _speed;
        float _accel;
        float _travelled;
        float _max;
        float _chargeLeft;
        float _fullScale;
        Sprite[] _frames;
        Sprite _impact;
        Sprite _ripple;
        Material _material;
        SpriteRenderer _sr;
        bool _falling;

        public void Init(
            HolyWaterDripFx owner,
            float speed,
            float accel,
            float maxDistance,
            float chargeTime,
            float fullScale,
            Sprite[] frames,
            Sprite impact,
            Sprite ripple,
            Material material)
        {
            _owner = owner;
            _speed = speed;
            _accel = accel;
            _max = maxDistance;
            _chargeLeft = chargeTime;
            _fullScale = fullScale;
            _frames = frames;
            _impact = impact;
            _ripple = ripple;
            _material = material;
            _sr = GetComponent<SpriteRenderer>();
            _falling = false;
        }

        void Update()
        {
            if (!_falling)
            {
                _chargeLeft -= Time.deltaTime;
                float grow = 1f - Mathf.Clamp01(_chargeLeft / Mathf.Max(0.01f, _owner.chargeDuration));
                // Ease-in swell at the ceiling seam.
                float s = Mathf.Lerp(0.55f, 1f, grow * grow);
                transform.localScale = Vector3.one * (_fullScale * s);

                if (_sr != null)
                {
                    var c = _sr.color;
                    c.a = 0.45f + grow * 0.55f;
                    _sr.color = c;
                    if (_frames != null && _frames.Length > 0 && _frames[0] != null)
                        _sr.sprite = _frames[0];
                }

                if (_chargeLeft > 0f)
                    return;

                _falling = true;
                transform.localScale = Vector3.one * _fullScale;
                if (_sr != null)
                {
                    var c = _sr.color;
                    c.a = 1f;
                    _sr.color = c;
                }
            }

            _speed += _accel * Time.deltaTime;
            float step = _speed * Time.deltaTime;
            transform.localPosition += Vector3.down * step;
            _travelled += step;

            if (_sr != null && _frames != null && _frames.Length > 0)
            {
                float t = Mathf.Clamp01(_travelled / Mathf.Max(0.01f, _max));
                int idx = Mathf.Min(_frames.Length - 1, Mathf.FloorToInt(t * _frames.Length));
                if (_frames[idx] != null)
                    _sr.sprite = _frames[idx];

                // Slight stretch near impact.
                float stretch = 1f + t * 0.18f;
                transform.localScale = new Vector3(_fullScale / stretch, _fullScale * stretch, 1f);

                var c = _sr.color;
                c.a = 1f - t * 0.2f;
                _sr.color = c;
            }

            if (_travelled >= _max)
            {
                SpawnImpact();
                _owner?.PulsePuddle();
                Destroy(gameObject);
            }
        }

        void SpawnImpact()
        {
            Vector3 hitPos = transform.position;

            if (_impact != null)
                SpawnFxSprite("HolyWaterImpact", _impact, hitPos, 0.38f, 3, 0.32f);

            if (_ripple != null)
                SpawnFxSprite("HolyWaterRipple", _ripple, hitPos + new Vector3(0f, -0.02f, 0f), 0.72f, 2, 0.45f, grow: true);
        }

        void SpawnFxSprite(string name, Sprite sprite, Vector3 worldPos, float worldHeight, int order, float duration, bool grow = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform.parent, false);
            go.transform.position = worldPos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = "Hazards";
            sr.sortingOrder = order;
            if (_material != null)
                sr.sharedMaterial = _material;

            float srcH = Mathf.Max(0.01f, sprite.bounds.size.y);
            float scale = worldHeight / srcH;
            go.transform.localScale = Vector3.one * (grow ? scale * 0.65f : scale);

            var fade = go.AddComponent<FadeAndKill>();
            fade.duration = duration;
            fade.growTo = grow ? scale * 1.35f : 0f;
        }
    }

    sealed class FadeAndKill : MonoBehaviour
    {
        public float duration = 0.35f;
        public float growTo;
        float _t;
        float _startScale;
        SpriteRenderer _sr;
        Color _base;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
                _base = _sr.color;
            _startScale = transform.localScale.x;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float u = Mathf.Clamp01(_t / Mathf.Max(0.01f, duration));
            if (_sr != null)
            {
                var c = _base;
                c.a = _base.a * (1f - u);
                _sr.color = c;
            }

            if (growTo > 0.01f)
            {
                float s = Mathf.Lerp(_startScale, growTo, u);
                transform.localScale = Vector3.one * s;
            }

            if (u >= 1f)
                Destroy(gameObject);
        }
    }
}
