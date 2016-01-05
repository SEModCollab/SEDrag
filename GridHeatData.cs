namespace SEDrag
{
	public class GridHeatData
	{
		public double front = 0;
		public double back = 0;
		public double up = 0;
		public double down = 0;
		public double left = 0;
		public double right = 0;

		public GridHeatData()
		{

		}
		public GridHeatData(double l, double r, double u, double d, double f, double b)
		{
			front = f;
			back = b;
			up = u;
			down = d;
			left = l;
			right = r;
		}

		public static GridHeatData operator /(GridHeatData c1, float c2)
		{
			return new GridHeatData(c1.left / c2, c1.right / c2, c1.up / c2, c1.down / c2, c1.front / c2, c1.back / c2);
		}
	}
}
