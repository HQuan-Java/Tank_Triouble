using UnityEngine;

namespace StateMachine
{
    public class EnemyAlert : IEnemyState
    {
        private EnemyAI self;

        // State-local fields (không cần expose ra EnemyAI)
        private float stateTimer;

        public EnemyAlert(EnemyAI self)
        {
            this.self = self;
        }

        public void Enter()
        {
            stateTimer = Random.Range(self.minReactionDelay, self.maxReactionDelay);
        }

        public void Update()
        {
            self.wantBrake = true;
            self.AimTurretAt(self.playerTransform, self.turretTrackSpeed * 2.5f);
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f) self.EnterState(EnemyAI.AIState.Chase);
        }

        public void Exit()
        {
        }
    }
}