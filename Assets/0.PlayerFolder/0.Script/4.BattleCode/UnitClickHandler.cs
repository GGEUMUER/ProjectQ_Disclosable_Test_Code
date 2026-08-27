using UnityEngine;

/// <summary>
/// 개별 유닛의 클릭 이벤트를 감지하고 처리하는 역할을 담당합니다.
/// 이 클래스를 통해 유닛의 배치나 선택과 같은 상위 로직으로부터 입력 처리를 분리하여,
/// 각 유닛이 독립적으로 자신의 클릭 이벤트를 관리할 수 있도록 합니다.
/// </summary>
public class UnitClickHandler : MonoBehaviour
{
    private UnitPlacementHandler _unitPlacemenHandler;
    private UnitMetaTag _unitMetaTag;

    /// <summary>
    /// 외부에서(주로 유닛 생성 시점에서) 필요한 의존성을 주입하기 위해 사용됩니다.
    /// 이 초기화 패턴을 통해 UnitClickHandler는 UnitPlacementHandler에 대한 직접적인 종속성 없이
    /// 유연하게 다른 핸들러와 연결될 수 있습니다.
    /// </summary>
    /// <param name="handler">클릭 이벤트를 전달받아 처리할 핸들러입니다.</param>
    /// <param name="metaTag">클릭된 유닛의 정보를 담고 있는 메타 데이터입니다.</param>
    public void Init(UnitPlacementHandler handler, UnitMetaTag metaTag)
    {
        _unitPlacemenHandler = handler;
        _unitMetaTag = metaTag;
    }

    /// <summary>
    /// Unity의 내장 메시지인 OnMouseDown을 사용하여 사용자 입력을 감지합니다.
    /// 복잡한 입력 시스템을 구현하는 대신, Collider가 있는 오브젝트에 대한 클릭을
    /// 가장 간단하게 처리하기 위해 이 방법을 선택했습니다.
    /// </summary>
    void OnMouseDown()
    {
        if (_unitPlacemenHandler != null)
        {
            _unitPlacemenHandler.OnUnitClicked(_unitMetaTag);
        }
    }
}
