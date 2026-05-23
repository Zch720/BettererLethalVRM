using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UniGLTF;
using UnityEngine;
using UniVRM10;

namespace OomJan.BetterLethalVRM
{
    internal class BetterLethalVRMInstance
    {
        private const int FirstPersonLayer = 23;
        private const int ThirdPersonLayer = 0;

        public readonly HashSet<Renderer> Renderers = new();
        public HashSet<(Transform target, Transform source, Quaternion localRotation)> BoneTranslation = new();
        public Transform DeadBodyRoot;
        public Dictionary<Transform, Transform> DeadMap;
        public float HipOffset;
        public PlayerControllerB PlayerControllerB;

        public Vrm10Instance Vrm10Instance;

        public bool UseVrmBlinkExpression;
        public bool UseVrmMouthExpression;
        public int BlinkBlendshapeIndex;
        public int MouthBlendshapeIndex;
        public float BlinkIntervalMin;
        public float BlinkIntervalMax;
        public float BlinkDuration;

        public SkinnedMeshRenderer FaceMeshRenderer;
        private float _nextBlinkTime = -1f;
        private float _blinkEndTime = 0f;

        public void UpdateBlink()
        {
            if (!UseVrmBlinkExpression && (BlinkBlendshapeIndex < 0 || FaceMeshRenderer == null || FaceMeshRenderer.sharedMesh == null)) return;
            if (!UseVrmBlinkExpression && BlinkBlendshapeIndex >= FaceMeshRenderer.sharedMesh.blendShapeCount) return;

            float now = Time.time;

            if (_nextBlinkTime < 0f)
                _nextBlinkTime = now + UnityEngine.Random.Range(BlinkIntervalMin, BlinkIntervalMax);

            float weight;
            if (now < _blinkEndTime)
            {
                weight = 100f;
            }
            else
            {
                weight = 0f;
                if (now >= _nextBlinkTime)
                {
                    _blinkEndTime = now + BlinkDuration;
                    _nextBlinkTime = _blinkEndTime + UnityEngine.Random.Range(BlinkIntervalMin, BlinkIntervalMax);
                }
            }

            if (UseVrmBlinkExpression)
                Vrm10Instance.Runtime.Expression.SetWeight(ExpressionKey.CreateFromPreset(ExpressionPreset.blink), weight / 100f);
            else
                FaceMeshRenderer.SetBlendShapeWeight(BlinkBlendshapeIndex, weight);
        }

        private static readonly float[] _voiceSamples = new float[256];

        public void UpdateLipSync(float sensitivitySelf, float sensitivityOthers)
        {
            if (!UseVrmMouthExpression && (MouthBlendshapeIndex < 0 || FaceMeshRenderer == null || FaceMeshRenderer.sharedMesh == null)) return;
            if (!UseVrmMouthExpression && MouthBlendshapeIndex >= FaceMeshRenderer.sharedMesh.blendShapeCount) return;

            float weight;

            if (PlayerControllerB.IsOwner)
            {
                var comms = BetterLethalVRMManager.Instance.GetDissonanceComms();
                var voicePlayer = comms?.FindPlayer(comms.LocalPlayerName);
                float amplitude = voicePlayer?.Amplitude ?? 0f;
                weight = Mathf.Clamp01(amplitude * sensitivitySelf) * 100f;
            }
            else
            {
                var audioSource = PlayerControllerB.currentVoiceChatAudioSource;
                if (audioSource != null && audioSource.isPlaying)
                {
                    audioSource.GetOutputData(_voiceSamples, 0);
                    float sum = 0f;
                    foreach (var s in _voiceSamples) sum += s * s;
                    float rms = Mathf.Sqrt(sum / _voiceSamples.Length);
                    weight = Mathf.Clamp01(rms * sensitivityOthers) * 100f;
                }
                else
                {
                    weight = 0f;
                }
            }

            if (UseVrmMouthExpression)
                Vrm10Instance.Runtime.Expression.SetWeight(ExpressionKey.CreateFromPreset(ExpressionPreset.aa), weight / 100f);
            else
                FaceMeshRenderer.SetBlendShapeWeight(MouthBlendshapeIndex, weight);
        }

        public void SetSkeletonMimic(Transform Root)
        {
            DeadBodyRoot = Root;
            DeadMap = new Dictionary<Transform, Transform>();

            if (PlayerControllerB.deadBody != null && PlayerControllerB.deadBody.transform == Root) Root.name = "spine";

            foreach (var tBoneTranslation in BoneTranslation)
            {
                var tTransform = Root.FindDescendant(tBoneTranslation.source.parent.name);
                var tNewBone = new GameObject("VRM Rotation Bone").transform;

                tNewBone.parent = tTransform;
                tNewBone.position = tTransform.position;
                tNewBone.localRotation = tBoneTranslation.localRotation;

                DeadMap[tBoneTranslation.source] = tNewBone;
            }

            foreach (var tRenderer in Root.GetComponentsInChildren<Renderer>())
                if ((PlayerControllerB.deadBody != null && PlayerControllerB.deadBody.transform == Root) ||
                    tRenderer.name is "LOD1" or "LOD2" or "LOD3" or "LevelSticker" or "BetaBadge")
                    tRenderer.enabled = false;
        }

        public void UpdateVisibility()
        {
            var tDeadShouldRender = !PlayerControllerB.isPlayerDead ||
                                   (DeadBodyRoot != null && PlayerControllerB.deadBody != null);

            var tLocalShouldRender = !PlayerControllerB.gameplayCamera.enabled;
            foreach (var tRenderer in Renderers)
            {
                tRenderer.gameObject.layer = tLocalShouldRender ? ThirdPersonLayer : FirstPersonLayer;
                tRenderer.enabled = tDeadShouldRender;
            }
        }
    }
}
