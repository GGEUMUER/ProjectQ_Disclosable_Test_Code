namespace Core.SinglePlay
{
    /// <summary>
    /// 게임 전반에 사용되는 상수 값들을 정의하는 클래스입니다.
    /// 하드코딩을 방지하고 값의 일관성을 유지하기 위해 사용됩니다.
    /// </summary>
    public static class GameConstants
    {
        /// <summary>
        /// 첫 번째 유닛 선택 시 사용되는 버튼 ID의 오프셋 값입니다.
        /// UI 버튼 인덱스와 실제 유닛 ID 간의 차이를 보정하는 데 사용됩니다.
        /// </summary>
        public const int FirstPickButtonIdOffset = 5;
    }
}
