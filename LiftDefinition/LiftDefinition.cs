using System;
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
	public class LiftDefinition
	{
		private Dictionary<string, LiftData> m_data;

		public Dictionary<string, LiftData> data
		{
			get { return m_data; }
			set { m_data = value; }
		}

		public void Init()
		{
			//init
		}

		public void Load()
		{
			//loading function
		}
	}
}
