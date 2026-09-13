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
        public bool IsWading { get; private set; }
        CharacterController controller;
        float pitch;
        float fallingSpeed;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            // Keep downward interaction and tool rays from hitting our own capsule.
            gameObject.layer = 2; // Unity Ignore Raycast; terrain collisions remain enabled.
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
            Move(input, Input.GetKey(KeyCode.LeftShift), Time.deltaTime);
            if (transform.position.y < -20 || Mathf.Abs(transform.position.x) > 443 || Mathf.Abs(transform.position.z) > 443)
                Teleport(ValleyShape.Spawn);
            if (Input.GetKeyDown(KeyCode.Home)) Teleport(ValleyShape.Spawn);
            if (Input.GetKeyDown(KeyCode.F7)) RiverDynamics.Current?.Visit();
        }

        public void Move(Vector2 input, bool sprint, float dt)
        {
            dt = Mathf.Clamp(dt, 0, .05f);
            if(HorseRiding.Current!=null && HorseRiding.Current.Mounted){IsWading=false;HorseRiding.Current.Move(input,sprint,dt);return;}
            Vector3 position = transform.position;
            float surface = RiverDynamics.Current == null ? ValleyShape.WaterHeight : RiverDynamics.Current.SurfaceHeight(position);
            IsWading = RiverDynamics.DepthAt(position.x, position.z) > .05f && position.y < surface + .12f;
            Vector3 motion = (transform.right * input.x + transform.forward * input.y) * (sprint ? 7 : 4);
            if (IsWading)
            {
                // Forgiving swimming support: waist-deep equilibrium and gentle current.
                // No breath meter, damage or underwater camera is required for family play.
                fallingSpeed += (surface - .65f - position.y) * 24 * dt;
                fallingSpeed *= Mathf.Exp(-6 * dt);
                fallingSpeed = Mathf.Clamp(fallingSpeed, -1.2f, 1.8f);
                motion *= .52f;
                if (RiverDynamics.Current != null) motion += RiverDynamics.CurrentAt(position) * .12f;
            }
            else
            {
                if (controller.isGrounded && fallingSpeed < 0) fallingSpeed = -2;
                fallingSpeed = Mathf.Max(fallingSpeed - 22 * dt, -40);
            }
            motion.y = fallingSpeed;
            controller.Move(motion * dt);
            if (IsWading && transform.position.y < surface - .95f)
                controller.Move(Vector3.up * (surface - .95f - transform.position.y));
        }

        public void Teleport(Vector3 position)
        {
            if(HorseRiding.Current!=null && HorseRiding.Current.Mounted)HorseRiding.Current.Release();
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            fallingSpeed = 0;
            IsWading = false;
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
