public enum StratagemDirection
{
    Up,
    Down,
    Left,
    Right
}

public interface IInvestigatable
{
    void OnListenStart();
    void OnListening();
    void OnStratagemInput(StratagemDirection direction);
    void OnListenEnd();
}