public interface IStaticTag 
{
    public int OrderId { get; set; }

    void ApplyStats(Tower tower);
}