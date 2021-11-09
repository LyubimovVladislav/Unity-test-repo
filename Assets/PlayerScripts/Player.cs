using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// ReSharper disable UnusedMember.Global - It does not detect properly that the message is dropped to this class

namespace PlayerScripts
{
	public class Player : MonoBehaviour
	{
		[Header("Mouse movement")] [SerializeField]
		private float mouseSensitivity = 0.1f;

		[Header("Keyboard Movement")] [SerializeField]
		private float speed = 12f;

		[SerializeField] private float inverseSmoothingMultiplier = 5f;

		[Header("Ascending of speed values")]
		[Tooltip(
			"The amount of seconds whole initial acceleration takes. Possible values -> [0, inf]" +
			"Responsible for stretching and shrinking in following function (deltaTime/speed)^curve")]
		[SerializeField]
		private float accelerationTime = 0.5f;

		[Tooltip(
			"The amount of curvature of the function Possible values -> [0, inf]" +
			"Responsible for concavity in following function (deltaTime/speed)^curve" +
			"If attribute = 1, the function becomes linear")]
		[SerializeField]
		private float accelerationCurvePower = 0.2f;

		[Header("Descending of speed values")]
		[Tooltip(
			"The amount of seconds whole initial deceleration takes. Possible values -> [0, inf]" +
			"Responsible for stretching and shrinking in following function 1-(deltaTime/speed)^curve")]
		[SerializeField]
		private float decelerationTime = 0.5f;

		[Tooltip(
			"The amount of curvature of the function Possible values -> [0, inf]\n" +
			"Responsible for concavity in following function (deltaTime/speed)^curve 1-(deltaTime/speed)^curve" +
			"If attribute = 1, the function becomes linear")]
		[SerializeField]
		private float decelerationCurvePower = 0.2f;

		[Header("Gravity")] [SerializeField] private float gravitationalConstant = -9.81f;
		[Tooltip("Epsilon")] [SerializeField] private float groundDistance = 0.1f;
		[SerializeField] private LayerMask groundMask;
		[SerializeField] private Transform groundCheck;
		[SerializeField] private float jumpHeight = 3f;

		private float _currentAccelerationTime;
		private float _currentDecelerationTime;
		private float _yRotation;
		private float _xRotation;
		private Transform _cameraPosition;
		private Transform _bodyPosition;
		private CharacterController _controller;
		private bool _isMovementPressed;
		private Vector3 _normalizedMovement;
		private Vector3 _smoothMovement;
		private Vector3 _downwardsVelocity;
		private bool _isGrounded;
		private bool _isJumping;


		private void Start()
		{
			_isJumping = false;
			_isGrounded = false;
			_downwardsVelocity = Vector3.zero;
			_currentAccelerationTime = 0f;
			_currentDecelerationTime = 0f;
			_controller = GetComponent<CharacterController>();
			_cameraPosition = GetComponentInChildren<Camera>().transform;
			_bodyPosition = transform;
			_yRotation = 0f;
			_xRotation = 0f;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			_isMovementPressed = false;
		}

		private void Update()
		{
			if (_isMovementPressed || !_smoothMovement.Equals(Vector3.zero))
				Move();
			
			_isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
			
			// TODO: refactor this 'if' statement code into ApplyGravity() or another method
			// Level of abstraction does not match
			if (_isGrounded && _downwardsVelocity.y < 0)
			{
				_isJumping = false;
				_downwardsVelocity.y = 0;
			}
			else
			{
				ApplyGravity();
			}
		}

		private void ApplyGravity()
		{
			_downwardsVelocity.y += gravitationalConstant * Time.deltaTime;
			_controller.Move(_downwardsVelocity * Time.deltaTime);
		}


		private void Move()
		{
			CalculateMovementInputSmoothing();
			Vector3 result = _isMovementPressed
				? SpeedGradualAcceleration(_smoothMovement)
				: SpeedGradualDeceleration(_smoothMovement);
			_controller.Move(result * (speed * Time.deltaTime));
		}


		private Vector3 SpeedGradualAcceleration(Vector3 velocity)
		{
			if (_currentAccelerationTime >= accelerationTime)
				return velocity;
			_currentAccelerationTime += Time.deltaTime;
			velocity *= Mathf.Pow(_currentAccelerationTime / accelerationTime, accelerationCurvePower);
			// TODO: Make a serializable field that controls what function is being used
			// Also possible formulas (kx)/(kx+1-x) and (x+kx)/(kx+1) 
			// Where k = (1-someVar)^3 <- k can have different function, just this way someVar can be small value
			return velocity;
		}

		private Vector3 SpeedGradualDeceleration(Vector3 velocity)
		{
			if (_currentDecelerationTime >= decelerationTime)
			{
				_smoothMovement = Vector3.zero;
				return Vector3.zero;
			}

			_currentDecelerationTime += Time.deltaTime;
			velocity *= 1 - Mathf.Pow(_currentDecelerationTime / decelerationTime, decelerationCurvePower);
			return velocity;
		}


		private void CalculateMovementInputSmoothing()
		{
			_smoothMovement = Vector3.Lerp(_smoothMovement, _normalizedMovement,
				Time.deltaTime * inverseSmoothingMultiplier);
		}

		public void OnMoveStart(InputValue value)
		{
			var rawMovement = value.Get<Vector2>();
			if (rawMovement == Vector2.zero)
				return;
			_normalizedMovement =
				(_bodyPosition.right * rawMovement.x + _bodyPosition.forward * rawMovement.y).normalized;
			_isMovementPressed = true;
			_currentDecelerationTime = 0f;
		}

		public void OnMoveStop(InputValue value)
		{
			_isMovementPressed = false;
			_currentAccelerationTime = 0f;
		}

		public void OnLook(InputValue value)
		{
			Vector2 rotation = value.Get<Vector2>();
			_yRotation += rotation.x * mouseSensitivity;
			_xRotation -= rotation.y * mouseSensitivity;
			_xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
			_cameraPosition.transform.localRotation = Quaternion.Euler(_xRotation, 0, 0);
			_bodyPosition.rotation = Quaternion.Euler(0, _yRotation, 0);
		}

		public void OnJump(InputValue value)
		{
			if (!_isGrounded) return;
			_downwardsVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravitationalConstant);
			_isJumping = true;
		}
	}
}