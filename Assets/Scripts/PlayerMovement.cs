using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls;
    private CharacterController controller;
    private float verticalVelocity;
    private float currentStamina;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRecoveryRate = 15f;

    private void Awake()
    {
        controls = new PlayerControls();
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
    }
    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
    }

    private void Update()
    {
        Vector2 moveInput = controls.Player.Move.ReadValue<Vector2>(); // 사용자 입력 vector2로 읽기
        bool isMoving = moveInput.sqrMagnitude > 0.01f; // 움직임이 거의 0이아니면 움직임상태임
        bool isSprinting = isMoving && controls.Player.Sprint.IsPressed() && currentStamina > 0; // 현재 이동중이고 스프린트 버튼 눌리고 있고 스태미나가 0 이상이면 참
        float currentSpeed = moveSpeed;

        if (isSprinting)
        {
            currentSpeed *= sprintMultiplier;
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }

        else{
            currentStamina += staminaRecoveryRate * Time.deltaTime;
           
        }
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // 스태미나 최저/최대 수치 설정
        Debug.Log(currentStamina);

        Vector3 horizontalMove = transform.right * moveInput.x + transform.forward * moveInput.y; // 현재 Player가 바라보는 방향 기준으로 이동

        if(controller.isGrounded && verticalVelocity < 0f){ // 
            verticalVelocity = -2f; // -2f는 안정적으로 isGrounded를 유지하기 위해 약간 누르는 것
        }

        if(controller.isGrounded && controls.Player.Jump.WasPressedThisFrame()){ // 점프
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // v^2 = 2gh. 초기 점프속도 = +5.42
        }

        verticalVelocity += gravity * Time.deltaTime; // 떨어지는 속도 계산

        Vector3 velocity = horizontalMove * currentSpeed; // 수평 움직임

        velocity.y = verticalVelocity; // 수직 움직임

        controller.Move(velocity * Time.deltaTime); // moveSpeed를 바꿔도 낙하속도는 일정하도록 수직/수평 움직임을 나눠서 계산했다가 합침
    }
}
