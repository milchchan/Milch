using UnityEngine;
using System.Collections;

namespace Milch
{
	public class RandomWind : MonoBehaviour
	{
		[SerializeField]
		private bool isActive = true;
		private SpringBone[] springBones = null;

		// Use this for initialization
		void Start()
		{
			this.springBones = GetComponent<SpringManager>().springBones;
		}

		// Update is called once per frame
		void Update()
		{
			Vector3 force = this.isActive ? new Vector3(Mathf.PerlinNoise(Time.time, 0.0f) * 0.005f, 0, 0) : Vector3.zero;

			for (int i = 0; i < this.springBones.Length; i++)
			{
				this.springBones[i].springForce = force;
			}
		}
	}
}
