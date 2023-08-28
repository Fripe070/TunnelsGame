using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
	[Header("Player")]
	[Tooltip("Move speed of the character in m/s")]
	public float WalkSpeed = 4.0f;
	[Tooltip("Sprint speed of the character in m/s")]
	public float SprintSpeed = 6.0f;
	[Tooltip("Speed of the character when exhausted in m/s")]
	public float ExhaustedSpeed = 2.0f;
	
	[Tooltip("Rotation speed of the character")]
	public float RotationSpeed = 1.0f;
	[Tooltip("Acceleration and deceleration")]
	public float SpeedChangeRate = 10.0f;
    
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
	public AudioSource footstepSource;
	public Light flashlight;
	public Slider staminaSlider;
	private Image _staminaFill;

	// cinemachine
	private float _cinemachineTargetPitch;

	// player
	private float _speed;
	private float _rotationVelocity;
	private float _verticalVelocity;
	private float _terminalVelocity = 53.0f;
	
	// stamina
	private float _stamina = 1.0f;
	private bool _exhausted;
    
	// timeout deltatime
	private float _jumpTimeoutDelta;
	private float _fallTimeoutDelta;
    
	private CharacterController _controller;
	private StarterAssetsInputs _input;
	private GameObject _mainCamera;
	
	private void Awake()
	{
		// get a reference to our main camera
		if (_mainCamera == null)
		{
			_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}
	}

	private void Start()
	{
		_controller = GetComponent<CharacterController>();
		_input = GetComponent<StarterAssetsInputs>();
		_staminaFill = staminaSlider.fillRect.GetComponent<Image>();

		// reset our timeouts on start
		_jumpTimeoutDelta = JumpTimeout;
		_fallTimeoutDelta = FallTimeout;
	}

	private void Update()
	{
		JumpAndGravity();
		GroundedCheck();
		StaminaUpdate();
		Move();
        
		if (flashlight.enabled != _input.flashlight) flashlight.enabled = _input.flashlight;
	}

	private void LateUpdate()
	{
		CameraRotation();
	}

	private void GroundedCheck()
	{
		// set sphere position, with offset
		var position = transform.position;
		Vector3 spherePosition = new Vector3(position.x, position.y - GroundedOffset, position.z);
		Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
	}

	private void CameraRotation()
	{
		// if there is an input
		if (_input.look.sqrMagnitude >= 0.01f)
		{
			_cinemachineTargetPitch += _input.look.y * RotationSpeed;
			_rotationVelocity = _input.look.x * RotationSpeed;

			// clamp our pitch rotation
			_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

			// Update Cinemachine camera target pitch
			CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

			// rotate the player left and right
			transform.Rotate(Vector3.up * _rotationVelocity);
		}
	}

	private void StaminaUpdate()
	{
		_stamina = Mathf.Clamp01(_stamina);
		staminaSlider.value = _stamina;
		staminaSlider.gameObject.SetActive(true); // _stamina < 1

		if (_exhausted && _stamina >= 1) _exhausted = false;
		
		_staminaFill.color = _exhausted ? exhaustedStaminaColor : normalStaminaColor;
		
		if (_stamina <= 0) _exhausted = true;

		if (!CanSprint()) _stamina += Time.deltaTime * StaminaGainRate;
		else if (_exhausted) _stamina += Time.deltaTime * StaminaGainRate * ExhaustedRegenMultiplier;
	}

	private bool CanSprint()
	{
		return _input.sprint && _input.move != Vector2.zero && !_exhausted;
	}
    
	private void Move()
	{
		float targetSpeed = WalkSpeed;
		if (_exhausted) targetSpeed = ExhaustedSpeed;
		else if (CanSprint())
		{
			_stamina -= Time.deltaTime * StaminaDrainRate;
			targetSpeed = SprintSpeed;
		}

		// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon
		// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is no input, set the target speed to 0
		if (_input.move == Vector2.zero) targetSpeed = 0.0f;

		// a reference to the players current horizontal velocity
		var velocity = _controller.velocity;
		float currentHorizontalSpeed = new Vector3(velocity.x, 0.0f, velocity.z).magnitude;

		float speedOffset = 0.1f;
		float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

		// accelerate or decelerate to target speed
		if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
		{
			// creates curved result rather than a linear one giving a more organic speed change
			// note T in Lerp is clamped, so we don't need to clamp our speed
			_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);

			// round speed to 3 decimal places
			_speed = Mathf.Round(_speed * 1000f) / 1000f;
		}
		else
		{
			_speed = targetSpeed;
		}

		// normalise input direction
		Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

		// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
		// if there is a move input rotate player when the player is moving
		if (_input.move != Vector2.zero)
		{
			// move
			var transform1 = transform;
			inputDirection = transform1.right * _input.move.x + transform1.forward * _input.move.y;
		}
			
		// Footsteps
		footstepSource.enabled = _speed > 0.1f && Grounded;

		// move the player
		_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
	}

	private void JumpAndGravity()
	{
		if (Grounded)
		{
			// reset the fall timeout timer
			_fallTimeoutDelta = FallTimeout;

			// stop our velocity dropping infinitely when grounded
			if (_verticalVelocity < 0.0f)
			{
				_verticalVelocity = -2f;
			}

			// Jump
			if (_input.jump && _jumpTimeoutDelta <= 0.0f)
			{
				// the square root of H * -2 * G = how much velocity needed to reach desired height
				_verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
			}

			// jump timeout
			if (_jumpTimeoutDelta >= 0.0f)
			{
				_jumpTimeoutDelta -= Time.deltaTime;
			}
		}
		else
		{
			// reset the jump timeout timer
			_jumpTimeoutDelta = JumpTimeout;

			// fall timeout
			if (_fallTimeoutDelta >= 0.0f)
			{
				_fallTimeoutDelta -= Time.deltaTime;
			}

			// if we are not grounded, do not jump
			_input.jump = false;
		}

		// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
		if (_verticalVelocity < _terminalVelocity)
		{
			_verticalVelocity += Gravity * Time.deltaTime;
		}
	}

	private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
	{
		if (lfAngle < -360f) lfAngle += 360f;
		if (lfAngle > 360f) lfAngle -= 360f;
		return Mathf.Clamp(lfAngle, lfMin, lfMax);
	}

	public void Die()
	{
		Application.Quit();
	}
	
	private void OnDrawGizmosSelected()
	{
		Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
		Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

		if (Grounded) Gizmos.color = transparentGreen;
		else Gizmos.color = transparentRed;

		// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
		Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
	}
}