using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Milch
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField]
        private GameObject model = null;
        [SerializeField]
        private Camera camera = null;
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
        private Rigidbody rigidbody = null;
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
            this.rigidbody = this.model.GetComponent<Rigidbody>();
            this.characterHips = this.model.transform.Find(this.modelRootName).Find(this.modelHipsName).gameObject;
            this.faceSkinnedMeshRenderer = this.model.transform.Find(this.modelFaceName).gameObject.GetComponent<SkinnedMeshRenderer>();
            this.model.transform.rotation = Quaternion.Euler(0.0f, 180.0f, 0.0f);
            this.eyeTransforms = new Transform[] { this.animator.GetBoneTransform(HumanBodyBones.LeftEye), this.animator.GetBoneTransform(HumanBodyBones.RightEye) };
            this.defaultEyeRotations = new Quaternion[] { this.eyeTransforms[0].localRotation, this.eyeTransforms[1].localRotation };
        }

        void FixedUpdate()
        {
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

            this.sourcePitch = Mathf.SmoothStep(this.sourcePitch, this.targetPitch, Time.fixedDeltaTime * this.transitionSpeed);
            this.sourceYaw = Mathf.SmoothStep(this.sourceYaw, this.targetYaw, Time.fixedDeltaTime * this.transitionSpeed);
            this.sourceCameraOffsetY = Mathf.SmoothStep(this.sourceCameraOffsetY, this.targetCameraOffsetY, Time.fixedDeltaTime * this.transitionSpeed);
            this.sourceCameraOffsetZ = Mathf.SmoothStep(this.sourceCameraOffsetZ, this.targetCameraOffsetZ, Time.fixedDeltaTime * this.transitionSpeed);

            if (this.sourceCameraOffsetZ != this.targetCameraOffsetY || this.sourceCameraOffsetZ != this.targetCameraOffsetZ || this.sourceCameraOffsetZ != this.targetCameraOffsetY || this.sourceCameraOffsetZ != this.targetCameraOffsetZ)
            {
                if (Mathf.Abs(this.targetPitch - this.sourcePitch) < float.Epsilon && Mathf.Abs(this.targetYaw - this.sourceYaw) < float.Epsilon && Mathf.Abs(this.targetCameraOffsetZ - this.sourceCameraOffsetY) < float.Epsilon && Mathf.Abs(this.targetCameraOffsetZ - this.sourceCameraOffsetZ) < float.Epsilon)
                {
                    this.sourcePitch = this.targetPitch;
                    this.sourceYaw = this.targetYaw;
                    this.sourceCameraOffsetY = this.targetCameraOffsetY;
                    this.sourceCameraOffsetZ = this.targetCameraOffsetZ;
                }

                this.camera.transform.position = new Vector3(this.model.transform.position.x, this.model.transform.position.y + this.sourceCameraOffsetY, this.model.transform.position.z + this.sourceCameraOffsetZ);
                this.camera.transform.RotateAround(this.model.transform.position + Vector3.up * this.sourceCameraOffsetY, Vector3.right, this.sourcePitch);
                this.camera.transform.RotateAround(this.model.transform.position + Vector3.up * this.sourceCameraOffsetY, Vector3.up, this.sourceYaw);
                this.camera.transform.LookAt(this.model.transform.position + Vector3.up * this.sourceCameraOffsetY);
            }

            if (this.isCloseup)
            {
                Vector3 cameraForward = Vector3.Scale(this.camera.transform.forward, new Vector3(1, 0, 1)).normalized;

                moveForward = cameraForward * verticalInput + this.camera.transform.right * horizontalInput;

                this.model.transform.rotation = Quaternion.Slerp(this.model.transform.rotation, Quaternion.LookRotation(cameraForward), Time.fixedDeltaTime * this.transitionSpeed);
            }
            else
            {
                Vector3 cameraForward = Vector3.Scale(this.camera.transform.forward, new Vector3(1, 0, 1)).normalized;

                moveForward = cameraForward * verticalInput + this.camera.transform.right * horizontalInput;

                if (moveForward != Vector3.zero)
                {
                    this.model.transform.rotation = Quaternion.Slerp(this.model.transform.rotation, Quaternion.LookRotation(moveForward), Time.fixedDeltaTime * this.transitionSpeed);
                }
            }

            if (this.isGrounded)
            {
                if (Input.GetButton("Jump") && !this.animator.GetBool("IsJumping") && !this.animator.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
                {
                    this.rigidbody.AddForce(this.model.transform.up * this.rigidbody.mass * Mathf.Sqrt(2.0f * -Physics.gravity.y * this.jumpHeight), ForceMode.Impulse);
                    this.animator.SetBool("IsJumping", true);
                }
                else
                {
                    var magnitude = new Vector2(horizontalInput, verticalInput).normalized.magnitude;

                    if (magnitude > 0.0f)
                    {
                        this.rigidbody.velocity = moveForward.normalized * this.maxMovementSpeed * magnitude;
                    }

                    this.animator.SetFloat("Speed", magnitude);
                }
            }

            if (this.rigidbody.velocity.magnitude > this.walkingSpeed)
            {
                var to = (Mathf.Min(this.rigidbody.velocity.magnitude, this.maxMovementSpeed) - this.walkingSpeed) / (this.maxMovementSpeed - this.walkingSpeed) * (this.maxFieldOfView - this.minFieldOfView) + this.minFieldOfView;

                if (to - this.camera.fieldOfView < float.Epsilon)
                {
                    this.camera.fieldOfView = this.maxFieldOfView;
                }
                else
                {
                    this.camera.fieldOfView = Mathf.SmoothStep(this.camera.fieldOfView, to, Time.fixedDeltaTime * this.dynamicFieldOfViewSpeed);
                }
            }
            else if (this.camera.fieldOfView - this.minFieldOfView < float.Epsilon)
            {
                this.camera.fieldOfView = this.minFieldOfView;
            }
            else
            {
                this.camera.fieldOfView = Mathf.SmoothStep(this.camera.fieldOfView, this.minFieldOfView, Time.fixedDeltaTime * this.dynamicFieldOfViewSpeed);
            }
        }

        // Update is called once per frame
        void Update()
        {
            var limit = this.viewingAngle / 2.0f;

            for (int i = 0; i < this.eyeTransforms.Length; i++)
            {
                var quaternion = Quaternion.LookRotation(this.camera.transform.position - this.eyeTransforms[i].position);
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