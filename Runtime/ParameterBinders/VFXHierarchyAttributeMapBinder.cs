using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

namespace UnityEngine.VFX.Utils
{
    [VFXBinder("Utility/Plane")]
    public class VFXHierarchyAttributeMapBinder : VFXBinderBase
    {
        [VFXParameterBinding("System.UInt32"), SerializeField]
        protected ExposedParameter m_BoneCount = "BoneCount";

        [VFXParameterBinding("Texture2D"), SerializeField]
        protected ExposedParameter m_PositionMap = "PositionMap";

        [VFXParameterBinding("Texture2D"), SerializeField]
        protected ExposedParameter m_TargetPositionMap = "TargetPositionMap";

        [VFXParameterBinding("Texture2D"), SerializeField]
        protected ExposedParameter m_RadiusPositionMap = "RadiusPositionMap";


        public enum RadiusMode
        {
            Fixed,
            Interpolate
        }

        public Transform HierarchyRoot;
        public float DefaultRadius = 0.1f;
        public uint MaximumDepth = 3;
        public RadiusMode Radius = RadiusMode.Fixed;

        private Texture2D position;
        private Texture2D targetPosition;
        private Texture2D radius;

        private List<Bone> bones;

        private struct Bone
        {
            public Transform source;
            public float sourceRadius;
            public Transform target;
            public float targetRadius;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            UpdateHierarchy();
        }

        void OnValidate()
        {
            UpdateHierarchy();
        }

        void UpdateHierarchy()
        {
            bones = ChildrenOf(HierarchyRoot, MaximumDepth);
            int count = bones.Count;
            Debug.Log("Found Bone Count: " + count);

            position = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);
            targetPosition = new Texture2D(count, 1, TextureFormat.RGBAHalf, false, true);
            radius = new Texture2D(count, 1, TextureFormat.RHalf, false, true);

            UpdateData();
        }

        List<Bone> ChildrenOf(Transform source, uint depth)
        {
            List<Bone> output = new List<Bone>();
            foreach (Transform child in source)
            {
                output.Add(new Bone()
                {
                    source = source.transform,
                    target = child.transform,
                    sourceRadius = DefaultRadius,
                    targetRadius = DefaultRadius,
                });
                if(depth > 0)
                    output.AddRange(ChildrenOf(child, depth-1));
            }
            return output;
        }


        void UpdateData()
        {
            int count = bones.Count;
            if (position.width != count) return;

            List<Color> positionList = new List<Color>();
            List<Color> targetList = new List<Color>();
            List<Color> radiusList = new List<Color>();

            for (int i = 0; i < count; i++)
            {
                Bone b = bones[i];
                positionList.Add(new Color(b.source.position.x, b.source.position.y, b.source.position.z, 1));
                targetList.Add(new Color(b.target.position.x, b.target.position.y, b.target.position.z, 1));
                radiusList.Add(new Color(b.sourceRadius, 0, 0, 1));
            }
            position.SetPixels(positionList.ToArray());
            targetPosition.SetPixels(targetList.ToArray());
            radius.SetPixels(radiusList.ToArray());

            position.Apply();
            targetPosition.Apply();
            radius.Apply();
        }


        public override bool IsValid(VisualEffect component)
        {
            return HierarchyRoot != null
                && component.HasTexture(m_PositionMap)
                && component.HasTexture(m_TargetPositionMap)
                && component.HasTexture(m_RadiusPositionMap)
                && component.HasUInt(m_BoneCount);
        }

        public override void UpdateBinding(VisualEffect component)
        {
            UpdateData();

            component.SetTexture(m_PositionMap, position);
            component.SetTexture(m_TargetPositionMap, targetPosition);
            component.SetTexture(m_RadiusPositionMap, radius);
            component.SetUInt(m_BoneCount, (uint)bones.Count);
        }

        public override string ToString()
        {
            return string.Format("Hierarchy to AttributeMaps");
        }
    }
}
