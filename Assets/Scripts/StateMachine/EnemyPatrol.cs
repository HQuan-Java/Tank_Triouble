using UnityEngine;

namespace StateMachine
{
    public class EnemyPatrol : IEnemyState
    {
        private EnemyAI self;

        // State-local fields (không cần expose ra EnemyAI)
        private float idleTimer;
        private bool  isIdling;

        public EnemyPatrol(EnemyAI self)
        {
            this.self = self;
        }

        public void Enter()
        {
            self.RequestPath(self.patrolTarget);
        }

        public void Update()
        {
            if (self.CanSeePlayer()) { self.EnterState(EnemyAI.AIState.Alert); return; }

            if (isIdling)
            {
                idleTimer -= Time.deltaTime;
                self.ScanTurret();
                self.wantBrake = true;
                if (idleTimer <= 0f)
                {
                    isIdling = false;
                    self.PickNewPatrolTarget();
                    self.RequestPath(self.patrolTarget);
                }
                return;
            }

            self.FollowPath(self.moveSpeed * 0.6f);

            if (self.ReachedPathEnd)
            {
                isIdling  = true;
                idleTimer = Random.Range(self.minIdleTime, self.maxIdleTime);
            }
        }

        public void Exit()
        {
            self.ClearPath();
        }
    }
}