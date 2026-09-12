using UnityEngine;

namespace ExplorersByNature
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonWalker : MonoBehaviour
    {
        public Camera view;
        public float sensitivity = 1.6f;
        public bool MenuOpen { get; private set; }
        public bool Automated { get; set; }
        CharacterController controller;
        float pitch;
        float fallingSpeed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            sensitivity = PlayerPrefs.GetFloat("LookSensitivity", 1.6f);
            view.fieldOfView = PlayerPrefs.GetFloat("FieldOfView", 75f);
            SetMenu(false);
        }

        void Update()
        {
            if (Automated) return;
            if (Input.GetKeyDown(KeyCode.Escape)) SetMenu(!MenuOpen);
            if (MenuOpen) return;
            transform.Rotate(0, Input.GetAxisRaw("Mouse X") * sensitivity, 0);
            pitch = Mathf.Clamp(pitch - Input.GetAxisRaw("Mouse Y") * sensitivity, -80, 80);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            Vector2 input = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1);
            Vector3 motion = (transform.right * input.x + transform.forward * input.y) * (Input.GetKey(KeyCode.LeftShift) ? 7 : 4);
            if (controller.isGrounded && fallingSpeed < 0) fallingSpeed = -2;
            fallingSpeed = Mathf.Max(fallingSpeed - 22 * Time.deltaTime, -40);
            motion.y = fallingSpeed;
            controller.Move(motion * Time.deltaTime);
            if (transform.position.y < -20 || Mathf.Abs(transform.position.x) > 443 || Mathf.Abs(transform.position.z) > 443)
                Teleport(ValleyShape.Spawn);
            if (Input.GetKeyDown(KeyCode.Home)) Teleport(ValleyShape.Spawn);
        }

        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            fallingSpeed = 0;
        }

        public void SyncLookPitch()
        {
            pitch=view.transform.localEulerAngles.x;
            if(pitch>180)pitch-=360;
            pitch=Mathf.Clamp(pitch,-80,80);
        }

        public void SetMenu(bool open)
        {
            MenuOpen = open;
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = open;
            if (!open && view != null)
            {
                PlayerPrefs.SetFloat("LookSensitivity", sensitivity);
                PlayerPrefs.SetFloat("FieldOfView", view.fieldOfView);
            }
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && !Automated) SetMenu(true);
        }
    }
}
