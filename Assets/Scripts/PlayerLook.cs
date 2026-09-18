using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    private PlayerControls controls;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensivity = 0.1f;

    private float xRotation = 0f;

    private void Awake(){

        // 플레이어 컨트롤 객체 생성
        controls = new PlayerControls();

        // 커서 잠그고 투명하게 하기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
        Vector2 lookInput = controls.Player.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensivity;
        float mouseY = lookInput.y * mouseSensivity;

        // 몸 전체를 y축 기준으로 회전
        transform.Rotate(Vector3.up * mouseX);

        // 카메라만 x축 기준으로 위 아래로 회전, 유니티 x축 회전 방향과 일반적으로 기대하는 화면 전환 방향은 반대이므로 -임
        xRotation -= mouseY;

        // 고개가 뒤로 넘어가지 않게하는 제한
        xRotation = Mathf.Clamp(xRotation, -80f, 80f); // -80~80까지만 xRotation 변형가능

        // 카메라 로컬 회전을 x축으로 xRotation도 돌린만큼 설정하라
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // 유니티에서 rotation에는 quarternion을 씀.

        
    }
}
