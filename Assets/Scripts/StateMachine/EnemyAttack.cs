using UnityEngine;

namespace StateMachine
{
    public class EnemyAttack : IEnemyState
    {
        private EnemyAI self;

        // State-local fields (không cần expose ra EnemyAI)
        private float fireTimer;
        private bool  isBackingOff;

        public EnemyAttack(EnemyAI self)
        {
            this.self = self;
        }

        public void Enter()
        {
            fireTimer    = Random.Range(0f, self.minFireInterval * 0.6f);
            isBackingOff = false;
        }

        public void Update()
        {
            if (!self.CanSeePlayer()) { self.EnterState(EnemyAI.AIState.Search); return; }
            float dist = Vector2.Distance(self.rb.position, self.playerTransform.position);
            // Hysteresis: cần ra xa hơn 30% mới quay sang Chase
            if (dist > self.attackRange * 1.3f) { self.EnterState(EnemyAI.AIState.Chase); return; }
            // Hysteresis: vào back-off khi < 45%, thoát khi > 65% → không dao động ngưỡng
            if (dist < self.attackRange * 0.45f) isBackingOff = true;
            else if (dist > self.attackRange * 0.65f) isBackingOff = false;
            if (isBackingOff)
            {
                // Lùi ra – target đặt xa (5 unit) để điểm đích ổn định, không nhảy theo từng frame
                Vector2 awayDir = (self.rb.position - (Vector2)self.playerTransform.position).normalized;
                self.SetBodyMoveTarget(self.rb.position + awayDir * 5f, self.moveSpeed * 0.55f);
            }
            else
            {
                // Đứng yên khi bắn – xe tăng dừng lại để nhắm
                self.wantBrake = true;
            }
            self.AimTurretAt(self.playerTransform, self.turretTrackSpeed);
            self.TryFire(ref fireTimer);
        }

        public void Exit()
        {
        }
    }
}