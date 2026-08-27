using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 유닛 ID 목록을 설정된 우선순위 규칙에 따라 정렬.
/// </summary>
/// <remarks>
/// 동일 우선순위 유닛은 무작위 배치.
/// 우선순위 규칙은 GetPriority 메서드에 하드코딩되어 있으며,
/// 향후 데이터 기반(예: ScriptableObject) 관리로 전환 권장.
/// </remarks>
public class UnitSorter
{
    private readonly Random _random = new Random();

    /// <summary>
    /// 유닛 ID 리스트를 우선순위와 무작위 순서에 따라 정렬.
    /// </summary>
    /// <param name="pcSelectedUnitIds">정렬할 유닛 ID 원본 리스트.</param>
    /// <returns>정렬이 적용된 원본 리스트 참조. 정렬 대상이 없으면 null 반환.</returns>
    /// <remarks>
    /// 정렬 기준:
    /// 1. GetPriority()로 얻은 우선순위(오름차순).
    /// 2. 동일 우선순위 내 무작위 순서.
    /// </remarks>
    public List<int> SortSelectedUnits(List<int> pcSelectedUnitIds)
    {
        if (pcSelectedUnitIds == null || pcSelectedUnitIds.Count <= 1) return null;

        var sortedList = pcSelectedUnitIds
            .OrderBy(id => GetPriority(id))
            .ThenBy(_ => _random.Next())
            .ToList();

        pcSelectedUnitIds.Clear();
        pcSelectedUnitIds.AddRange(sortedList);

        return pcSelectedUnitIds;
    }

    /// <summary>
    /// 유닛 ID에 따른 정렬 우선순위 가중치 반환.
    /// </summary>
    /// <param name="unitId">대상 유닛 ID.</param>
    /// <returns>우선순위 값 (낮을수록 정렬 순서가 앞).</returns>
    /// <remarks>
    /// 기획 데이터에 의존하는 하드코딩된 값.
    /// 관련 기획 변경 시 반드시 동기화 필요.
    /// </remarks>
    private int GetPriority(int unitId)
    {
        return unitId switch
        {
            5 => 1,
            6 => 2,
            0 => 3,
            1 => 4,
            2 or 3 or 4 => 5,
            7 or 8 or 9 => 6,
            _ => 7
        };
    }
}
