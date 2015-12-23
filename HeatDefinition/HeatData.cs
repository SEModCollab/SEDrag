﻿using System;
using System.Collections.Generic;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRageMath;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Components;
using Sandbox.Definitions;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;


namespace SEDrag.Definition
{
	public class HeatData
	{
		private Base6Directions.Direction m_front = Base6Directions.Direction.Forward; //which direction the block considers to be its 'forward'
		private Base6Directions.Direction m_top = Base6Directions.Direction.Up; //which direction the block considers to be its 'up'

		private double m_heatMult_f = 1.0;
		private double m_heatMult_b = 1.0;
		private double m_heatMult_u = 1.0;
		private double m_heatMult_d = 1.0;
		private double m_heatMult_l = 1.0;
		private double m_heatMult_r = 1.0;

		private double m_heatThresh_f = 750.0;
		private double m_heatThresh_b = 750.0;
		private double m_heatThresh_u = 750.0;
		private double m_heatThresh_d = 750.0;
		private double m_heatThresh_l = 750.0;
		private double m_heatThresh_r = 750.0;
		//etc

		//should have some variables for stabilization
		//some for adjusting the center of lift. 

		public Base6Directions.Direction front
		{
			get { return m_front; }
			set { m_front = value; }
		}
		public Base6Directions.Direction top
		{
			get { return m_top; }
			set { m_top = value; }
		}
		public double heatThresh_f
		{
			get { return m_heatThresh_f; }
			set { m_heatThresh_f = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_b
		{
			get { return m_heatThresh_b; }
			set { m_heatThresh_b = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_u
		{
			get { return m_heatThresh_u; }
			set { m_heatThresh_u = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_d
		{
			get { return m_heatThresh_d; }
			set { m_heatThresh_d = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_l
		{
			get { return m_heatThresh_l; }
			set { m_heatThresh_l = (value >= 750.0 ? value : 750.0); }
		}
		public double heatThresh_r
		{
			get { return m_heatThresh_r; }
			set { m_heatThresh_r = (value >= 750.0 ? value : 750.0); }
		}


		public double heatMult_f
		{
			get { return m_heatMult_f; }
			set { m_heatMult_f = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_b
		{
			get { return m_heatMult_b; }
			set { m_heatMult_b = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_d
		{
			get { return m_heatMult_d; }
			set { m_heatMult_d = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_u
		{
			get { return m_heatMult_u; }
			set { m_heatMult_u = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_l
		{
			get { return m_heatMult_l; }
			set { m_heatMult_l = (value >= 0.0 ? value : 0.0); }
		}
		public double heatMult_r
		{
			get { return m_heatMult_r; }
			set { m_heatMult_r = (value >= 0.0 ? value : 0.0); }
		}

		public double getHeatMult(Base6Directions.Direction dir)
		{
			return heatMult_f; //todo translate direction to local
        }
		public double getHeatTresh(Base6Directions.Direction dir)
		{
			return heatThresh_f; //todo translate direction to local
		}
	}
}

