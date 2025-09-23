/// <summary>
/// 목적지 타입 정의
/// 이 파일을 먼저 생성해야 다른 스크립트들이 GoalType을 인식합니다
/// </summary>
public enum GoalType
{
    Individual,     // 개별 목적지 (1:1 매칭)
    Shared,         // 공유 목적지 (여러 캐릭터가 같은 목적지)
    Single          // 단일 목적지 (모든 캐릭터가 하나의 목적지로)
}