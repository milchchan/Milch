using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Milch
{
    public class CompassController : MonoBehaviour
    {
        [SerializeField]
        private GameObject targetObject = null;
        [SerializeField]
        private GameObject basePanelObject = null;
        [SerializeField]
        private float transitionSpeed = 10.0f;
        private RectTransform baseTransform = null;
        private string heading = "0";
        private float sourceAngle = 0.0f;
        private float targetAngle = 0.0f;

        // Start is called before the first frame update
        void Start()
        {
            this.baseTransform = this.basePanelObject.GetComponent<RectTransform>();
        }

        // Update is called once per frame
        void Update()
        {
            this.targetAngle = this.targetObject.transform.eulerAngles.y;

            if (this.sourceAngle != this.targetAngle)
            {
                float length = this.baseTransform.sizeDelta.x / 2;
                float offset = this.baseTransform.sizeDelta.y / 2;
                float leftSideAngle;
                float rightSideAngle;

                if (this.sourceAngle < this.targetAngle)
                {
                    leftSideAngle = 360.0f - this.targetAngle + this.sourceAngle;
                    rightSideAngle = this.targetAngle - this.sourceAngle;
                }
                else
                {
                    leftSideAngle = this.sourceAngle - this.targetAngle;
                    rightSideAngle = 360.0f - this.sourceAngle + this.targetAngle;
                }
                
                if (leftSideAngle > rightSideAngle)
                {
                    this.sourceAngle += Mathf.SmoothStep(0, rightSideAngle, Time.deltaTime * this.transitionSpeed);

                    if (this.sourceAngle >= 360.0)
                    {
                        this.sourceAngle = this.sourceAngle - 360.0f;
                    }
                }
                else
                {
                    this.sourceAngle -= Mathf.SmoothStep(0, leftSideAngle, Time.deltaTime * this.transitionSpeed);

                    if (this.sourceAngle < 0.0)
                    {
                        this.sourceAngle = 360.0f + this.sourceAngle;
                    }
                }

                if (Mathf.Abs(this.targetAngle - this.sourceAngle) < float.Epsilon)
                {
                    this.sourceAngle = this.targetAngle;
                }

                if (this.sourceAngle < 180)
                {
                    this.baseTransform.localPosition = new Vector3(length - offset - length - length / 360.0f * this.sourceAngle, this.baseTransform.localPosition.y, this.baseTransform.localPosition.z);
                }
                else
                {
                    this.baseTransform.localPosition = new Vector3(length - offset - length / 360.0f * this.sourceAngle, this.baseTransform.localPosition.y, this.baseTransform.localPosition.z);
                }
            }

            this.heading = string.Format("{0:0}", this.targetObject.transform.eulerAngles.y);
        }
    }
}