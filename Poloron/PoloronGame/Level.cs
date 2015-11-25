using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PoloronGame
{
	public class GameLevels
	{
		public List<Level> Levels { get; set; }

		public GameLevels()
		{
			Levels = new List<Level>();
		}
	}

	public class Level
	{
		[XmlAttribute()]
		public int Id { get; set; }
		
		[XmlAttribute()]
		public int Polorons { get; set; }

		[XmlAttribute()]
		public bool Moving { get; set; }

		[XmlAttribute()]
		public bool GateMoving { get; set; }
	}
}
