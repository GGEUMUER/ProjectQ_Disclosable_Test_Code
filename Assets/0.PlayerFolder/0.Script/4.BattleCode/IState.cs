public interface IState
{
    void Enter(Character character);
    void Exit(Character character);
}
public interface IExecutableState : IState
{
    void Execute(Character character);
}