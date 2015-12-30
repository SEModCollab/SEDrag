namespace SEDrag
{
	public class DragSettings
	{
		private int m_mult = 500;
		private int m_radMult = 500;
		private bool m_lift = false;
		private bool m_heat = true;
		private bool m_smoke = true;
		private bool m_burn = false;
		private DebugLevel logLevel = DebugLevel.None;
		public enum DebugLevel
		{
			None = 0,
			Error,
			Info,
			Verbose,
			Custom
		}
		//private bool m_autolift = false;
		public bool advancedlift
		{
			get
			{
				return m_lift;
			}
			set
			{
				m_lift = value;
			}
		}
		/*public bool auto_advancedlift
		{
			get
			{
				return m_autolift;
			}
			set
			{
				m_autolift = value;
			}
		}*/
		public DebugLevel debug
		{
			get { return logLevel; }
			set { logLevel = value; }
		}
		public int mult
		{
			get
			{
				return m_mult;
			}
			set
			{
				if (value > 0)
					m_mult = value;
			}
		}
		public int radMult
		{
			get
			{
				return m_radMult;
			}
			set
			{
				if (value > 0)
					m_radMult = value;
			}
		}
		public bool heat
		{
			get
			{
				return m_heat;
			}
			set
			{
				m_heat = value;
			}
		}

		public bool showsmoke
		{
			get
			{
				return m_smoke;
			}
			set
			{
				m_smoke = value;
			}
		}

		public bool showburn
		{
			get
			{
				return m_burn;
			}
			set
			{
				m_burn = value;
			}
		}
	}
}
