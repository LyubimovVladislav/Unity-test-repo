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
		private float dragPower = 0.9f;

		[SerializeField] private float epsilon = 0.01f;
		[SerializeField] private float ascendingSpeed = 1f;
		[SerializeField] private float descendingSpeed = 1f;
		[SerializeField] private float speed = 12f;
		[SerializeField] private float movementSmoothing = 1f;
		

		private float _currentAscendingTime;
		private float _currentDescendingTime;
		private float _yRotation;
		private float _xRotation;
		private Transform _camera;
		private Transform _body;
		private CharacterController _controller;
		private float _currentSpeed;
		private bool _isMovementPressed;
		private Vector3 _normalizedMovement;
		private Vector3 _smoothMovement;


		private void Start()
		{
			_currentSpeed = 0f;
			_currentAscendingTime = 0f;
			_currentDescendingTime = 0f;
			_controller = GetComponent<CharacterController>();
			_camera = GetComponentInChildren<Camera>().transform;
			_body = transform;
			_yRotation = 0f;
			_xRotation = 0f;
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			_isMovementPressed = false;
		}

		private void Update()
		{
			Move();
		}

		private void Move()
		{
			
			CalculateMovementInputSmoothing();
			_controller.Move(_smoothMovement * (speed * Time.deltaTime));
		}


		private void CalculateMovementInputSmoothing()
		{
        
			_smoothMovement = Vector3.Lerp(_smoothMovement, _normalizedMovement, Time.deltaTime * movementSmoothing);

		}

		public void OnMoveStart(InputValue value)
		{ 
			var rawMovement = value.Get<Vector2>();
			_normalizedMovement = (_body.right * rawMovement.x + _body.forward * rawMovement.y).normalized;
			_isMovementPressed = true;
			_currentDescendingTime = 0f;
		}

		public void OnMoveStop(InputValue value)
		{
			_isMovementPressed = false;
			_currentAscendingTime = 0f;
		}

		public void OnLook(InputValue value)
		{
			Vector2 rotation = value.Get<Vector2>();
			_yRotation += rotation.x * mouseSensitivity;
			_xRotation -= rotation.y * mouseSensitivity;
			_xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
			_camera.transform.localRotation = Quaternion.Euler(_xRotation, 0, 0);
			_body.rotation = Quaternion.Euler(0, _yRotation, 0);
		}
	}
}