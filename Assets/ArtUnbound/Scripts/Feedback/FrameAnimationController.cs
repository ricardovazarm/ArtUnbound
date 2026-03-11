using System;
using System.Collections;
using ArtUnbound.Data;
using UnityEngine;

namespace ArtUnbound.Feedback
{
    /// <summary>
    /// Controls the frame reveal animation when puzzle is completed.
    /// Aligned with FrameTier: Bronce, Plata, Oro, Platinum.
    /// </summary>
    public class FrameAnimationController : MonoBehaviour
    {
        public event Action OnAnimationComplete;

        [Header("Frame Objects (optional - auto-created if null)")]
        [Tooltip("Bronce = Easy (64 pieces)")]
        [SerializeField] private GameObject frameBronce;
        [Tooltip("Plata = Normal (130-144 pieces)")]
        [SerializeField] private GameObject framePlata;
        [Tooltip("Oro = Hard (256 pieces)")]
        [SerializeField] private GameObject frameOro;
        [Tooltip("Platinum = Expert (512 pieces)")]
        [SerializeField] private GameObject framePlatinum;

        [Header("Tier Materials (for auto-creation)")]
        [SerializeField] private Material bronceMaterial;
        [SerializeField] private Material plataMaterial;
        [SerializeField] private Material oroMaterial;
        [SerializeField] private Material platinumMaterial;

        [Header("Animation Settings")]
        [SerializeField] private float revealDuration = 1.5f;
        [SerializeField] private float scalePunchAmount = 0.2f;
        [SerializeField] private float glowIntensity = 2f;
        [SerializeField] private float glowDuration = 0.5f;

        [Header("Particle Effects (optional)")]
        [SerializeField] private ParticleSystem sparkleEffect;
        [SerializeField] private ParticleSystem glowEffect;

        [Header("Frame Colors")]
        [SerializeField] private Color bronceColor = new Color(0.8f, 0.5f, 0.2f);
        [SerializeField] private Color plataColor = new Color(0.75f, 0.75f, 0.75f);
        [SerializeField] private Color oroColor = new Color(1f, 0.84f, 0f);
        [SerializeField] private Color platinumColor = new Color(0.85f, 0.85f, 0.9f);

        private GameObject currentFrame;
        private FrameTier currentTier;
        private Coroutine animationCoroutine;

        private void Awake()
        {
            EnsureFramesExist();
        }

        /// <summary>
        /// Creates frame objects if not assigned. Uses tier materials to build simple quad frames.
        /// </summary>
        private void EnsureFramesExist()
        {
            if (frameBronce != null && framePlata != null && frameOro != null && framePlatinum != null)
                return;

            if (bronceMaterial == null || plataMaterial == null || oroMaterial == null || platinumMaterial == null)
            {
                Debug.LogWarning("[FrameAnimationController] Frame objects and tier materials are unassigned. Frame reveal will play without visuals. Assign materials (Frame_Bronce, Frame_Plata, Frame_Oro, Frame_Platinum) in Inspector.");
                return;
            }

            if (frameBronce == null) frameBronce = CreateFrameQuad("FrameBronce", bronceMaterial);
            if (framePlata == null) framePlata = CreateFrameQuad("FramePlata", plataMaterial);
            if (frameOro == null) frameOro = CreateFrameQuad("FrameOro", oroMaterial);
            if (framePlatinum == null) framePlatinum = CreateFrameQuad("FramePlatinum", platinumMaterial);
        }

        private GameObject CreateFrameQuad(string name, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(0.8f, 0.6f, 0.05f);
            go.SetActive(false);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null && mat != null)
                renderer.sharedMaterial = mat;

            return go;
        }

        /// <summary>
        /// Plays the frame reveal animation for the given tier.
        /// </summary>
        public void PlayFrameReveal(FrameTier tier)
        {
            currentTier = tier;

            // Hide all frames first
            HideAllFrames();

            // Get and show the appropriate frame
            currentFrame = GetFrameForTier(tier);
            if (currentFrame != null)
            {
                currentFrame.SetActive(true);
                currentFrame.transform.localScale = Vector3.zero;
            }

            // Start the animation
            if (animationCoroutine != null)
                StopCoroutine(animationCoroutine);

            animationCoroutine = StartCoroutine(RevealAnimationCoroutine());
        }

