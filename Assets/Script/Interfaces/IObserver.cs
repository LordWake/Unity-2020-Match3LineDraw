public enum TYPE_OF_NOTIFY
{
   UpdateScore, UpdateMoves, OnMoveCubesStart,
   OnMoveCubesEnd, OnRebuildStart, OnRebuildEnd,
   ChangeMusicPitch, ChangeMusicToNormalPitch
};
public interface IObserver
{
    void OnNotify(TYPE_OF_NOTIFY typeOfNotify);
}
