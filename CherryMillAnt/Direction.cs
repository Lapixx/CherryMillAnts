using System;

namespace Ants {

	public enum Direction {
		North,
		East,
        South,
		West
	}

	public static class DirectionExtensions {

		public static char ToChar (this Direction self) {
			switch (self)
			{
				case Direction.East:
					return 'e';

				case Direction.North:
					return 'n';

				case Direction.South:
					return 's';

				case Direction.West:
					return 'w';

				default:
					throw new ArgumentException ("Unknown direction", "self");
			}
		}

        public static Direction Rotate(Direction dir, int rotate)
        {
            return (Direction)(((int)dir + rotate + 4) % 4);
        }
	}
}