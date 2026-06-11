using UnityEngine;

namespace MagicExamHall
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerSpriteAnimator : MonoBehaviour
    {
        private const float IdleFrameSeconds = 0.5f;
        private const float WalkFrameSeconds = 0.13f;
        private const float CastFrameSeconds = 0.1f;
        private const float CastChargeSeconds = 0.34f;
        private const float CastReleaseSeconds = 0.22f;
        private const float MovingThreshold = 0.08f;

        public Color fallbackSkin = new(0.95f, 0.92f, 0.78f);
        public Color fallbackRobe = new(0.28f, 0.62f, 0.96f);

        private SpriteRenderer spriteRenderer;
        private PixelSpriteView staticSpriteView;
        private PlayerSpriteSet spriteSet;
        private PlayerFacing facing = PlayerFacing.Down;
        private PlayerAnimationState state = PlayerAnimationState.Idle;
        private int sortingOrder = 30;
        private int frameIndex;
        private float frameTimer;
        private float castTimer;
        private bool initialized;

        public PlayerFacing Facing => facing;
        public PlayerAnimationState CurrentState => state;
        public int CurrentFrameIndex => frameIndex;
        public bool HasExternalFrames => spriteSet != null && spriteSet.HasExternalFrames;

        private bool IsCasting => state is PlayerAnimationState.CastCharge or PlayerAnimationState.CastRelease;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        private void Update()
        {
            Initialize();
            Tick(Time.deltaTime);
        }

        public void SetSortingOrder(int value)
        {
            sortingOrder = value;
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = sortingOrder;
            }
        }

        public void SetMotion(Vector2 input, Vector2 currentVelocity)
        {
            Initialize();

            var direction = input.sqrMagnitude > 0.001f ? input : currentVelocity;
            if (!IsCasting && direction.sqrMagnitude > MovingThreshold * MovingThreshold)
            {
                facing = ResolveFacing(direction);
            }

            if (IsCasting)
            {
                return;
            }

            SetState(currentVelocity.sqrMagnitude > MovingThreshold * MovingThreshold || input.sqrMagnitude > MovingThreshold * MovingThreshold
                ? PlayerAnimationState.Walk
                : PlayerAnimationState.Idle);
        }

        public void PlayCast()
        {
            Initialize();
            SetState(PlayerAnimationState.CastCharge);
            castTimer = 0f;
        }

        public void InitializeForTests()
        {
            initialized = false;
            Initialize();
        }

        public void TickForTests(float deltaTime)
        {
            Initialize();
            Tick(deltaTime);
        }

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
            staticSpriteView = GetComponent<PixelSpriteView>();
            if (staticSpriteView != null)
            {
                fallbackSkin = staticSpriteView.primary;
                fallbackRobe = staticSpriteView.secondary;
                sortingOrder = Mathf.Max(sortingOrder, staticSpriteView.sortingOrder);
                staticSpriteView.enabled = false;
            }

            spriteSet = PlayerSpriteLibrary.Load(fallbackSkin, fallbackRobe);
            spriteRenderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = sortingOrder;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            initialized = true;
            ApplyFrame();
        }

        private void Tick(float deltaTime)
        {
            if (IsCasting)
            {
                castTimer += deltaTime;
                if (state == PlayerAnimationState.CastCharge && castTimer >= CastChargeSeconds)
                {
                    castTimer = 0f;
                    SetState(PlayerAnimationState.CastRelease);
                }
                else if (state == PlayerAnimationState.CastRelease && castTimer >= CastReleaseSeconds)
                {
                    castTimer = 0f;
                    SetState(PlayerAnimationState.Idle);
                }
            }

            var frameCount = spriteSet.GetFrameCount(state, facing);
            if (frameCount <= 1)
            {
                return;
            }

            frameTimer += deltaTime;
            var frameSeconds = FrameSeconds(state);
            while (frameTimer >= frameSeconds)
            {
                frameTimer -= frameSeconds;
                frameIndex = (frameIndex + 1) % frameCount;
                ApplyFrame();
            }
        }

        private void SetState(PlayerAnimationState next)
        {
            if (state == next)
            {
                return;
            }

            state = next;
            frameIndex = 0;
            frameTimer = 0f;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            spriteRenderer.sprite = spriteSet.GetFrame(state, facing, frameIndex);
            spriteRenderer.sharedMaterial = PixelMaterialProvider.SpriteMaterial;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        private static float FrameSeconds(PlayerAnimationState value)
        {
            return value switch
            {
                PlayerAnimationState.Idle => IdleFrameSeconds,
                PlayerAnimationState.Walk => WalkFrameSeconds,
                _ => CastFrameSeconds
            };
        }

        private static PlayerFacing ResolveFacing(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                return direction.x < 0f ? PlayerFacing.Left : PlayerFacing.Right;
            }

            return direction.y < 0f ? PlayerFacing.Down : PlayerFacing.Up;
        }
    }
}
