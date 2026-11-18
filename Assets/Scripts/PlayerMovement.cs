using System.Runtime.CompilerServices;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.VFX;

public class PlayerMovement : MonoBehaviour
{
    private PlayerControls controls; // 컨트롤러
    private CharacterController controller;
    [SerializeField] private Slider staminaSlider;

    private float verticalVelocity; // 수직 속력
    private float currentStamina; // 현재 스태미나
    private bool isExhausted = false; // 탈진 상태
    private float slideTimer; // 슬라이딩 유지 시간
    private Vector3 slideDirection; // 슬라이딩 유지 방향

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f;
    [SerializeField] private float staminaRecoveryRate = 15f;
    [SerializeField] private float exhaustedThreshold = 25f; // 탈진 상태 임계값, 스태미나가 0 이하로 해당 수치까지 회복될 때 까지 스프린트 불가
    [SerializeField] private float slideSpeed = 8f;
    [SerializeField] private float slideDuration = 0.6f;
    [SerializeField] private Transform cameraTransform; // 카메라 위치
    [SerializeField] private float standingCameraHeight = 1.6f; // 서있는 상태 카메라 위치
    [SerializeField] private float crouchingCameraHeight = 1.0f; // 앉은 상태 카메라 위치
    private enum StanceState // 가능한 이동 자세
    {
        Standing,
        Crouching
    }
    private enum MovementState // 가능한 움직임 상태
    {
        Idling,
        Walking,
        Sprinting,
        Sliding
    }
    private StanceState currentStanceState = StanceState.Standing; // 현재 이동 자세 변수
    private MovementState currentMovementState = MovementState.Idling; // 현재 움직임 상태 변수
    

    private void Awake()
    {
        controls = new PlayerControls();
        controller = GetComponent<CharacterController>();
        currentStamina = maxStamina;
        staminaSlider.maxValue = maxStamina;
        staminaSlider.value = currentStamina; 
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
        Vector3 horizontalMove = transform.right * moveInput.x + transform.forward * moveInput.y; // 현재 Player가 바라보는 방향 기준으로 이동
        bool isMoving = moveInput.sqrMagnitude > 0.01f; // 움직임이 거의 0이아니면 이동 중 상태

        HandleMovementState(horizontalMove, isMoving); // 움직임 처리
        CameraSet(); // 카메라위치 처리
    }

    private void HandleSlide(){
        slideTimer -= Time.deltaTime;
        verticalVelocity += gravity * Time.deltaTime; // 떨어지는 속도 계산
        Vector3 velocity = slideDirection * slideSpeed; // 수평 움직임

        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        velocity.y = verticalVelocity; // 수직 움직임

        controller.Move(velocity * Time.deltaTime);

        if(slideTimer <= 0){
            currentMovementState = MovementState.Idling;
            currentStanceState = StanceState.Crouching;
        }
    }

    private void HandleMovementState(Vector3 horizontalMove, bool isMoving){
        
        // 슬라이딩상태 우선 제어
        if (currentMovementState == MovementState.Sliding)
        {
            HandleSlide();
            return;
        }

        // 슬라이딩 상태 정의
        if (currentMovementState == MovementState.Sprinting
        && controls.Player.Crouch.WasPressedThisFrame())
        {
            currentMovementState = MovementState.Sliding;
            currentStanceState = StanceState.Crouching;

            slideDirection = horizontalMove.normalized;
            slideTimer = slideDuration;

            return;
        }

        // 현재 플레이어 상태 정의
        if (controls.Player.Crouch.WasPressedThisFrame())
        { // 토글 식, 웅크리기 버튼을 눌렀음, 이미 웅크린 상태가 아니면 웅크리기 상태
            if (currentStanceState != StanceState.Crouching)
            {
                currentStanceState = StanceState.Crouching;
            }
            else
            {   
                currentStanceState = StanceState.Standing;
            }
        }
        if (isMoving && controls.Player.Sprint.IsPressed() && !isExhausted)
        { // 이동 중, 달리기 버튼 누르는 중, 탈진 상태 아니면 달리기 상태
            currentMovementState = MovementState.Sprinting;
            currentStanceState = StanceState.Standing;
        }
        else if (isMoving)
        { // 이동 중이라면 걷기 상태
            currentMovementState = MovementState.Walking;
        }
        else
        { // 기본 정지 상태
            currentMovementState = MovementState.Idling;
        }

        HandleMovement(horizontalMove, currentStanceState, currentMovementState); // 스태미나 관리 & 플레이어 속도 조정 함수
    }
    private void HandleMovement(Vector3 horizontalMove, StanceState stanceState, MovementState movementState){

        float currentSpeed = moveSpeed;

        // 달리기 및 스태미나 관리
        if (stanceState == StanceState.Standing && movementState == MovementState.Sprinting) // 서있기 --- 달리기 중
        {
            currentSpeed *= sprintMultiplier;
            currentStamina -= staminaDrainRate * Time.deltaTime;
        }
        else // 나머지 모든 상황
        {
            currentStamina += staminaRecoveryRate * Time.deltaTime;
        }
        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina); // 스태미나 최저/최대 수치 설정

        if (currentStamina <= 0f)
        {
            isExhausted = true;
        }
        if (isExhausted && currentStamina >= exhaustedThreshold)
        {
            isExhausted = false;
        }
        staminaSlider.value = currentStamina;

        /// 점프
        if (controller.isGrounded && verticalVelocity < 0f)
        { // 
            verticalVelocity = -2f; // -2f는 안정적으로 isGrounded를 유지하기 위해 약간 누르는 것
        }

        if (controller.isGrounded && controls.Player.Jump.WasPressedThisFrame())
        { // 점프
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity); // v^2 = 2gh. 초기 점프속도 = +5.42
        }
        // 웅크리기
        if(stanceState == StanceState.Crouching){
            currentSpeed = crouchSpeed;
        }

        /// 속도 계산 및 움직임
        verticalVelocity += gravity * Time.deltaTime; // 떨어지는 속도 계산

        Vector3 velocity = horizontalMove * currentSpeed; // 수평 움직임

        velocity.y = verticalVelocity; // 수직 움직임

        controller.Move(velocity * Time.deltaTime); // moveSpeed를 바꿔도 낙하속도는 일정하도록 수직/수평 움직임을 나눠서 계산했다가 합침
    
    }

    private void CameraSet(){
        Vector3 cameraPosition = cameraTransform.localPosition; // 현재 카메라 위치 받기
        if(currentStanceState == StanceState.Standing){ // 선자세 카메라 위치
            cameraPosition.y = standingCameraHeight;
        }
        else if(currentStanceState == StanceState.Crouching){ // 앉은 자세 카메라 위치
            cameraPosition.y = crouchingCameraHeight;
        }

        cameraTransform.localPosition = cameraPosition; // 카메라 위치 업데이트
    }
}
