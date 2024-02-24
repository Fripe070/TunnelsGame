#region

using System;
using System.Diagnostics.CodeAnalysis;
using Interactions;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

#endregion

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
	private void OnValidate()
	{
		// Ensure a child with a renderer exists
		if (GetComponentInChildren<Renderer>() is null)
			Debug.LogError("PlayerController requires a child with a renderer");
	}

	#region Fields
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float WalkSpeed = 4.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 6.0f;
	[Tooltip("Speed of the character when exhausted in m/s")]
	public float ExhaustedSpeed = 2.0f;
	public Vector3 spawnPosition;
	
	[Tooltip("Rotation speed of the character")]
	public float RotationSpeed = 1.0f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;
    
	[Space(10)]
	public float MaxHealth = 100f;
	public float health;
    
	[Space(10)]
	[Tooltip("How much stamina should be drained every second of sprinting")]
	public float StaminaDrainRate = 0.1f;
	[Tooltip("How much stamina should be regenerated every second of not springing")]
	public float StaminaGainRate = 0.1f;
	[Tooltip("How much slower the player should regain stamina when exhausted")]
	public float ExhaustedRegenMultiplier = 0.8f;
	public Color normalStaminaColor = Color.green;
	public Color exhaustedStaminaColor = Color.grey;

	[Space(10)]
	[Tooltip("The height the player can jump")]
	public float JumpHeight = 1.2f;
	[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
	public float Gravity = -15.0f;

	[Space(10)]
	[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
	public float JumpTimeout = 0.1f;
	[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
	public float FallTimeout = 0.15f;

	[Header("Player Grounded")]
	[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
	public bool Grounded = true;
	[Tooltip("Useful for rough ground")]
	public float GroundedOffset = -0.14f;
	[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
	public float GroundedRadius = 0.5f;
	[Tooltip("What layers the character uses as ground")]
	public LayerMask GroundLayers;

	[Header("Cinemachine")]
	[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
	public GameObject CinemachineCameraTarget;
	[Tooltip("How far in degrees can you move the camera up")]
	public float TopClamp = 90.0f;
	[Tooltip("How far in degrees can you move the camera down")]
	public float BottomClamp = -90.0f;

	[Header("Objects")]
	public AudioSource playerAudioSource;
	public AudioSource footstepSource;
	public Light flashlight;
	public NetworkVariable<bool> flashlightEnabled = new( false, 
		NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner );
	
	private Slider _staminaSlider;
	private Image _staminaFill;
	private Slider _healthSlider;
	private TextMeshProUGUI _interactionText;

	[Header("Interactions")] 
	public float interactionDistance;
	public float interactionRadius = 0.1f;
	public LayerMask interactionLayerMask;

	// cinemachine
	private float _cinemachineTargetPitch;

	// player
	private float _speed;
	private float _rotationVelocity;
	private float _verticalVelocity;
	private const float TerminalVelocity = 53.0f;
	private bool _interacted;

	// stamina
	private float _stamina = 1.0f;
	private bool _exhausted;
    
	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;

	public bool noclip;
    
	private CharacterController _controller;
	private TextMeshPro _nametag;
	#endregion
    
    private void Awake()
    {
        if (_healthSlider == null) _healthSlider = GameObject.Find("HealthBar").GetComponent<Slider>();
        if (_staminaSlider == null) _staminaSlider = GameObject.Find("StaminaBar").GetComponent<Slider>();
        if (_interactionText == null) _interactionText = GameObject.Find("Interaction Text").GetComponent<TextMeshProUGUI>();
        if (_nametag == null) _nametag = GameObject.Find("Nametag").GetComponent<TextMeshPro>();
    }

    private void Start()
    {
        _controller = GetComponent<CharacterController>();
        _staminaFill = _staminaSlider.fillRect.GetComponent<Image>();

        // reset our timeouts on start
        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
        
        health = MaxHealth;
        
        // Disable other players camera
        if (!IsOwner) CinemachineCameraTarget.GetComponentInChildren<Camera>().gameObject.SetActive(false);
        
        RandomiseHue();
        
#if UNITY_EDITOR
        var initialSpawn = GameObject.FindGameObjectWithTag("Spawnpoint").transform.position;
        if (IsOwner) transform.position = initialSpawn;
        spawnPosition = initialSpawn;
#endif
	    // _nametag.text = NetworkManager.Singleton.LocalClient.ClientId == NetworkObjectId ? "You" : "Player " + NetworkObjectId;
    }

    public override void OnNetworkSpawn()
    {
	    flashlightEnabled.OnValueChanged += (_, newValue) => flashlight.enabled = newValue;
    }
    

    private void RandomiseHue()
    {
	    // TODO: Ensure the color is the same on all clients
	    // TODO: either by sending an RPC or by using a seed constant for the client
	    var material = gameObject.GetComponentInChildren<Renderer>().material;
	    
	    float h, s, v;
	    Color.RGBToHSV(material.color, out h, out s, out v);
	    UnityEngine.Random.InitState(NetworkObjectId.GetHashCode());
	    h += UnityEngine.Random.value;
	    h %= 1;
	    material.color = Color.HSVToRGB(h, s, v);
    }

    private Vector2 _move;
    private Vector2 _look;
    
    private void Update()
    {
	    if (!IsOwner) return;
	    
	    _move = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
	    Cursor.lockState = CursorLockMode.Locked;
	    
	    if (Input.GetKeyDown(KeyCode.F)) flashlightEnabled.Value = !flashlightEnabled.Value;
	    
	    if (noclip) {
		    Noclip();
		    return;
	    }
        JumpAndGravity();
        GroundedCheck();
        StaminaUpdate();
        Move();
    }

    private void Noclip()
    {
	    Grounded = false;
	    _controller.enabled = false;
	    
	    // Relative to camera forward
	    Vector3 moveDirection = CinemachineCameraTarget.transform.forward * _move.y + CinemachineCameraTarget.transform.right * _move.x;
	    moveDirection = moveDirection.normalized;
	    moveDirection *= Input.GetKey(KeyCode.LeftShift) ? SprintSpeed : WalkSpeed;
	    
	    transform.position += moveDirection * WalkSpeed * Time.deltaTime;
	    _controller.enabled = true;
#if UNITY_EDITOR
	    Debug.DrawRay(transform.position, moveDirection * 3, Color.yellow);
#endif
    }

    private void LateUpdate()
    {
	    if (!IsOwner) return;
        CameraRotation();
        Interact();  // We need the camera rotation here
        
        HealthUpdate();
    }
    
    private void HealthUpdate()
	{
		health = Mathf.Max(0, health);
		_healthSlider.value = health / MaxHealth;
		if (health <= 0) Die();
	}

    private void GroundedCheck()
    {
        // set sphere position, with offset
        Vector3 position = transform.position;
        var spherePosition = new Vector3(position.x, position.y - GroundedOffset, position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
    }

    private void CameraRotation()
    {
	    _look = new Vector2(Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"));
	    
        if (_look.sqrMagnitude < 0.01f) return;

        _cinemachineTargetPitch += _look.y * RotationSpeed;
        _rotationVelocity = _look.x * RotationSpeed;

        // clamp our pitch rotation
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

        // Update Cinemachine camera target pitch
        CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

        // rotate the player left and right
        transform.Rotate(Vector3.up * _rotationVelocity);
    }

    private void StaminaUpdate()
    {
        _stamina = Mathf.Clamp01(_stamina);
        _staminaSlider.value = _stamina;
        // staminaSlider.gameObject.SetActive(_stamina < 1);

        if (_exhausted && _stamina >= 1) _exhausted = false;

        _staminaFill.color = _exhausted ? exhaustedStaminaColor : normalStaminaColor;

        if (_stamina <= 0) _exhausted = true;

        if (!CanSprint()) _stamina += Time.deltaTime * StaminaGainRate;
        else if (_exhausted) _stamina += Time.deltaTime * StaminaGainRate * ExhaustedRegenMultiplier;
    }

    private bool CanSprint()
    {
	    return Input.GetKey(KeyCode.LeftShift) && _move != Vector2.zero && !_exhausted;
    }

    private void Move()
    {
        var targetSpeed = WalkSpeed;
        if (_exhausted)
        {
            targetSpeed = ExhaustedSpeed;
        }
        else if (CanSprint())
        {
            _stamina -= Time.deltaTime * StaminaDrainRate;
            targetSpeed = SprintSpeed;
        }

        // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon
        // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is no input, set the target speed to 0
        if (_move == Vector2.zero) targetSpeed = 0.0f;

        // a reference to the players current horizontal velocity
        Vector3 velocity = _controller.velocity;
        var currentHorizontalSpeed = new Vector3(velocity.x, 0.0f, velocity.z).magnitude;

        const float speedOffset = 0.1f;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            // creates curved result rather than a linear one giving a more organic speed change
            // note T in Lerp is clamped, so we don't need to clamp our speed
            // TODO: Multiplying by amplitude makes strafing faster?
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * _move.magnitude, Time.deltaTime * SpeedChangeRate);

            // round speed to 3 decimal places
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        // normalise input direction
        Vector3 inputDirection = new Vector3(_move.x, 0.0f, _move.y).normalized;

        // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        // if there is a move input rotate player when the player is moving
        if (_move != Vector2.zero)
        {
            // move
            Transform transform1 = transform;
            inputDirection = transform1.right * _move.x + transform1.forward * _move.y;
        }

        // Footsteps
        footstepSource.mute = !Grounded || _speed < 0.1f;
        footstepSource.pitch = Math.Max(1f, (_speed / WalkSpeed) * 0.8f);

        // move the player
        _controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            // reset the fall timeout timer
            _fallTimeoutDelta = FallTimeout;

            // stop our velocity dropping infinitely when grounded
            if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

            // Jump
            if (Input.GetKeyDown(KeyCode.Space) && _jumpTimeoutDelta <= 0.0f)
                // the square root of H * -2 * G = how much velocity needed to reach desired height
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            // jump timeout
            if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
        }

        // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
        if (_verticalVelocity < TerminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    // ReSharper disable Unity.PerformanceAnalysis
    public void Die()
    {
	    Debug.Log("Player died");
	    // Application.OpenURL("https://youtu.be/dQw4w9WgXcQ");
        
        health = MaxHealth;
        transform.position = spawnPosition;
    }
    
    [ClientRpc]
    public void DamageClientRpc(float dmg, ClientRpcParams rpcParams = default)
    {
	    health -= dmg;
    }

    private void Interact()
    {
        _interactionText.text = "";

        var hitColliders = new Collider[1];
        Vector3 cameraPosition = CinemachineCameraTarget.transform.position;
        Vector3 endPointPosition = cameraPosition + interactionDistance * CinemachineCameraTarget.transform.forward;

        Physics.OverlapCapsuleNonAlloc(
            cameraPosition,
            endPointPosition,
            interactionRadius,
            hitColliders,
            interactionLayerMask);
        
#if UNITY_EDITOR
	    // Debug.Log(hitColliders);
		Debug.DrawLine(cameraPosition, endPointPosition, Color.red);
#endif

	    IInteractive interactive = null;
	    foreach (var hitCollider in hitColliders)
	    {
		    if (hitCollider is null) continue;
		    interactive = hitCollider.GetComponent<IInteractive>();
		    if (interactive is null) continue;
		    
		    if (Input.GetKeyDown(KeyCode.E)) interactive.Interact(this);
	    }
	    
	    if (interactive is null) return;
		_interactionText.text = interactive.InteractionText;
    }
    
    public bool CanSeePosition(Vector3 pos, float width = 45f, int range = 60, float proximityAwareness = -1f)
    {
	    Vector3 playerPosition = transform.position;
	    Vector3 cameraPosition = CinemachineCameraTarget.transform.position;
	    
	    float distance = Vector3.Distance(playerPosition, pos);
	    bool inRange = distance < range;
	    bool isInFOV = Vector3.Angle(CinemachineCameraTarget.transform.forward, pos - cameraPosition) < width;
	    bool isVeryClose = distance < proximityAwareness;
	    
	    var allButPlayerLayer = ~LayerMask.GetMask("Character");
	    
	    bool collidersInTheWay = Physics.Linecast(cameraPosition, pos, allButPlayerLayer, QueryTriggerInteraction.Ignore);
	    
	    return inRange && (isInFOV || isVeryClose) && !collidersInTheWay;
    }

#if UNITY_EDITOR
    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")] // Idc. It's for debugging only
    private void OnDrawGizmosSelected()
    {
        Vector3 cameraPosition = CinemachineCameraTarget.transform.position;

        // Grounded check sphere
        var transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        var transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);
        if (Grounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
            GroundedRadius);

        Vector3 endPointPosition = cameraPosition + interactionDistance * CinemachineCameraTarget.transform.forward;
        DrawWireCapsule(cameraPosition, endPointPosition, interactionRadius, Color.red);
    }

    public static void DrawWireCapsule(Vector3 pos1, Vector3 pos2, float radius, Color color = default)
    {
        if (color != default) Handles.color = color;

        Vector3 forward = pos2 - pos1;
        Quaternion rot = Quaternion.LookRotation(forward);
        var pointOffset = radius / 2f;
        var length = forward.magnitude;
        var center2 = new Vector3(0f, 0, length);
        Matrix4x4 angleMatrix = Matrix4x4.TRS(pos1, rot, Handles.matrix.lossyScale);

        using (new Handles.DrawingScope(angleMatrix))
        {
            Handles.DrawWireDisc(Vector3.zero, Vector3.forward, radius);
            Handles.DrawWireArc(Vector3.zero, Vector3.up, Vector3.left * pointOffset, -180f, radius);
            Handles.DrawWireArc(Vector3.zero, Vector3.left, Vector3.down * pointOffset, -180f, radius);
            Handles.DrawWireDisc(center2, Vector3.forward, radius);
            Handles.DrawWireArc(center2, Vector3.up, Vector3.right * pointOffset, -180f, radius);
            Handles.DrawWireArc(center2, Vector3.left, Vector3.up * pointOffset, -180f, radius);

            DrawLine(radius, 0f, length);
            DrawLine(-radius, 0f, length);
            DrawLine(0f, radius, length);
            DrawLine(0f, -radius, length);
        }
    }

    private static void DrawLine(float arg1, float arg2, float forward)
    {
        Handles.DrawLine(new Vector3(arg1, arg2, 0f), new Vector3(arg1, arg2, forward));
    }
#endif
}