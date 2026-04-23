using UnityEngine;

namespace StateMachine
{
    public class EnemyChase : IEnemyState
    {
        private EnemyAI self;
        
        // Timer để làm mới đường đi đuổi theo player
        private float repathTimer;

        public EnemyChase(EnemyAI self)
        {
            this.self = self;
        }

        public void Enter()
        {
            RequestChasePath();
            repathTimer = self.pathRepathRate;
        }

        public void Update()
        {
            if (!self.CanSeePlayer()) { self.EnterState(EnemyAI.AIState.Search); return; }
            float dist = Vector2.Distance(self.rb.position, self.playerTransform.position);
            if (dist <= self.attackRange) { self.EnterState(EnemyAI.AIState.Attack); return; }

            repathTimer -= Time.deltaTime;
            if (repathTimer <= 0f && !self.PathPending)
            {
                RequestChasePath();
                repathTimer = self.pathRepathRate;
            }

            self.FollowPath(self.moveSpeed);
            self.AimTurretAt(self.playerTransform, self.turretTrackSpeed);
        }

        private void RequestChasePath()
        {
            Vector2 targetPos = self.playerTransform.position;
            
            // Tung xúc xắc xem lần cập nhật đường này có đuổi khôn ngoan không
            bool isSmart = Random.value <= self.searchIntelligence;
            
            if (!isSmart)
            {
                // Kém khôn: Không nhắm thẳng vào tâm Player mà chạy chệch choạc lạng lách (bám lệch 2.5m)
                targetPos += Random.insideUnitCircle * 2.5f;
                targetPos = self.ClampToBounds(targetPos);
            }
            
            self.RequestPath(targetPos);
        }

        public void Exit()
        {
            self.ClearPath();
        }
    }
}