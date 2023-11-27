using UnityEngine;
using System.Collections;

namespace Milch
{
	public class SpringManager : MonoBehaviour
	{
		public SpringBone[] springBones;

		private void LateUpdate()
		{
			for (int i = 0; i < springBones.Length; i++)
			{
				springBones[i].UpdateSpring();
			}
		}
	}
}
