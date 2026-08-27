using System;

public class StateMachine
{
    private IState currentState;
    private Character owner;
    private Action updateAction; // 상태마다 실행할 액션
    public StateMachine(Character owner)
    {
        this.owner = owner;
    }

    public void ChangeState(IState newState)
    {
        currentState?.Exit(owner);
        currentState = newState;
        currentState.Enter(owner);
        
        // 상태가 IExecutableState를 구현했으면 실행 액션 등록
        updateAction = currentState is IExecutableState exec
            ? () => exec.Execute(owner)
            : (Action)null;
    }

    public void Update()
    {
        updateAction?.Invoke();
    }
}