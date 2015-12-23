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
using System.Text.RegularExpressions;
using System.Runtime.Serialization;

namespace SEDrag.Definition
{
	public class HeatDefinition
	{
		private Dictionary<string, HeatData> m_data = new Dictionary<string, HeatData>();
		private const string FILE = "heat.xml";
		public Dictionary<string, HeatData> data
		{
			get { return m_data; }
			set { m_data = value; }
		}
		public void Init()
		{
			//init
			var def = MyDefinitionManager.Static.GetAllDefinitions();
			Regex reg = new Regex("heat{(.*?)}");
			Regex regcom = new Regex(",");
			Regex regeq = new Regex("=");
			HeatData value;
            foreach (var keys in def)
			{
				try
				{

					Log.DebugWrite(DragSettings.DebugLevel.Info,keys.Id.SubtypeName);
					Log.DebugWrite(DragSettings.DebugLevel.Info,keys.DescriptionString);
					if (keys.DescriptionString == null || keys.DescriptionString.Length == 0) continue;
					var res = reg.Split(keys.DescriptionString);
					Log.IncreaseIndent();
					if (res.Length > 1)
					{
						Log.Info(res[1]);
						if(m_data.TryGetValue(keys.Id.SubtypeName, out value))
						{
							//already exists!
							Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("Warning Duplicate match found: {0}", keys.Id.SubtypeName));
						}
						else
						{
							value = new HeatData();
							var search = regcom.Split(res[1]);
							if (search == null)
							{
								Log.DecreaseIndent();
								continue;
							}
							foreach (string parts in search)
							{
								var dataeq = regeq.Split(parts);
								if (dataeq.Length == 0)
								{
									Log.DecreaseIndent();
									continue;
								}
								switch(dataeq[0].ToLower())
								{
									case "all":
										value.heatMult_u = Convert.ToDouble(dataeq[1]);
										value.heatMult_d = Convert.ToDouble(dataeq[1]);
										value.heatMult_l = Convert.ToDouble(dataeq[1]);
										value.heatMult_r = Convert.ToDouble(dataeq[1]);
										value.heatMult_f = Convert.ToDouble(dataeq[1]);
										value.heatMult_b = Convert.ToDouble(dataeq[1]);
										break;
									case "u":
									case "up":
										value.heatMult_u = Convert.ToDouble(dataeq[1]);
										break;
									case "d":
									case "down":
										value.heatMult_d = Convert.ToDouble(dataeq[1]);
										break;
									case "l":
                                    case "left":
										value.heatMult_l = Convert.ToDouble(dataeq[1]);
										break;
									case "r":
									case "right":
										value.heatMult_r = Convert.ToDouble(dataeq[1]);
										break;
									case "f":
									case "forward":
										value.heatMult_f = Convert.ToDouble(dataeq[1]);
										break;
                                    case "b":
									case "backward":
										value.heatMult_b = Convert.ToDouble(dataeq[1]);
										break;
									case "face":
										switch(dataeq[1].ToLower())
										{
											case "u":
											case "up":
												value.front = Base6Directions.Direction.Up;
												break;
											case "d":
											case "down":
												value.front = Base6Directions.Direction.Down;
												break;
											case "l":
											case "left":
												value.front = Base6Directions.Direction.Left;
												break;
											case "r":
											case "right":
												value.front = Base6Directions.Direction.Right;
												break;
											case "f":
											case "forward":
												value.front = Base6Directions.Direction.Forward;
												break;
											case "b":
											case "backward":
												value.front = Base6Directions.Direction.Backward;
												break;
										}
										break;
									case "top":
										switch (dataeq[1].ToLower())
										{
											case "u":
											case "up":
												value.top = Base6Directions.Direction.Up;
												break;
											case "d":
											case "down":
												value.top = Base6Directions.Direction.Down;
												break;
											case "l":
											case "left":
												value.top = Base6Directions.Direction.Left;
												break;
											case "r":
											case "right":
												value.top = Base6Directions.Direction.Right;
												break;
											case "f":
											case "forward":
												value.top = Base6Directions.Direction.Forward;
												break;
											case "b":
											case "backward":
												value.top = Base6Directions.Direction.Backward;
												break;
										}
										break;
									case "tall":
										value.heatThresh_u = Convert.ToDouble(dataeq[1]);
										value.heatThresh_d = Convert.ToDouble(dataeq[1]);
										value.heatThresh_l = Convert.ToDouble(dataeq[1]);
										value.heatThresh_r = Convert.ToDouble(dataeq[1]);
										value.heatThresh_f = Convert.ToDouble(dataeq[1]);
										value.heatThresh_b = Convert.ToDouble(dataeq[1]);
										break;
									case "tu":
									case "tup":
										value.heatThresh_u = Convert.ToDouble(dataeq[1]);
										break;
									case "td":
									case "tdown":
										value.heatThresh_d = Convert.ToDouble(dataeq[1]);
										break;
									case "tl":
									case "tleft":
										value.heatThresh_l = Convert.ToDouble(dataeq[1]);
										break;
									case "tr":
									case "tright":
										value.heatThresh_r = Convert.ToDouble(dataeq[1]);
										break;
									case "tf":
									case "tforward":
										value.heatThresh_f = Convert.ToDouble(dataeq[1]);
										break;
									case "tb":
									case "tbackward":
										value.heatThresh_b = Convert.ToDouble(dataeq[1]);
										break;
								}
							}
							Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("Adding {0}", keys.Id.SubtypeName));
							data.Add(keys.Id.SubtypeName, value);
						}
					}
					Log.DecreaseIndent();

				}
				catch (Exception ex)
				{
					Log.DebugWrite(DragSettings.DebugLevel.Error, string.Format("Warning Error in Description: {0} {1}", keys.Id.SubtypeName, ex.ToString()));
				}

			}
			foreach(KeyValuePair<string, HeatData> items in data)
			{
				Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("{0} updated.", items.Key));
			}
		}
		public void Load(bool l_default = false)
		{
			Log.DebugWrite(DragSettings.DebugLevel.Info, "Loading XML");
			if (l_default)
			{
				m_data.Clear();
				Init();
				Log.DebugWrite(DragSettings.DebugLevel.Info, "Loaded Defaults");
				return;
			}
			try
			{
				if (MyAPIGateway.Utilities.FileExistsInLocalStorage(FILE, typeof(HeatContainerWrapper)) && !l_default)
				{
					var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(FILE, typeof(HeatContainerWrapper));
					var xmlText = reader.ReadToEnd();
					reader.Close();
					//m_data.Clear();
					var savecontainer = MyAPIGateway.Utilities.SerializeFromXML<HeatContainerWrapper>(xmlText);
					loadContainer(savecontainer);
					Log.DebugWrite(DragSettings.DebugLevel.Info, "Load Complete");
					return;
				}
			}
			catch (Exception ex)
			{
				Log.DebugWrite(DragSettings.DebugLevel.Error, ex);
			}
		}

		private void loadContainer(HeatContainerWrapper savecontainer)
		{
			HeatData trash = new HeatData();
			foreach (HeatContainer savedata in savecontainer.wrapper)
			{
				
				if(data.TryGetValue(savedata.subtypeid, out trash))
				{
					data.Remove(savedata.subtypeid);
				}
				data.Add(savedata.subtypeid, savedata.value);
			}
		}

		public void Save()
		{
			Log.DebugWrite(DragSettings.DebugLevel.Info, "Saving XML (heat)");
			HeatContainerWrapper savecontainer = new HeatContainerWrapper();
			foreach(KeyValuePair<string, HeatData> kpair in data)
			{
				Log.DebugWrite(DragSettings.DebugLevel.Info, kpair.Key);
				savecontainer.wrapper.Add(new HeatContainer(kpair.Key, kpair.Value));
			}
			var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(FILE, typeof(HeatContainerWrapper));
			writer.Write(MyAPIGateway.Utilities.SerializeToXML(savecontainer));
			writer.Flush();
			writer.Close();
			Log.DebugWrite(DragSettings.DebugLevel.Info, "Save Complete");
		}
		public class HeatContainerWrapper
		{
			public List<HeatContainer> wrapper = new List<HeatContainer>();
			public HeatContainerWrapper()
			{

			}
		}
		public class HeatContainer
		{
			public string subtypeid;
			public HeatData value;
			public HeatContainer()
			{
				subtypeid = "";
			}
			public HeatContainer(string key, HeatData value)
			{
				subtypeid = key;
				this.value = value;
			}
		}

	}
}