        private IEnumerator RevealAnimationCoroutine()
        {
            if (currentFrame == null)
            {
                OnAnimationComplete?.Invoke();
                yield break;
            }

            // Play sparkle effect
            if (sparkleEffect != null)
            {
                var main = sparkleEffect.main;
                main.startColor = GetColorForTier(currentTier);
                sparkleEffect.Play();
            }

            // Scale up animation with easing
            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;

            while (elapsed < revealDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / revealDuration;

                // Elastic ease out
                float eased = ElasticEaseOut(t);
                currentFrame.transform.localScale = Vector3.LerpUnclamped(Vector3.zero, targetScale, eased);

                yield return null;
            }

            currentFrame.transform.localScale = targetScale;

            // Play glow effect
            yield return StartCoroutine(GlowPulseCoroutine());

            // Scale punch for emphasis
            yield return StartCoroutine(ScalePunchCoroutine());

            OnAnimationComplete?.Invoke();
        }

        private IEnumerator GlowPulseCoroutine()
        {
            if (glowEffect != null)
            {
                var main = glowEffect.main;
                main.startColor = GetColorForTier(currentTier);
                glowEffect.Play();
            }

            // Apply emission glow to frame material
            var renderer = currentFrame.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                Material mat = renderer.material;
                Color baseColor = GetColorForTier(currentTier);

                float elapsed = 0f;
                while (elapsed < glowDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / glowDuration;
                    float intensity = Mathf.Sin(t * Mathf.PI) * glowIntensity;

                    mat.SetColor("_EmissionColor", baseColor * intensity);
                    yield return null;
                }

                mat.SetColor("_EmissionColor", Color.black);
            }
            else
            {
                yield return new WaitForSeconds(glowDuration);
            }
        }

        private IEnumerator ScalePunchCoroutine()
        {
            Vector3 originalScale = currentFrame.transform.localScale;
            Vector3 punchScale = originalScale * (1f + scalePunchAmount);

            // Punch up
            float elapsed = 0f;
            float punchDuration = 0.1f;

            while (elapsed < punchDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / punchDuration;
                currentFrame.transform.localScale = Vector3.Lerp(originalScale, punchScale, t);
                yield return null;
            }

            // Return to normal
            elapsed = 0f;
            while (elapsed < punchDuration * 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (punchDuration * 2);
                currentFrame.transform.localScale = Vector3.Lerp(punchScale, originalScale, t);
                yield return null;
            }

            currentFrame.transform.localScale = originalScale;
        }

        private float ElasticEaseOut(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            float p = 0.3f;
            float s = p / 4f;

            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        private GameObject GetFrameForTier(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Bronce => frameBronce,
                FrameTier.Plata => framePlata,
                FrameTier.Oro => frameOro,
                FrameTier.Platinum => framePlatinum,
                _ => frameBronce  // Changed: Bronce is default
            };
        }

        private Color GetColorForTier(FrameTier tier)
        {
            return tier switch
            {
                FrameTier.Bronce => bronceColor,
                FrameTier.Plata => plataColor,
                FrameTier.Oro => oroColor,
                FrameTier.Platinum => platinumColor,
                _ => bronceColor  // Changed: Bronce is default
            };
        }

        private void HideAllFrames()
        {
            if (frameBronce != null) frameBronce.SetActive(false);
            if (framePlata != null) framePlata.SetActive(false);
            if (frameOro != null) frameOro.SetActive(false);
            if (framePlatinum != null) framePlatinum.SetActive(false);
        }

        /// <summary>
        /// Sets the frame visibility without animation.
        /// </summary>
        public void SetFrameImmediate(FrameTier tier)
        {
            HideAllFrames();
            currentTier = tier;
            currentFrame = GetFrameForTier(tier);

            if (currentFrame != null)
            {
                currentFrame.SetActive(true);
                currentFrame.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Hides the current frame.
        /// </summary>
        public void HideFrame()
        {
            HideAllFrames();
            currentFrame = null;
        }

        /// <summary>
        /// Stops any running animation.
        /// </summary>
        public void StopAnimation()
        {
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
                animationCoroutine = null;
            }

            if (sparkleEffect != null)
                sparkleEffect.Stop();

            if (glowEffect != null)
                glowEffect.Stop();
        }

        private void OnDisable()
        {
            StopAnimation();
        }
    }
}
