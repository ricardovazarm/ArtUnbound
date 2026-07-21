using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArtUnbound.Tutorial
{
    /// <summary>
    /// Ghost hand actor for the first-run tutorial. Owns a ghosted copy of the XR Hands
    /// sample RightHand mesh and plays scripted demo animations (tap / pinch-and-carry)
    /// entirely by code — no AnimationClips, no dependency on the real hand tracking.
    /// Zero knowledge of the game flow: TutorialFlowController decides when to play what.
    /// </summary>
    public class TutorialHandController : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("Malla de mano rigged (Assets/Samples/XR Hands/1.7.1/HandVisualizer/Models/RightHand.fbx). NO usar el prefab con HandVisualizer: seguiria el tracking real.")]
        [SerializeField] private GameObject handModelPrefab;
        [Tooltip("Material fantasma (GhostPreviewMat). Se instancia en runtime para animar el alpha sin tocar el asset.")]
        [SerializeField] private Material ghostMaterial;
        [Tooltip("Offset de rotacion para alinear el eje del FBX con la direccion de avance (el rig del sample no apunta con +Z). Ajustar una vez en pruebas.")]
        [SerializeField] private Vector3 handRotationOffsetEuler = new Vector3(-90f, 0f, 0f);

        [Header("Animacion")]
        [Tooltip("Distancia (m) frente al target donde la mano hace hover antes del tap.")]
        [SerializeField] private float hoverDistance = 0.12f;
        [Tooltip("Profundidad (m) del avance del gesto de tap.")]
        [SerializeField] private float tapDepth = 0.025f;
        [Tooltip("Alpha maximo del ghost (el material base usa 0.235). Subir si se pierde sobre passthrough.")]
        [SerializeField] private float maxAlpha = 0.35f;

        [Header("Pinch (curl de dedos por bone)")]
        [Tooltip("Grados de flexion del indice al cerrar el pinch (por joint: proximal/intermediate/distal).")]
        [SerializeField] private float indexCurlDegrees = 25f;
        [Tooltip("Grados de flexion del pulgar al cerrar el pinch (por joint).")]
        [SerializeField] private float thumbCurlDegrees = 15f;

        public bool IsPlaying { get; private set; }

        private GameObject _handInstance;
        private Material _runtimeMat;
        private Coroutine _demoCoroutine;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        // Bones de pinch: transform + rotacion local de reposo (pose abierta del FBX)
        private readonly List<(Transform bone, Quaternion rest, float curl)> _pinchBones =
            new List<(Transform, Quaternion, float)>();

        // Nombres de joints del rig del sample de XR Hands. Si el FBX usa otros nombres,
        // no se encuentra ninguno y SetPinch degrada a no-op (la demo sigue sin dedos).
        private static readonly (string name, bool isThumb)[] PinchJointNames =
        {
            ("IndexProximal", false), ("IndexIntermediate", false), ("IndexDistal", false),
            ("ThumbMetacarpal", true), ("ThumbProximal", true), ("ThumbDistal", true),
        };

        // ─────────────────────────────────────────────────────────────────────
        //  API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Tap demo: appear near the target → approach → tap in/out twice → fade out. Repeats 'loops' times.</summary>
        public void PlayTapDemo(Transform target, int loops = 2, Action onFinished = null)
        {
            if (target == null) { onFinished?.Invoke(); return; }
            StartDemo(TapDemoRoutine(target, Mathf.Max(1, loops), onFinished));
        }

        /// <summary>Pinch-and-carry demo: appear at 'from' → close pinch → arc to 'to' → release. Repeats 'loops' times. Never touches real pieces.</summary>
        public void PlayGrabDemo(Vector3 fromWorld, Vector3 toWorld, int loops = 2, Action onFinished = null)
        {
            StartDemo(GrabDemoRoutine(fromWorld, toWorld, Mathf.Max(1, loops), onFinished));
        }

        /// <summary>Aborts any running demo immediately (quick fade + deactivate). Safe to call anytime.</summary>
        public void StopDemo()
        {
            if (_demoCoroutine != null)
            {
                StopCoroutine(_demoCoroutine);
                _demoCoroutine = null;
            }
            IsPlaying = false;
            if (_handInstance != null && _handInstance.activeSelf)
                StartCoroutine(QuickFadeOut());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SETUP
        // ─────────────────────────────────────────────────────────────────────

        private void StartDemo(IEnumerator routine)
        {
            if (IsPlaying) StopDemo();
            if (!EnsureHandInstance()) return;
            IsPlaying = true;
            _demoCoroutine = StartCoroutine(routine);
        }

        /// <summary>Instancia lazy de la mano fantasma; devuelve false si faltan referencias.</summary>
        private bool EnsureHandInstance()
        {
            if (_handInstance != null) return true;
            if (handModelPrefab == null || ghostMaterial == null)
            {
                Debug.LogWarning("[Tutorial] TutorialHandController: handModelPrefab o ghostMaterial sin asignar — tutorial sin efecto");
                return false;
            }

            _handInstance = Instantiate(handModelPrefab, transform);
            _handInstance.name = "GhostHand";

            // El FBX puede traer Animator: fuera, la pose la controla este script.
            foreach (var animator in _handInstance.GetComponentsInChildren<Animator>(true))
                Destroy(animator);

            // Un solo material runtime compartido por todos los renderers para animar el alpha.
            _runtimeMat = new Material(ghostMaterial);
            foreach (var renderer in _handInstance.GetComponentsInChildren<Renderer>(true))
            {
                var mats = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = _runtimeMat;
                renderer.sharedMaterials = mats;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            CachePinchBones();
            SetAlpha(0f);
            _handInstance.SetActive(false);
            return true;
        }

        private void CachePinchBones()
        {
            _pinchBones.Clear();
            foreach (var t in _handInstance.GetComponentsInChildren<Transform>(true))
            {
                foreach (var (jointName, isThumb) in PinchJointNames)
                {
                    if (t.name.IndexOf(jointName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _pinchBones.Add((t, t.localRotation, isThumb ? thumbCurlDegrees : indexCurlDegrees));
                        break;
                    }
                }
            }
            Debug.Log($"[Tutorial] GhostHand: {_pinchBones.Count} pinch bones encontrados" +
                      (_pinchBones.Count == 0 ? " (fallback sin dedos: la demo mueve solo la mano)" : string.Empty));
        }

        private void OnDestroy()
        {
            if (_runtimeMat != null) Destroy(_runtimeMat);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DEMOS
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator TapDemoRoutine(Transform target, int loops, Action onFinished)
        {
            for (int loop = 0; loop < loops; loop++)
            {
                if (target == null || !target.gameObject.activeInHierarchy) break;

                Vector3 targetPos = target.position;
                Vector3 hoverPos  = HoverPoint(targetPos, out Quaternion handRot);
                // Ligeramente abajo-derecha para no tapar el elemento que se enseña
                Vector3 restPos = hoverPos + HoverRight(targetPos) * 0.04f - Vector3.up * 0.03f;

                PlaceHand(restPos, handRot);
                SetPinch(0f);
                yield return Fade(0f, maxAlpha, 0.3f);

                // Dos taps por loop
                for (int tap = 0; tap < 2; tap++)
                {
                    if (target != null) targetPos = target.position; // seguir al target si la UI se movio
                    Vector3 nearPos = Vector3.Lerp(targetPos, HoverPoint(targetPos, out handRot), 0.25f);

                    yield return MoveHand(restPos, nearPos, handRot, 0.45f);
                    Vector3 tapDir = (targetPos - nearPos).normalized;
                    yield return MoveHand(nearPos, nearPos + tapDir * tapDepth, handRot, 0.15f);
                    yield return MoveHand(nearPos + tapDir * tapDepth, nearPos, handRot, 0.15f);
                    yield return new WaitForSeconds(0.35f);
                    restPos = nearPos;
                }

                yield return Fade(maxAlpha, 0f, 0.3f);
                yield return new WaitForSeconds(0.4f);
            }

            FinishDemo(onFinished);
        }

        private IEnumerator GrabDemoRoutine(Vector3 fromWorld, Vector3 toWorld, int loops, Action onFinished)
        {
            for (int loop = 0; loop < loops; loop++)
            {
                Vector3 hoverFrom = HoverPoint(fromWorld, out Quaternion handRot);

                PlaceHand(hoverFrom, handRot);
                SetPinch(0f);
                yield return Fade(0f, maxAlpha, 0.3f);

                // Acercarse a la pieza y cerrar el pinch
                Vector3 grabPos = Vector3.Lerp(fromWorld, hoverFrom, 0.15f);
                yield return MoveHand(hoverFrom, grabPos, handRot, 0.4f);
                yield return AnimatePinch(0f, 1f, 0.25f);
                yield return new WaitForSeconds(0.15f);

                // Viaje con arco hacia el slot
                Vector3 dropPos = Vector3.Lerp(toWorld, HoverPoint(toWorld, out _), 0.15f);
                Vector3 arcUp   = Camera.main != null ? Camera.main.transform.up : Vector3.up;
                float duration = 1.2f, elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                    Vector3 pos = Vector3.Lerp(grabPos, dropPos, t) + arcUp * (Mathf.Sin(t * Mathf.PI) * 0.05f);
                    _handInstance.transform.position = pos;
                    yield return null;
                }

                // Soltar: abrir pinch + micro-retroceso
                yield return AnimatePinch(1f, 0f, 0.2f);
                yield return MoveHand(dropPos, dropPos + (hoverFrom - fromWorld).normalized * 0.03f, _handInstance.transform.rotation, 0.15f);
                yield return new WaitForSeconds(0.4f);

                yield return Fade(maxAlpha, 0f, 0.3f);
                yield return new WaitForSeconds(0.4f);
            }

            FinishDemo(onFinished);
        }

        private void FinishDemo(Action onFinished)
        {
            SetPinch(0f);
            if (_handInstance != null) _handInstance.SetActive(false);
            IsPlaying = false;
            _demoCoroutine = null;
            onFinished?.Invoke();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Punto de hover del lado del usuario (hacia la camara) y rotacion de la mano mirando al target.</summary>
        private Vector3 HoverPoint(Vector3 target, out Quaternion handRotation)
        {
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : target + Vector3.back;
            Vector3 toUser = (camPos - target).normalized;
            Vector3 hover  = target + toUser * hoverDistance;
            Vector3 fwd    = (target - hover).normalized;
            handRotation   = Quaternion.LookRotation(fwd, Vector3.up) * Quaternion.Euler(handRotationOffsetEuler);
            return hover;
        }

        private Vector3 HoverRight(Vector3 target)
        {
            Vector3 camPos = Camera.main != null ? Camera.main.transform.position : target + Vector3.back;
            return Vector3.Cross(Vector3.up, (target - camPos).normalized).normalized;
        }

        private void PlaceHand(Vector3 position, Quaternion rotation)
        {
            _handInstance.transform.SetPositionAndRotation(position, rotation);
            _handInstance.SetActive(true);
        }

        private IEnumerator MoveHand(Vector3 from, Vector3 to, Quaternion rotation, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                _handInstance.transform.SetPositionAndRotation(Vector3.Lerp(from, to, t), rotation);
                yield return null;
            }
        }

        private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            SetAlpha(toAlpha);
        }

        private IEnumerator QuickFadeOut()
        {
            yield return Fade(CurrentAlpha(), 0f, 0.15f);
            if (_handInstance != null) _handInstance.SetActive(false);
        }

        private void SetAlpha(float alpha)
        {
            if (_runtimeMat == null) return;
            Color c = _runtimeMat.HasProperty(BaseColorId) ? _runtimeMat.GetColor(BaseColorId) : Color.white;
            c.a = alpha;
            if (_runtimeMat.HasProperty(BaseColorId)) _runtimeMat.SetColor(BaseColorId, c);
            else _runtimeMat.color = c;
        }

        private float CurrentAlpha()
        {
            if (_runtimeMat == null) return 0f;
            return _runtimeMat.HasProperty(BaseColorId) ? _runtimeMat.GetColor(BaseColorId).a : _runtimeMat.color.a;
        }

        private IEnumerator AnimatePinch(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                SetPinch(Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            SetPinch(to);
        }

        /// <summary>t=0 mano abierta (pose de reposo del FBX), t=1 pinch cerrado. No-op si no se hallaron bones.</summary>
        private void SetPinch(float t)
        {
            foreach (var (bone, rest, curl) in _pinchBones)
            {
                if (bone == null) continue;
                bone.localRotation = rest * Quaternion.Euler(curl * t, 0f, 0f);
            }
        }
    }
}
