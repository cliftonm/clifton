namespace PoloronInterfaces
{
	public class Poloron : Ball2D
	{
		public PoloronId Id { get; set; }
		public PoloronState State { get; set; }
		public bool Visible { get; set; }

		public Poloron()
		{
			Visible = true;
		}
	}
}
