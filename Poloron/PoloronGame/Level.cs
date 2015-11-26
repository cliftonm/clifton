using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PoloronGame
{
	public class GameLevels
	{
		public List<Level> Levels { get; set; }
	}

	public class Level
	{
		[XmlAttribute()]
		public int Id { get; set; }
		
		[XmlAttribute()]
		public int Polorons { get; set; }

		[XmlAttribute()]
		public int Moving { get; set; }

		[XmlAttribute()]
		public bool GateMoving { get; set; }

		public List<Position> Positions { get; set; }
	}

	public class Position
	{
		[XmlAttribute()]
		public int X { get; set; }

		[XmlAttribute()]
		public int Y { get; set; }
	}
}
