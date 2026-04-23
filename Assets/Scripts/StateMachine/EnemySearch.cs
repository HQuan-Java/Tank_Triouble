using UnityEngine;

namespace StateMachine
{
    public class EnemySearch : IEnemyState
    {
        private EnemyAI self;

        // State-local fields
        private float stateTimer;
        private Vector2 searchTarget;
        private bool reachedPos;

        public EnemySearch(EnemyAI self)
        {
            this.self = self;
        }

        public void Enter()
        {
            stateTimer = Random.Range(self.minSearchTime, self.maxSearchTime);
            
            // Tung xúc xắc xem lần này khôn hay ngu (dựa vào tỉ lệ searchIntelligence)
            bool isSmart = Random.value <= self.searchIntelligence;

            if (isSmart)
            {
                // Khôn: rảo bước kiểm tra khu vực hướng về phía điểm cuối cùng nhìn thấy Player (bán kính 4.5)
                Vector2 randomOffset = Random.insideUnitCircle * 4.5f;
                searchTarget = self.ClampToBounds((Vector2)self.playerTransform.position + randomOffset);
            }
            else
            {
                // Kém khôn: đi bừa một hướng tào lao xa xa để tìm kiếm chứ không liên quan gì vị trí Player nữa
                Vector2 randomOffset = Random.insideUnitCircle * Random.Range(8f, 12f);
                searchTarget = self.ClampToBounds(self.rb.position + randomOffset);
            }

            reachedPos = false;
            
            // Yêu cầu Seeker đi tới điểm đã quyết định
            self.RequestPath(searchTarget);
        }

        public void Update()
        {
            // Liên tục đảo nòng pháo tìm quanh
            self.ScanTurret();
            
            // Nếu tình cờ thấy lại player trong lúc chạy đến điểm check -> chuyển Alert/Chase
            if (self.CanSeePlayer()) { self.EnterState(EnemyAI.AIState.Alert); return; }

            if (!reachedPos)
            {
                // Hành quân cẩn thận tới điểm vừa mất dấu
                self.FollowPath(self.moveSpeed * 0.85f);
                
                // Tới nơi thì bắt đầu tính giờ rà soát
                if (self.ReachedPathEnd)
                {
                    reachedPos = true;
                }
            }
            else
            {
                // Đứng lại quét nòng tìm
                self.wantBrake = true;
                stateTimer -= Time.deltaTime;

                // Quá lâu không thấy ai -> bỏ cuộc quay về tuần tra
                if (stateTimer <= 0f)
                {
                    self.PickNewPatrolTarget();
                    self.EnterState(EnemyAI.AIState.Patrol);
                }
            }
        }

        public void Exit()
        {
            self.ClearPath();
        }
    }
}