using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Milch
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private GameObject model = null;
        [SerializeField]
        private Camera followCamera = null;
        [SerializeField]
        private float minFieldOfView = 60.0f;
        [SerializeField]
        private float maxFieldOfView = 90.0f;
        [SerializeField]
        private float dynamicFieldOfViewSpeed = 5.0f;
        [SerializeField]
        private float cameraNeutralOffsetY = 2.5f;
        [SerializeField]
        private float cameraNeutralOffsetZ = -5.0f;
        [SerializeField]
        private float cameraCloseupOffsetY = 1.5f;
        [SerializeField]
        private float cameraCloseupOffsetZ = -2.5f;
        [SerializeField]
        private float cameraCloseupAltOffsetY = 1.0f;
        [SerializeField]
        private float cameraCloseupAltOffsetZ = -1.0f;
        [SerializeField]
        private float maxMovementSpeed = 10.0f;
        [SerializeField]
        private float walkingSpeed = 0.4f;
        [SerializeField]
        private float transitionSpeed = 10.0f;
        [SerializeField]
        private float jumpHeight = 1.0f;
        [SerializeField]
        private bool invertXAxis = false;
        [SerializeField]
        private bool invertYAxis = false;
        [SerializeField]
        private float minPitch = -45;
        [SerializeField]
        private float maxPitch = 45;
        private Animator animator = null;
        private Rigidbody rb = null;
        private GameObject characterHips = null;
        private SkinnedMeshRenderer faceSkinnedMeshRenderer = null;
        private bool isGrounded = true;
        private float cameraSpeed = 1.0f;
        private float sourceYaw = 0.0f;
        private float sourcePitch = 0.0f;
        private float targetYaw = 0.0f;
        private float targetPitch = 0.0f;
        private float sourceCharge = 0.0f;
        private float targetCharge = 0.0f;
        private bool isCloseup = false;
        private float sourceCameraOffsetY = 1.0f;
        private float sourceCameraOffsetZ = -5.0f;
        private float targetCameraOffsetY = 1.0f;
        private float targetCameraOffsetZ = -5.0f;
        private float idleTime = 0.0f;
        private float blinkedTime = 0.0f;
        private float blinkInterval = 10.0f;
        private float blinkStep = 0.0f;
        private float maxBlinkStep = 1.0f;
        private float blinkSpeed = 2.0f;
        private float viewingAngle = 60.0f;
        private Transform[] eyeTransforms = null;
        private Quaternion[] defaultEyeRotations = null;
        private string modelRootName = "metarig";
        private string modelHipsName = "hips";
        private string modelFaceName = "face";
        private string modelEyesBlendShapeName = "eyes_close";
        private Vector3 velocity = Vector3.zero;
        private bool jumpRequired = false;

        public float LookSensitivity
        {
            get
            {
                return this.cameraSpeed;
            }
            set
            {
                this.cameraSpeed = value;
            }
        }

        public bool IsInvertedLook
        {
            get
            {
                return this.invertYAxis;
            }
            set
            {
                this.invertYAxis = value;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            this.targetPitch = this.maxPitch / 2.0f;
            this.sourceCameraOffsetY = this.targetCameraOffsetY = this.cameraNeutralOffsetY;
            this.sourceCameraOffsetZ = this.targetCameraOffsetZ = this.cameraNeutralOffsetZ;
            this.animator = this.model.GetComponent<Animator>();
            this.rb = this.GetComponent<Rigidbody>();
            this.characterHips = this.model.transform.Find(this.modelRootName).Find(this.modelHipsName).gameObject;
            this.faceSkinnedMeshRenderer = this.model.transform.Find(this.modelFaceName).gameObject.GetComponent<SkinnedMeshRenderer>();
            this.model.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
            this.eyeTransforms = new Transform[] { this.animator.GetBoneTransform(HumanBodyBones.LeftEye), this.animator.GetBoneTransform(HumanBodyBones.RightEye) };
            this.defaultEyeRotations = new Quaternion[] { this.eyeTransforms[0].localRotation, this.eyeTransforms[1].localRotation };
        }

        void FixedUpdate()
        {
            if (this.jumpRequired)
            {
                this.rb.AddForce(this.model.transform.up * this.rb.mass * Mathf.Sqrt(2.0f * -Physics.gravity.y * this.jumpHeight), ForceMode.Impulse);
                this.jumpRequired = false;
            }

            this.rb.linearVelocity = new Vector3(this.velocity.x, this.rb.linearVelocity.y, this.velocity.z);
        }

        // Update is called once per frame
        void Update()
        {
            var limit = this.viewingAngle / 2.0f;
            var maxInput = Mathf.Exp(1.0f) - 1.0f;
            var horizontalInput = Input.GetAxisRaw("Horizontal");
            var verticalInput = Input.GetAxisRaw("Vertical");
            Vector3 moveForward;

            horizontalInput = (Mathf.Exp(Mathf.Abs(horizontalInput)) - 1.0f) / maxInput * Mathf.Sign(horizontalInput);
            verticalInput = (Mathf.Exp(Mathf.Abs(verticalInput)) - 1.0f) / maxInput * Mathf.Sign(verticalInput);

            if (Input.GetKey(KeyCode.LeftShift))
            {
                this.targetYaw += this.invertXAxis ? -horizontalInput : horizontalInput;
                this.targetPitch += this.invertYAxis ? -verticalInput : verticalInput;

                if (this.targetPitch > this.maxPitch)
                {
                    this.targetPitch = this.maxPitch;
                }
                else if (this.targetPitch < this.minPitch)
                {
                    this.targetPitch = this.minPitch;
                }

                horizontalInput = 0.0f;
                verticalInput = 0.0f;
            }

            if (Input.GetKey(KeyCode.Tab)/* || Input.GetAxisRaw("LeftTrigger") > 0.5*/)
            {
                this.targetCameraOffsetY = this.cameraCloseupOffsetY;
                this.targetCameraOffsetZ = this.cameraCloseupOffsetZ;
                this.isCloseup = true;
            }
            else
            {
                this.targetCameraOffsetY = this.cameraNeutralOffsetY;
                this.targetCameraOffsetZ = this.cameraNeutralOffsetZ;
                this.isCloseup = false;
            }

            this.sourcePitch = Mathf.SmoothStep(this.sourcePitch, this.targetPitch, Time.deltaTime * this.transitionSpeed);
            this.sourceYaw = Mathf.SmoothStep(this.sourceYaw, this.targetYaw, Time.deltaTime * this.transitionSpeed);
            this.sourceCameraOffsetY = Mathf.SmoothStep(this.sourceCameraOffsetY, this.targetCameraOffsetY, Time.deltaTime * this.transitionSpeed);
            this.sourceCameraOffsetZ = Mathf.SmoothStep(this.sourceCameraOffsetZ, this.targetCameraOffsetZ, Time.deltaTime * this.transitionSpeed);

            if (this.sourceCameraOffsetZ != this.targetCameraOffsetY || this.sourceCameraOffsetZ != this.targetCameraOffsetZ || this.sourceCameraOffsetZ != this.targetCameraOffsetY || this.sourceCameraOffsetZ != this.targetCameraOffsetZ)
            {
                if (Mathf.Abs(this.targetPitch - this.sourcePitch) < float.Epsilon && Mathf.Abs(this.targetYaw - this.sourceYaw) < float.Epsilon && Mathf.Abs(this.targetCameraOffsetZ - this.sourceCameraOffsetY) < float.Epsilon && Mathf.Abs(this.targetCameraOffsetZ - this.sourceCameraOffsetZ) < float.Epsilon)
                {
                    this.sourcePitch = this.targetPitch;
                    this.sourceYaw = this.targetYaw;
                    this.sourceCameraOffsetY = this.targetCameraOffsetY;
                    this.sourceCameraOffsetZ = this.targetCameraOffsetZ;
                }

                this.followCamera.transform.position = new Vector3(this.transform.position.x, this.transform.position.y + this.sourceCameraOffsetY, this.transform.position.z + this.sourceCameraOffsetZ);
                this.followCamera.transform.RotateAround(this.transform.position + Vector3.up * this.sourceCameraOffsetY, Vector3.right, this.sourcePitch);
                this.followCamera.transform.RotateAround(this.transform.position + Vector3.up * this.sourceCameraOffsetY, Vector3.up, this.sourceYaw);
                this.followCamera.transform.LookAt(this.transform.position + Vector3.up * this.sourceCameraOffsetY);
            }

            if (this.isCloseup)
            {
                Vector3 cameraForward = Vector3.Scale(this.followCamera.transform.forward, new Vector3(1, 0, 1)).normalized;

                moveForward = cameraForward * verticalInput + this.followCamera.transform.right * horizontalInput;

                this.model.transform.rotation = Quaternion.Slerp(this.model.transform.rotation, Quaternion.LookRotation(cameraForward), Time.deltaTime * this.transitionSpeed);
            }
            else
            {
                Vector3 cameraForward = Vector3.Scale(this.followCamera.transform.forward, new Vector3(1, 0, 1)).normalized;

                moveForward = cameraForward * verticalInput + this.followCamera.transform.right * horizontalInput;

                if (moveForward != Vector3.zero)
                {
                    this.model.transform.rotation = Quaternion.Slerp(this.model.transform.rotation, Quaternion.LookRotation(moveForward), Time.deltaTime * this.transitionSpeed);
                }
            }

            if (this.isGrounded)
            {
                if (Input.GetKeyUp(KeyCode.Return))
                {
                    if (this.animator.GetBool("IsDance"))
                    {
                        this.animator.SetBool("IsDance", false);
                    }
                    else
                    {
                        this.animator.SetBool("IsDance", true);
                    }
                }
                else if (Input.GetButton("Jump") && !this.animator.GetBool("IsJumping") && !this.animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
                {
                    this.jumpRequired = true;
                    this.animator.SetBool("IsJumping", true);
                }
                else
                {
                    var magnitude = new Vector2(horizontalInput, verticalInput).normalized.magnitude;

                    if (magnitude > 0.0f)
                    {
                        this.velocity = moveForward.normalized * this.maxMovementSpeed * magnitude;
                    }
                    else
                    {
                        this.velocity = Vector3.zero;
                    }

                    this.animator.SetFloat("Speed", magnitude);
                }
            }

            if (this.rb.linearVelocity.magnitude > this.walkingSpeed)
            {
                var to = (Mathf.Min(this.rb.linearVelocity.magnitude, this.maxMovementSpeed) - this.walkingSpeed) / (this.maxMovementSpeed - this.walkingSpeed) * (this.maxFieldOfView - this.minFieldOfView) + this.minFieldOfView;

                if (to - this.followCamera.fieldOfView < float.Epsilon)
                {
                    this.followCamera.fieldOfView = this.maxFieldOfView;
                }
                else
                {
                    this.followCamera.fieldOfView = Mathf.SmoothStep(this.followCamera.fieldOfView, to, Time.deltaTime * this.dynamicFieldOfViewSpeed);
                }
            }
            else if (this.followCamera.fieldOfView - this.minFieldOfView < float.Epsilon)
            {
                this.followCamera.fieldOfView = this.minFieldOfView;
            }
            else
            {
                this.followCamera.fieldOfView = Mathf.SmoothStep(this.followCamera.fieldOfView, this.minFieldOfView, Time.deltaTime * this.dynamicFieldOfViewSpeed);
            }

            for (int i = 0; i < this.eyeTransforms.Length; i++)
            {
                var quaternion = Quaternion.LookRotation(this.followCamera.transform.position - this.eyeTransforms[i].position);
                var angleX = quaternion.eulerAngles.x;
                var angleY = quaternion.eulerAngles.y;
                var angleZ = quaternion.eulerAngles.z;

                if (angleX > limit)
                {
                    if (angleX < 360 - limit)
                    {
                        angleX = limit;
                    }
                }

                if (angleY > limit)
                {
                    if (angleY < 360 - limit)
                    {
                        angleY = limit;
                    }
                }

                if (angleZ > limit)
                {
                    if (angleZ < 360 - limit)
                    {
                        angleZ = limit;
                    }
                }

                this.eyeTransforms[i].rotation = Quaternion.Euler(new Vector3() { x = angleX, y = angleY, z = angleZ });
                this.eyeTransforms[i].localRotation *= this.defaultEyeRotations[i];
            }

            if (this.animator.GetCurrentAnimatorStateInfo(0).IsName("Movement") && this.animator.GetFloat("Speed") == 0.0f)
            {
                this.idleTime += Time.deltaTime;

                if (this.idleTime >= this.blinkInterval)
                {
                    this.blinkStep += Time.deltaTime * this.blinkSpeed;

                    if (this.blinkStep >= this.maxBlinkStep)
                    {
                        this.blinkStep = 0.0f;
                        this.maxBlinkStep = Mathf.Round(UnityEngine.Random.Range(1.0f, 2.0f));
                        this.idleTime = 0.0f;
                        this.faceSkinnedMeshRenderer.SetBlendShapeWeight(this.faceSkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(this.modelEyesBlendShapeName), 0.0f);
                    }
                    else
                    {
                        this.faceSkinnedMeshRenderer.SetBlendShapeWeight(this.faceSkinnedMeshRenderer.sharedMesh.GetBlendShapeIndex(this.modelEyesBlendShapeName), Mathf.Abs(Mathf.Sin(this.blinkStep * Mathf.PI)) * 100.0f);
                    }
                }
            }
            else
            {
                this.idleTime = 0.0f;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.name == "Ground")
            {
                this.isGrounded = true;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (this.animator.GetBool("IsJumping") && this.animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
            {
                this.animator.SetBool("IsJumping", false);
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.name == "Ground")
            {
                this.isGrounded = false;
            }
        }
    }
}