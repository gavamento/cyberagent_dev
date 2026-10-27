using UnityEngine;

namespace CyberAgentDev.ObjectPrototypeLearning
{
    // 歩く敵の移動。Kinematicの Rigidbody2D を横へ動かし、足元の地面に沿って落ちる。
    // 敵本体の当たり判定はTriggerなので、地面に乗せる処理はここで行う。
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class EnemyGroundMover : MonoBehaviour
    {
        [Tooltip("横の速さ(ユニット/秒)。マイナスで左へ")]
        [SerializeField] float speed = -1f;
        [Tooltip("足元の地面が下がったときに落ちる重力(ユニット/秒²)")]
        [Min(0f)][SerializeField] float gravity = 30f;
        [Tooltip("地面を探す距離(ユニット)")]
        [Min(0.1f)][SerializeField] float groundCheckDistance = 20f;
        [Tooltip("画面の右端からこの距離(ユニット)まで近づいたら動き出す。それまでは生成した位置で待つ")]
        [Min(0f)][SerializeField] float activateMargin = 1f;

        // これより高い段差は壁として扱う
        const float MaxStepUp = 0.05f;

        readonly RaycastHit2D[] hits = new RaycastHit2D[8];
        Rigidbody2D body;
        Camera cam;
        float fallVelocity;
        bool activated;

        // 状態に合わせて敵のスクリプトから変える
        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            cam = Camera.main;
        }

        void FixedUpdate()
        {
            // 画面に入る前に歩き出すと、生成時の配置(段差まわりの安全地帯など)が崩れるので待つ
            if (!activated)
            {
                if (!IsNearScreen()) return;
                activated = true;
            }

            float dt = Time.fixedDeltaTime;
            Vector2 position = body.position;
            // 進む先の地面が足元より高ければ壁なので、よじ登らずに止まる
            float nextX = position.x + speed * dt;
            if (!TryFindGround(new Vector2(nextX, position.y), out float aheadY) || aheadY <= position.y + MaxStepUp)
                position.x = nextX;

            if (TryFindGround(position, out float groundY) && position.y - groundY <= 0.01f)
            {
                position.y = groundY;
                fallVelocity = 0f;
            }
            else
            {
                fallVelocity += gravity * dt;
                position.y -= fallVelocity * dt;
                // 落ちすぎて地面を抜けないようにする
                if (TryFindGround(body.position, out groundY) && position.y < groundY) position.y = groundY;
            }
            body.MovePosition(position);
        }

        // 画面の右端から activateMargin 以内まで来たか。カメラがなければ待たずに動く
        bool IsNearScreen()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return true;
            float right = cam.transform.position.x + cam.orthographicSize * cam.aspect;
            return body.position.x <= right + activateMargin;
        }

        // 足元の、Triggerでも動く物体でもない当たり判定(地面のタイルマップなど)の上面
        bool TryFindGround(Vector2 feet, out float groundY)
        {
            groundY = 0f;
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.Raycast(feet + Vector2.up * 0.1f, Vector2.down, filter, hits, groundCheckDistance);
            float best = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var other = hits[i].collider.attachedRigidbody;
                // Playerなど動く物体の上には乗らない
                if (other != null && other.bodyType != RigidbodyType2D.Static) continue;
                best = Mathf.Max(best, hits[i].point.y);
            }
            if (float.IsNegativeInfinity(best)) return false;
            groundY = best;
            return true;
        }
    }
}
