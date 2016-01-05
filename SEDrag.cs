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
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using ParallelTasks;
using SEDrag.Definition;
using Sandbox.Common.ObjectBuilders.VRageData;

namespace SEDrag
{

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
	public class SEDrag : MyGameLogicComponent
	{
		private MyObjectBuilder_EntityBase objectBuilder;
		private int resolution = 200;
		private IMyCubeGrid grid = null;
		private MyCubeGrid mGrid = null;
		private BoundingBox dragBox;
		private bool init = false;
		private bool initcomplete = false;
		private bool dirty = false;//we force an update in Init
		private int lastupdate = 0;
		private Vector3D centerOfLift = Vector3.Zero;


		//private Dictionary<allside, IMySlimBlock> parimeterBlocks = new Dictionary<allside, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_xmax = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_ymax = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_zmax = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_xmin = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_ymin = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_zmin = new Dictionary<side, IMySlimBlock>();

		private Dictionary<IMySlimBlock, IMyEntity> m_xmax_burn = new Dictionary<IMySlimBlock, IMyEntity>();
		private Dictionary<IMySlimBlock, IMyEntity> m_ymax_burn = new Dictionary<IMySlimBlock, IMyEntity>();
		private Dictionary<IMySlimBlock, IMyEntity> m_zmax_burn = new Dictionary<IMySlimBlock, IMyEntity>();
		private Dictionary<IMySlimBlock, IMyEntity> m_xmin_burn = new Dictionary<IMySlimBlock, IMyEntity>();
		private Dictionary<IMySlimBlock, IMyEntity> m_ymin_burn = new Dictionary<IMySlimBlock, IMyEntity>();
		private Dictionary<IMySlimBlock, IMyEntity> m_zmin_burn = new Dictionary<IMySlimBlock, IMyEntity>();

		private Dictionary<side, string> m_s_xmax = new Dictionary<side, string>();
		private Dictionary<side, string> m_s_ymax = new Dictionary<side, string>();
		private Dictionary<side, string> m_s_zmax = new Dictionary<side, string>();
		private Dictionary<side, string> m_s_xmin = new Dictionary<side, string>();
		private Dictionary<side, string> m_s_ymin = new Dictionary<side, string>();
		private Dictionary<side, string> m_s_zmin = new Dictionary<side, string>();

		private Dictionary<side, MyBlockOrientation> m_o_xmax = new Dictionary<side, MyBlockOrientation>();
		private Dictionary<side, MyBlockOrientation> m_o_ymax = new Dictionary<side, MyBlockOrientation>();
		private Dictionary<side, MyBlockOrientation> m_o_zmax = new Dictionary<side, MyBlockOrientation>();
		private Dictionary<side, MyBlockOrientation> m_o_xmin = new Dictionary<side, MyBlockOrientation>();
		private Dictionary<side, MyBlockOrientation> m_o_ymin = new Dictionary<side, MyBlockOrientation>();
		private Dictionary<side, MyBlockOrientation> m_o_zmin = new Dictionary<side, MyBlockOrientation>();

		private GridHeatData heat = new GridHeatData();
		//private double heat_f = 0;
		//private double heat_b = 0;
		//private double heat_l = 0;
		//private double heat_r = 0;
		//private double heat_u = 0;
		//private double heat_d = 0;

		private bool m_showlight = false;
		private bool dontUpdate = false;
		private double drag = 0;
		private Random m_rand = new Random((int)(DateTime.UtcNow.ToBinary()));
		private IMyEntity lightEntity;
		private IMyEntity centerEntity;
		private IMyEntity massEntity;
		private int tick = 0;
		private double heatDelta = 0;
		private double heatCache = 0;
		private Task task;


		private int burnfcnt = 0;
		private int burnbcnt = 0;
		private int burnucnt = 0;
		private int burndcnt = 0;
		private int burnlcnt = 0;
		private int burnrcnt = 0;

		private float small_max
		{
			get
			{
				return Core.instance.small_max;
			}
		}
		private float large_max
		{
			get
			{
				return Core.instance.large_max;
			}

		}

		private bool showcenter
		{
			get
			{
				return Core.instance.showCenterOfLift;
			}
		}

		private bool showsmoke
		{
			get
			{
				return Core.instance.settings.showsmoke;
			}
		}
		private bool showlight
		{
			get
			{
				if (Core.instance.settings.showburn)
					return m_showlight;
				else
					return false;
			}
			set
			{
				m_showlight = value;
			}
		}
		public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
		{
			return copy ? (MyObjectBuilder_EntityBase)objectBuilder.Clone() : objectBuilder;
		}
		public void Update()
		{

		}
		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			this.objectBuilder = objectBuilder;
			Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
			//task = MyAPIGateway.Parallel.Start(refreshDragBox);
			dirty = true;//
			//Entity.Flags |= EntityFlags.Sync;
        }

		private void handleSplit(MyCubeGrid orig, MyCubeGrid newgrid)
		{

			if(orig.EntityId == Entity.EntityId)
			{
				int totalblocks = orig.BlocksCount + newgrid.BlocksCount;
				GridHeatData data = new GridHeatData();
				//float factor = newgrid.BlocksCount / totalblocks;
				data = heat;// / factor;
				//factor = orig.BlocksCount / totalblocks;
				//heat /= factor;
				Core.instance.heatTransferCache.Add(newgrid.EntityId, data);
			}


		}

		private void refreshDragBox()
		{
			if (dontUpdate) return;
			List<IMySlimBlock> blocks = new List<IMySlimBlock>();
			HashSet<side> lx = new HashSet<side>();
			HashSet<side> ly = new HashSet<side>();
			HashSet<side> lz = new HashSet<side>();
			Dictionary<side, IMySlimBlock> o_xmax = new Dictionary<side, IMySlimBlock>();
			Dictionary<side, IMySlimBlock> o_ymax = new Dictionary<side, IMySlimBlock>();
			Dictionary<side, IMySlimBlock> o_zmax = new Dictionary<side, IMySlimBlock>();
			Dictionary<side, IMySlimBlock> o_xmin = new Dictionary<side, IMySlimBlock>();
			Dictionary<side, IMySlimBlock> o_ymin = new Dictionary<side, IMySlimBlock>();
			Dictionary<side, IMySlimBlock> o_zmin = new Dictionary<side, IMySlimBlock>();

			Dictionary<side, string> s_xmax = new Dictionary<side, string>();
			Dictionary<side, string> s_ymax = new Dictionary<side, string>();
			Dictionary<side, string> s_zmax = new Dictionary<side, string>();
			Dictionary<side, string> s_xmin = new Dictionary<side, string>();
			Dictionary<side, string> s_ymin = new Dictionary<side, string>();
			Dictionary<side, string> s_zmin = new Dictionary<side, string>();
			Vector3D _centerOfLift = Vector3D.Zero;

			Dictionary<side, MyBlockOrientation> d_xmax;
			Dictionary<side, MyBlockOrientation> d_ymax;
			Dictionary<side, MyBlockOrientation> d_zmax;
			Dictionary<side, MyBlockOrientation> d_xmin;
			Dictionary<side, MyBlockOrientation> d_ymin;
			Dictionary<side, MyBlockOrientation> d_zmin;

			double xadj = 0;
			double yadj = 0;
			double zadj = 0;

			Vector3D center = Vector3D.Zero;

			//Dictionary<allside, IMySlimBlock> parim = new Dictionary<allside, IMySlimBlock>();
			try
			{


				//only call when blocks are added/removed

				//double cx, cy, cz = 0;
				Vector3D comw = Entity.Physics.CenterOfMassWorld - Entity.GetPosition();


				double t_x = 0;
				double t_y = 0;
				double t_z = 0;
				IMySlimBlock t;
				bool ignore = false;
				//MyAPIGateway.Utilities.ShowMessage(Core.NAME, "realcenter: " + comw.ToString());
				grid.GetBlocks(blocks, delegate (IMySlimBlock e)
				{
					if (ignore)
						return false;
					if (e.FatBlock != null)
					{
						if (e.FatBlock.BlockDefinition.SubtypeId == "lightDummy" ||
							e.FatBlock.BlockDefinition.SubtypeId == "batteryDummy" ||
							e.FatBlock.BlockDefinition.SubtypeName == "Small_centerofmassghost" || 
							e.FatBlock.BlockDefinition.SubtypeName == "Large_centerofmassghost" || 
							e.FatBlock.BlockDefinition.SubtypeName == "Small_centerofliftghost" || 
							e.FatBlock.BlockDefinition.SubtypeName == "Large_centerofliftghost")
						{
							ignore = true;
							return false;
						}
					}

                    var x = new side(e.Position.Y, e.Position.Z);
					var y = new side(e.Position.X, e.Position.Z);
					var z = new side(e.Position.Y, e.Position.X);

					if (!lx.Contains(x))
					{
						lx.Add(x);
						o_xmax.Add(x, e);
						o_xmin.Add(x, e);
					}
					else
					{

						if (o_xmax.TryGetValue(x, out t))
						{
							if (t.Position.X > e.Position.X)
							{
								o_xmax.Remove(x);
								o_xmax.Add(x, e);
							}
						}
						if (o_xmin.TryGetValue(x, out t))
						{
							if (t.Position.X < e.Position.X)
							{
								o_xmin.Remove(x);
								o_xmin.Add(x, e);
							}
						}
					}

					if (!ly.Contains(y))
					{
						ly.Add(y);
						o_ymax.Add(y, e);
						o_ymin.Add(y, e);
					}
					else
					{
						if (o_ymax.TryGetValue(y, out t))
						{
							if (t.Position.Y > e.Position.Y)
							{
								o_ymax.Remove(y);
								o_ymax.Add(y, e);
							}
						}
						if (o_ymin.TryGetValue(y, out t))
						{
							if (t.Position.Y < e.Position.Y)
							{
								o_ymin.Remove(y);
								o_ymin.Add(y, e);
							}
						}
					}
					if (!lz.Contains(z))
					{
						lz.Add(z);
						o_zmax.Add(z, e);
						o_zmin.Add(z, e);
					}
					else
					{
						if (o_zmax.TryGetValue(z, out t))
						{
							if (t.Position.Z > e.Position.Z)
							{
								o_zmax.Remove(z);
								o_zmax.Add(z, e);
							}
						}
						if (o_zmin.TryGetValue(z, out t))
						{
							if (t.Position.Z < e.Position.Z)
							{
								o_zmin.Remove(z);
								o_zmin.Add(z, e);
							}
						}
					}
					return false;
				});
				if (ignore)
				{
					MyAPIGateway.Utilities.InvokeOnGameThread(() =>
					{
						dontUpdate = true;
						dirty = true;
					});
					return;
				}

				center = WorldtoGrid(Entity.Physics.CenterOfMassWorld);

				xadj = center.X;
				yadj = center.Y;
				zadj = center.Z;
				//get parimeter blocks

				subtypeCache(ref o_xmax, out s_xmax, out d_xmax);
				subtypeCache(ref o_ymax, out s_ymax, out d_ymax);
				subtypeCache(ref o_zmax, out s_zmax, out d_zmax);
				subtypeCache(ref o_xmin, out s_xmin, out d_xmin);
				subtypeCache(ref o_ymin, out s_ymin, out d_ymin);
				subtypeCache(ref o_zmin, out s_zmin, out d_zmin);

				var bb = new BoundingBox(Vector3.Zero, new Vector3(Math.Sqrt(lx.Count), Math.Sqrt(ly.Count), Math.Sqrt(lz.Count)) * (grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f));// * (grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f)
				dragBox = new BoundingBox(-bb.Center, bb.Center);//center the box
				
				calculateArea(ref t_x, ref t_y, ref t_z, ref o_xmax, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref o_xmin, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref o_ymax, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref o_ymin, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref o_zmax, ref xadj, ref yadj, ref zadj);
				calculateArea(ref t_x, ref t_y, ref t_z, ref o_zmin, ref xadj, ref yadj, ref zadj);

				_centerOfLift = new Vector3D(calcCenter(t_x, lx.Count), calcCenter(t_y, ly.Count), calcCenter(t_z, lz.Count));
				if (Math.Abs(_centerOfLift.X) < grid.GridSize) _centerOfLift.X = 0;
				if (Math.Abs(_centerOfLift.Y) < grid.GridSize) _centerOfLift.Y = 0;
				if (Math.Abs(_centerOfLift.Z) < grid.GridSize) _centerOfLift.Z = 0;
				//_centerOfLift = Vector3D.Multiply(_centerOfLift, (grid.GridSizeEnum == MyCubeSize.Small ? 0.5d : 2.5d));
				//centerOfLift += new Vector3D((grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f));

			}
			catch (Exception ex)
			{
				MyAPIGateway.Utilities.InvokeOnGameThread(() =>
				{
					dirty = true;//failed update
				});
				return;
			}

			MyAPIGateway.Utilities.InvokeOnGameThread(() => {
				try
				{
					centerOfLift = _centerOfLift;
					m_xmax = o_xmax;
					m_xmin = o_xmin;
					m_ymax = o_ymax;
					m_ymin = o_ymin;
					m_zmax = o_zmax;
					m_zmin = o_zmin;
					m_s_xmax = s_xmax;
					m_s_ymax = s_ymax;
					m_s_zmax = s_zmax;
					m_s_xmin = s_xmin;
					m_s_ymin = s_ymin;
					m_s_zmin = s_zmin;

					m_o_xmax = d_xmax;
					m_o_ymax = d_ymax;
					m_o_zmax = d_zmax;
					m_o_xmin = d_xmin;
					m_o_ymin = d_ymin;
					m_o_zmin = d_zmin;

					Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("Entity ID: {0} Update:", Entity.EntityId));
					Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("  center: {1}",Entity.EntityId, center.ToString()));
					Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("  {0} Max: {1} - {0} Min: {2} Adj: {3}", "X", grid.Max.X, grid.Min.X, xadj));
					Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("  {0} Max: {1} - {0} Min: {2} Adj: {3}", "Y", grid.Max.Y, grid.Min.Y, yadj));
					Log.DebugWrite(DragSettings.DebugLevel.Info, string.Format("  {0} Max: {1} - {0} Min: {2} Adj: {3}", "Z", grid.Max.Z, grid.Min.Z, zadj));
					Log.DebugWrite(DragSettings.DebugLevel.Info, "  CenterOfLift_X: " + centerOfLift.X.ToString());
					Log.DebugWrite(DragSettings.DebugLevel.Info, "  CenterOfLift_Y: " + centerOfLift.Y.ToString());
					Log.DebugWrite(DragSettings.DebugLevel.Info, "  CenterOfLift_Z: " + centerOfLift.Z.ToString());
				}
				catch (Exception ex)
				{
					dirty = true;
					Log.DebugWrite(DragSettings.DebugLevel.Error, ex);
				}
            });
			


		}

		private void calculateArea(ref double t_x, ref double t_y, ref double t_z, ref Dictionary<side, IMySlimBlock> side, ref double xadj, ref double yadj, ref double zadj)
		{
			foreach (KeyValuePair<side, IMySlimBlock> entry in side)
			{
				//add them up
				double x = entry.Value.Position.X - xadj;
				double y = entry.Value.Position.Y - yadj;
				double z = entry.Value.Position.Z - zadj;
				t_x += x;
				t_y += y;
				t_z += z;

			}
		}
		private double calcCenter(double t, int cnt)
		{
			if (cnt == 0) return 0.0f;
			return Math.Sqrt(Math.Abs(t / cnt)) * (t > 0 ? 1 : -1);
		}
		private void subtypeCache(ref Dictionary<side, IMySlimBlock> input, out Dictionary<side, string> result, out Dictionary<side, MyBlockOrientation> directionresult)
		{
			var cont = new Dictionary<side, string>();
			var dir = new Dictionary<side, MyBlockOrientation>();
			foreach (KeyValuePair<side, IMySlimBlock> kpair in input)
			{
				var obj = kpair.Value.GetCopyObjectBuilder();
				cont.Add(kpair.Key, obj.SubtypeName);
                dir.Add(kpair.Key, obj.BlockOrientation);
			}
			result = cont;
			directionresult = dir;
		}

		void refreshCenterOfLift()
		{
			try
			{
				if (grid.IsStatic) return;
				if (showcenter && !dontUpdate && initcomplete)
				{
					if (centerEntity == null || centerEntity.Closed)
					{
						var prefab = MyDefinitionManager.Static.GetPrefabDefinition((grid.GridSizeEnum == MyCubeSize.Small ? "SmCenterOfLiftGhost" : "LgCenterOfLiftGhost"));
						var p_grid = prefab.CubeGrids[0];

						Vector3D pos = grid.Physics.CenterOfMassWorld;

						p_grid.PositionAndOrientation = new VRage.MyPositionAndOrientation( pos, grid.LocalMatrix.Forward, grid.LocalMatrix.Up );
						p_grid.LinearVelocity = Entity.Physics.LinearVelocity;
                        MyAPIGateway.Entities.RemapObjectBuilder(p_grid);
						centerEntity = MyAPIGateway.Entities.CreateFromObjectBuilder(p_grid);
						centerEntity.CastShadows = false;
						centerEntity.Flags |= EntityFlags.Visible;
						centerEntity.Flags &= ~EntityFlags.Save;//do not save
						centerEntity.Flags &= ~EntityFlags.Sync;//do not sync
						centerEntity.Physics.Enabled = false;
						MyAPIGateway.Entities.AddEntity(centerEntity);
					}
					else
					{
						
						if (centerEntity is IMyCubeGrid)
						{
							var lgrid = (IMyCubeGrid)centerEntity;
							//List<IMySlimBlock> l = new List<IMySlimBlock>();
							Vector3D pos = Vector3D.Transform( Vector3D.Multiply((WorldtoGrid(Entity.Physics.CenterOfMassWorld) + centerOfLift), grid.GridSize), grid.WorldMatrix);
							MatrixD mat = new MatrixD(grid.WorldMatrix);
							mat.Translation = pos;
                            lgrid.SetWorldMatrix(mat);
						}
						else
							centerEntity.Close();
					}
					//Log.DebugWrite(DragSettings.DebugLevel.Custom, string.Format("mCheck: {0} {1}", massEntity == null, (massEntity == null || massEntity.Closed)));
					if (massEntity == null || massEntity.Closed)
					{
						//var mdef = MyDefinitionManager.Static.GetPrefabDefinitions();
						var mprefab = MyDefinitionManager.Static.GetPrefabDefinition((grid.GridSizeEnum == MyCubeSize.Small ? "SmCenterOfMassGhost" : "LgCenterOfMassGhost"));
						var m_grid = mprefab.CubeGrids[0];
						Vector3D pos = grid.Physics.CenterOfMassWorld;
			
                        m_grid.PositionAndOrientation = new VRage.MyPositionAndOrientation(pos, grid.LocalMatrix.Forward, grid.LocalMatrix.Up);
						m_grid.LinearVelocity = Entity.Physics.LinearVelocity;
						MyAPIGateway.Entities.RemapObjectBuilder(m_grid);
						massEntity = MyAPIGateway.Entities.CreateFromObjectBuilder(m_grid);
						//massEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(p_grid);
						//Log.DebugWrite(DragSettings.DebugLevel.Custom, string.Format("Check2: {0} ", massEntity.EntityId));
						massEntity.CastShadows = false;
						massEntity.Flags |= EntityFlags.Visible;
						massEntity.Flags &= ~EntityFlags.Save;//do not save
						massEntity.Flags &= ~EntityFlags.Sync;//do not sync
						massEntity.Physics.Enabled = false;	
						MyAPIGateway.Entities.AddEntity(massEntity);
					}
					else
					{

						if (massEntity is IMyCubeGrid)
						{
							var mgrid = (IMyCubeGrid)massEntity;
							//List<IMySlimBlock> l = new List<IMySlimBlock>();
							Vector3D pos = grid.Physics.CenterOfMassWorld;
							MatrixD mat = new MatrixD(grid.WorldMatrix);
							mat.Translation = pos;
							//mat.Up = new Vector3D(0, 1, 0);
							mgrid.SetWorldMatrix(mat);
							//mgrid.SetColorMaskForSubparts(new Vector3(1));
						}
						else
							massEntity.Close();

					}
				}
				else
				{
					if (centerEntity != null && !centerEntity.Closed)
						centerEntity.Close();
					if (massEntity != null && !massEntity.Closed)
						massEntity.Close();
				}
			}
			catch (Exception ex)
			{
				//MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0}", ex.Message));
				Log.DebugWrite(DragSettings.DebugLevel.Error, "Error in refreshCenterOfLift");
			}
		}
		Vector3D WorldtoGrid(Vector3D coords)
		{
			Vector3D localCoords = Vector3D.Transform(coords, grid.WorldMatrixNormalizedInv);
			localCoords /= grid.GridSize;
			return localCoords;
		}
		void refreshLightGrid()
		{
			//Log.DebugWrite(DragSettings.DebugLevel.Custom, string.Format("RLG Entity ID {0}", Entity.EntityId));
            try
			{
				if (grid.IsStatic) return;
				if (showlight)
				{
					//Log.DebugWrite(DragSettings.DebugLevel.Custom, string.Format("sh"));
					if (lightEntity == null || lightEntity.Closed)
					{
						//Log.DebugWrite(DragSettings.DebugLevel.Custom, string.Format("spawn"));
						var def = MyDefinitionManager.Static.GetPrefabDefinitions();
						var prefab = MyDefinitionManager.Static.GetPrefabDefinition("LightDummy");// LgCenterOfLiftGhost
						var p_grid = prefab.CubeGrids[0];
						p_grid.PositionAndOrientation = new VRage.MyPositionAndOrientation(grid.Physics.CenterOfMassWorld + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 20), -Entity.WorldMatrix.Forward, Entity.WorldMatrix.Up);
						p_grid.LinearVelocity = Entity.Physics.LinearVelocity;
						MyAPIGateway.Entities.RemapObjectBuilder(p_grid);
						lightEntity = MyAPIGateway.Entities.CreateFromObjectBuilder(p_grid);
						lightEntity.CastShadows = false;
						lightEntity.Flags |= EntityFlags.Visible;
						lightEntity.Flags &= ~EntityFlags.Save;//do not save
						lightEntity.Flags |= EntityFlags.DrawOutsideViewDistance;
						if(lightEntity.Flags.HasFlag(EntityFlags.SkipIfTooSmall))
						{
							lightEntity.Flags &= ~EntityFlags.SkipIfTooSmall;//do not skip
						}
						
						lightEntity.Physics.Enabled = false;
						MyAPIGateway.Entities.AddEntity(lightEntity);
					}

					if (lightEntity != null && lightEntity is IMyCubeGrid)
					{
						//Log.DebugWrite(DragSettings.DebugLevel.Custom, string.Format("move"));
						var lgrid = (IMyCubeGrid)lightEntity;
						List<IMySlimBlock> l = new List<IMySlimBlock>();
						Vector3 pos = (grid.Physics.CenterOfMassWorld + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), grid.LocalAABB.HalfExtents.Length()));
						var block = grid.RayCastBlocks(pos, (grid.GetPosition() - Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), grid.LocalAABB.Extents.Length())));
						if (block.HasValue)
						{
							pos = grid.GridIntegerToWorld(block.Value) + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), grid.GridSize*2);
						}
						//lgrid.SetPosition(pos);
						//if (lgrid.Physics != null)
						//	lgrid.Physics.LinearVelocity = Entity.Physics.LinearVelocity;
						//var mat = lgrid.LocalMatrix;
						//mat.Forward = Vector3.Normalize(Entity.Physics.LinearVelocity);
						//lgrid.LocalMatrix = mat;
						MatrixD mat = new MatrixD(grid.WorldMatrix);
						mat.Translation = pos;
						//mat.Forward = Vector3.Normalize(Entity.Physics.LinearVelocity);
						lgrid.SetWorldMatrix(mat);
						lgrid.GetBlocks(l, delegate (IMySlimBlock e) {

							if (e.FatBlock is IMyReflectorLight)
							{
								int delta = (int)(heatDelta / 4 > 25 ? 25 : heatDelta / 4);
								Color color = MyMath.VectorFromColor(255, (byte)(delta), 0, 100);
								
								var light = (IMyReflectorLight)e.FatBlock;
								//light.WorldMatrix = MatrixD.Rescale(new MatrixD(light.WorldMatrix), 50) ;
								light.SetValueFloat("Intensity", (float)(heatCache > 500 ? (heatCache-500) / 250 : 0));
								light.SetValueFloat("Radius", grid.LocalAABB.Extents.Length());
								light.SetValue("Color", color);

							}
							return false;
						});
					}
					else
						lightEntity.Close();
				}
				else
				{
					if (lightEntity != null && !lightEntity.Closed)
						lightEntity.Close();
				}
			}
			catch (Exception ex)
			{
				//MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0}", ex.Message));
				Log.DebugWrite(DragSettings.DebugLevel.Error, "Error in refreshLight");
			}
		}


		private void refreshBoxParallel()
		{
			if (dontUpdate) return;
			if(dirty && task.IsComplete && lastupdate <= 0)
			{
				lastupdate = 30;//2x a second if needed. Yea for other threads!
				dirty = false;
				task = MyAPIGateway.Parallel.Start(refreshDragBox, calcComplete);
            }
			else
			{
				if(dirty)
					lastupdate--;
			}
		}
		public void calcComplete()
		{
			initcomplete = true;
			//Log.DebugWrite(DragSettings.DebugLevel.Custom, "Completed! " + parimeterBlocks.Count.ToString());
		}
		private void blockChange(IMySlimBlock obj)
		{
			dirty = true;
		}
		private void init_grid()
		{
			Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Init Grid", Entity.EntityId));
			if (!init && Core.instance != null)
			{
				init = true;
				grid.OnBlockAdded += blockChange;
				grid.OnBlockRemoved += blockChange;
				grid.OnClosing += onClose;
				mGrid = (MyCubeGrid)Entity;
				mGrid.OnGridSplit += handleSplit;
				dirty = true;
				dragBox = grid.LocalAABB;
				GridHeatData temp;
				if (Core.instance.heatTransferCache.TryGetValue(Entity.EntityId, out temp))
				{
					Core.instance.heatTransferCache.Remove(Entity.EntityId);
					heat = temp;
				}
				else
				{

				}
			}
		}
		private void onClose(IMyEntity obj)
		{
			if (!task.IsComplete) task.valid = false;
			grid.OnClosing -= onClose;
			grid.OnBlockAdded -= blockChange;
			grid.OnBlockRemoved -= blockChange;
			if (lightEntity != null && !lightEntity.Closed)
				lightEntity.Close();//close our 'effect'
			if (massEntity != null && !massEntity.Closed)
				massEntity.Close();
			if (centerEntity != null && !centerEntity.Closed)
				centerEntity.Close();
			if (mGrid != null)
				mGrid.OnGridSplit -= handleSplit;
			removeBurnEffect(ref m_xmax_burn);
			removeBurnEffect(ref m_ymax_burn);
			removeBurnEffect(ref m_zmax_burn);
			removeBurnEffect(ref m_xmin_burn);
			removeBurnEffect(ref m_ymin_burn);
			removeBurnEffect(ref m_zmin_burn);
		}

		public override void UpdateBeforeSimulation()
		{
			if (dontUpdate) return;
			Log.DebugWrite(DragSettings.DebugLevel.Verbose, "UpdateBeforeSimulation()");
			if (Core.instance == null)
				return;

			if (MyAPIGateway.Utilities == null) return;

			if (grid == null)
			{
				if (Entity == null)
					return;
				if (Entity is IMyCubeGrid)
					grid = (IMyCubeGrid)Entity;
				else
					return;
			}

			if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				//fine
			}
			else
			if (!(Core.instance.isServer || MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId))
			{
				//Entity.Physics.Enabled = false;//turn off?
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format( "Not updating {0}, not controlled by current player or server.", Entity.EntityId));
				return;//save cycles
			}
			else
			{
				if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId && Entity.Physics == null)
				{
					Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0} has no physics!", Entity.EntityId));
				}

			}



			if (!init) init_grid();
			if (!init) return;
			refreshBoxParallel();

			if (mGrid.Physics == null)
			{
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0} has no physics!", Entity.EntityId));
				return;
			};
			if (Core.instance.showCenterOfLift ) showLift();
			Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Get air Density", Entity.EntityId));
			List<long> removePlanets = new List<long>();
			var dragForce = Vector3.Zero;
			float atmosphere = 0;

			try
			{
				foreach (var kv in Core.instance.planets)
				{
					var planet = kv.Value;

					if (planet.Closed || planet.MarkedForClose)
					{
						removePlanets.Add(kv.Key);
						continue;
					}

					if (planet.HasAtmosphere)
					{
						atmosphere += planet.GetAirDensity(Entity.GetPosition());
					}
				}
				if (removePlanets.Count > 0)
				{
					foreach (var id in removePlanets)
					{
						Core.instance.planets.Remove(id);
					}

					removePlanets.Clear();
				}
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Air Density: {1}", Entity.EntityId, atmosphere));
				showheat();

				
				//1370 is melt tempw

				heatLoss(atmosphere);
				overheatCheck();
				refreshLightGrid();
				refreshCenterOfLift();

				if (atmosphere < 0.05f)
					return;
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Calculating Drag", Entity.EntityId));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: {1}", Entity.EntityId, mGrid.Physics.LinearVelocity.ToString()));
				if (mGrid.Physics.LinearVelocity == Vector3.Zero)
					return;

				dragForce = -mGrid.Physics.LinearVelocity;
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Reverse Velocity Vector {1}", Entity.EntityId, dragForce.ToString()));
				Vector3 dragNormal = Vector3.Normalize(dragForce);
				MatrixD dragMatrix = MatrixD.CreateFromDir(dragNormal);
				MatrixD mat = MatrixD.Invert(Entity.WorldMatrix);
				dragMatrix = dragMatrix * mat;
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Local Reverse Velocity Normal {1}", Entity.EntityId, dragMatrix.Forward.ToString()));
				double aw = 0;
				double ah = 0;
				double ad = 0;
				double a = getArea(dragBox, Vector3.Normalize(dragMatrix.Forward), ref aw, ref ah, ref ad);
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Area {1} aw: {2}, ah: {3}, ad {4}", Entity.EntityId, a, aw, ah, ad));
				double up =      getLiftCI(ah,  Vector3.Normalize(dragMatrix.Forward).Y);
				double left =    getLiftCI(aw,  Vector3.Normalize(dragMatrix.Forward).X);
				double forward = getLiftCI(ad,  Vector3.Normalize(dragMatrix.Forward).Z);
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: CI up: {1}, right: {2}, forward {3}", Entity.EntityId, up, left, forward));
				float c = (float)(0.25d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() * a);

				float u = (float)(up *      0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared());
				float l = (float)(left *   0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared());
				float f = (float)(forward * 0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared());
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: base Drag: {1}, u: {2}, l {3}, f {4}", Entity.EntityId, c, u, l, f));
				float adj = 1;
				if (grid.GridSizeEnum == MyCubeSize.Small && small_max > 0)
					adj = 104.4f / small_max;
				if (grid.GridSizeEnum == MyCubeSize.Large && large_max > 0)
					adj = 104.4f / large_max;
				if (adj < 0.2f) adj = 0.2f;
				drag = c * Core.instance.settings.mult / 100 * adj;
                dragForce = Vector3.Multiply(dragNormal, (float)drag);
				Vector3 liftup    = Vector3.Multiply(Entity.WorldMatrix.Up,      u * Core.instance.settings.mult / 100 * adj);
				Vector3 liftleft = Vector3.Multiply(Entity.WorldMatrix.Left,	 l * Core.instance.settings.mult / 100 * adj);
				Vector3 liftforw  = Vector3.Multiply(Entity.WorldMatrix.Forward, f * Core.instance.settings.mult / 100 * adj);


				if (Core.instance.settings.advancedlift)
				{
					
					//MatrixD c_lift = MatrixD.CreateTranslation(centerOfLift);
					//c_lift *= grid.LocalMatrix.GetOrientation();
					//var lift_adj = c_lift.Translation;
					Vector3D pos = Vector3D.Zero;
					if (centerOfLift == Vector3D.Zero)
						pos = mGrid.Physics.CenterOfMassWorld;
					else
						pos = Vector3D.Transform(Vector3D.Multiply((WorldtoGrid(mGrid.Physics.CenterOfMassWorld) + centerOfLift), grid.GridSize), mGrid.WorldMatrix);
					Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Adv Lift: {1}", Entity.EntityId, (liftforw + liftleft + liftup).ToString()));
					if ((liftforw + liftleft + liftup).Length() > 10.0f)
						mGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, (liftforw + liftleft + liftup), pos, Vector3.Zero);
					Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Drag: {1}", Entity.EntityId, dragForce.ToString()));
					if (dragForce.Length() > 10.0f)
						mGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce, pos, Vector3.Zero);
				}
				else
				{
					Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Lift: {1}", Entity.EntityId, (liftforw + liftleft + liftup).ToString()));
					if ((liftforw + liftleft + liftup).Length() > 10.0f)
						mGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, (liftforw + liftleft + liftup), mGrid.Physics.CenterOfMassWorld, Vector3.Zero);
					Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Drag: {1}", Entity.EntityId, dragForce.ToString()));
					if (dragForce.Length() > 10.0f)
						mGrid.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce, mGrid.Physics.CenterOfMassWorld, Vector3.Zero);
				}
				

					

				applyHeat(-Vector3D.Multiply(Vector3D.Normalize(dragMatrix.Forward), (c + liftforw.Length() + liftleft.Length() + liftup.Length()) * Core.instance.settings.mult / 100 * adj), aw, ah, ad);
				



			}
			catch(Exception ex)
			{
				Log.DebugWrite(DragSettings.DebugLevel.Error, string.Format("Exception in drag update: {0}", ex.ToString()));

			}
		}
		private double getLiftCI(double width, float x)
		{
			double _ret = Math.Pow(width, 2) * x;
			return _ret * Math.Pow(Math.Cos(Math.Abs(x) * Math.PI) * -1 + 1, 2) / 32;// 
		}

		private double getArea(BoundingBox dragBox, Vector3 _v, ref double areawidth, ref double areaheight, ref double areadepth)
		{
			areawidth = dragBox.Width * Math.Abs(_v.X);// ((-Math.Cos((Math.Abs(_v.X)) * Math.PI) + 1) / 2);////Math.Abs(_v.X)
			areaheight = dragBox.Height * Math.Abs(_v.Y);// ((-Math.Cos((Math.Abs(_v.X)) * Math.PI) + 1) / 2);//Math.Abs(_v.Y)
			areadepth = dragBox.Depth * Math.Abs(_v.Z);// ((-Math.Cos((Math.Abs(_v.X)) * Math.PI) + 1) / 2);//Math.Abs(_v.Z)
			return Math.Pow(areawidth + areaheight + areadepth, 2);
		}
		private void applyHeat(Vector3D dragVector, double aw, double ah, double ad)
		{
			var x = dragVector.X / aw;
			var y = dragVector.Y / ah;
			var z = dragVector.Z / ad;

			double scale = 100000;
			x /= scale;
			y /= scale;
			z /= scale;
			double nheatDelta = Math.Abs(x) + Math.Abs(y) + Math.Abs(z);
			heatDelta = (heatDelta > nheatDelta ? heatDelta - 0.01 : nheatDelta);
			if (heatDelta < 0) heatDelta = 0;
            if (x > 0)
			{
				//left
				heat.left += Math.Abs(x);
			}
			if (x < 0)
			{
				//right
				heat.right += Math.Abs(x);
			}
			if (y > 0)
			{
				//up
				heat.up += Math.Abs(y);
			}
			if (y < 0)
			{
				//down
				heat.down += Math.Abs(y);
			}
			if (z < 0)
			{
				//forward
				heat.front += Math.Abs(z);
			}
			if (z > 0)
			{
				//backward
				heat.back += Math.Abs(z);
			}
			
        }

		private void heatLoss(float _atmosphere)
		{
			Log.DebugWrite(DragSettings.DebugLevel.Verbose, "heatLoss()");
            if (_atmosphere < 0.05f) _atmosphere = 0.05f;//good enough for space
			disappate(ref heat.front, _atmosphere);
			disappate(ref heat.back, _atmosphere);
			disappate(ref heat.left, _atmosphere);
			disappate(ref heat.right, _atmosphere);
			disappate(ref heat.up, _atmosphere);
			disappate(ref heat.down, _atmosphere);
		}

		private void disappate(ref double heatpart, float atmo)
		{
			heatpart -= (heatpart * 0.001f * atmo * Core.instance.settings.radMult/50);
		}

		private void showLift()
		{

			if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				return;
			}

			if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId)
			{
				
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("CenterofLift: {0:N4}, {1:N4}, {2:N4}", centerOfLift.X, centerOfLift.Y, centerOfLift.Z) );
				Core.instance.showCenterOfLift = false;
			}
		}
		private void showheat()
		{

			if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				return;
			}
			if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId)
			{
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat f: {1:N4}", Entity.EntityId, heat.front));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat b: {1:N4}", Entity.EntityId, heat.back));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat u: {1:N4}", Entity.EntityId, heat.up));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat d: {1:N4}", Entity.EntityId, heat.down));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat l: {1:N4}", Entity.EntityId, heat.left));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat r: {1:N4}", Entity.EntityId, heat.right));
			}
		}
		private void overheatCheck()
		{
			//MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat: {0:N0}", heatDelta));

			
			bool critical = false;
			bool warn = false;
			double tHeat = heat.front;

			if (heat.back > tHeat) tHeat = heat.back;
			if (heat.up > tHeat) tHeat = heat.up;
			if (heat.left > tHeat) tHeat = heat.left;
			if (heat.right > tHeat) tHeat = heat.right;
			if (heat.down > tHeat) tHeat = heat.down;

			if (tHeat > 750) critical = true;
			heatCache = tHeat;
			if (!critical)
			{
				if (tHeat > 500)
				{
					warn = true;
				}
			}
			if (warn || critical || heatDelta > 4.0 && tHeat > 400)
			{
				showlight = true;
			}
			else
				showlight = false;
			playsmoke(critical);
			if (!Core.instance.settings.heat)
				return;
			if (critical)
				doDamage();

            if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null || MyAPIGateway.Session.ControlledObject.Entity == null || MyAPIGateway.Session.ControlledObject.Entity.Parent == null)
			{
				return;
			}

			if (MyAPIGateway.Session.ControlledObject.Entity.Parent.EntityId == Entity.EntityId )
			{
				
                if (warn)
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: Warning {0:N0}", tHeat), 20, Sandbox.Common.MyFontEnum.White);
				else if (critical)
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: Critical {0:N0}", tHeat), 20, Sandbox.Common.MyFontEnum.Red);
				else if (tHeat > 250)
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: {0:N0}", tHeat), 20, Sandbox.Common.MyFontEnum.White);
			}
		}

		private void playsmoke(bool critical)
		{
			return;
			//Log.DebugWrite(DragSettings.DebugLevel.Custom, "playsmoke");
			if (showsmoke && critical)
			{
				//Log.DebugWrite(DragSettings.DebugLevel.Custom, "effects");
				if (heat.front > 750)
				{
					if (m_zmax_burn.Count == 0 || burnfcnt++ > 180)
					{
						burnfcnt = 0;
						removeBurnEffect(ref m_zmax_burn);
						createBurnEffect(m_zmax, ref m_zmax_burn);
					}
					updateBurnEffect(ref m_zmax_burn);
				}
				else
				{
					removeBurnEffect(ref m_zmax_burn);
				}
				if (heat.back > 750)
				{
					if (m_zmin_burn.Count == 0 || burnbcnt++ > 180)
					{
						burnbcnt = 0;
						removeBurnEffect(ref m_zmin_burn);
						createBurnEffect(m_zmin, ref m_zmin_burn);
					}
					updateBurnEffect(ref m_zmin_burn);
				}
				else
				{
					removeBurnEffect(ref m_zmin_burn);
				}
				if (heat.up > 750)
				{
					if (m_ymax_burn.Count == 0 || burnucnt++ > 180)
					{
						burnucnt = 0;
						removeBurnEffect(ref m_ymax_burn);
						createBurnEffect(m_ymax, ref m_ymax_burn);
					}
					updateBurnEffect(ref m_ymax_burn);
				}
				else
				{
					removeBurnEffect(ref m_ymax_burn);
				}
				if (heat.down > 750)
				{
					if (m_ymin_burn.Count == 0 || burndcnt++ > 180 )
					{
						burndcnt = 0;
						removeBurnEffect(ref m_ymin_burn);
						createBurnEffect(m_ymin, ref m_ymin_burn);
					}
					updateBurnEffect(ref m_ymin_burn);
				}
				else
				{
					removeBurnEffect(ref m_ymin_burn);
				}
				if (heat.right > 750)
				{
					if (m_xmax_burn.Count == 0 || burnrcnt++ > 180)
					{
						burnrcnt = 0;
						removeBurnEffect(ref m_xmax_burn);
						createBurnEffect(m_xmax, ref m_xmax_burn);
					}
					updateBurnEffect(ref m_xmax_burn);
				}
				else
				{
					removeBurnEffect(ref m_xmax_burn);
				}
				if (heat.left > 750)
				{
					if (m_xmin_burn.Count == 0 || burnlcnt++ > 180)
					{
						burnlcnt = 0;
						removeBurnEffect(ref m_xmin_burn);
						createBurnEffect(m_xmin, ref m_xmin_burn);
					}
					updateBurnEffect(ref m_xmin_burn);
				}
				else
				{
					removeBurnEffect(ref m_xmin_burn);
				}
			}
			else
			{
				removeBurnEffect(ref m_xmax_burn);
				removeBurnEffect(ref m_ymax_burn);
				removeBurnEffect(ref m_zmax_burn);
				removeBurnEffect(ref m_xmin_burn);
				removeBurnEffect(ref m_ymin_burn);
				removeBurnEffect(ref m_zmin_burn);
			}
		}

		private void updateBurnEffect(ref Dictionary<IMySlimBlock, IMyEntity> burn)
		{
			List<IMySlimBlock> rem = new List<IMySlimBlock>();
			foreach(KeyValuePair<IMySlimBlock, IMyEntity> kval in burn)
			{
				try
				{
					if (kval.Key.IsDestroyed)
						rem.Add(kval.Key);
					Vector3D pos = grid.GridIntegerToWorld(kval.Key.Position);
					MatrixD mat = new MatrixD(kval.Value.WorldMatrix);
					mat.Translation = pos;
					kval.Value.SetWorldMatrix(mat);
				}
				catch
				{
					try
					{

						rem.Add(kval.Key);
					}
					catch
					{
						Log.DebugWrite(DragSettings.DebugLevel.Error, "Error in update burn.");
					}
					
				}
			}
			foreach(IMySlimBlock e in rem)
			{
				IMyEntity val;
				if(burn.TryGetValue(e, out val))
                {
					if (!val.MarkedForClose)
						val.Close();
				}
				burn.Remove(e);
			}
		}

		private void removeBurnEffect(ref Dictionary<IMySlimBlock, IMyEntity> burn)
		{
			if(burn.Count > 0)
			foreach(KeyValuePair<IMySlimBlock, IMyEntity> kval in burn)
			{
				if (kval.Value == null) continue;
				if(!kval.Value.MarkedForClose)
					kval.Value.Close();//close
			}
			burn.Clear();
		}

		private void createBurnEffect(Dictionary<side, IMySlimBlock> side, ref Dictionary<IMySlimBlock, IMyEntity> burn)
		{
			try
			{

				//Log.DebugWrite(DragSettings.DebugLevel.Custom, "create burn");
				foreach (KeyValuePair<side, IMySlimBlock> kpair in side)
				{
					if (m_rand.NextDouble() < 0.25)
					{
						//Log.DebugWrite(DragSettings.DebugLevel.Custom, "spawnsmokedummy");
						//var def = MyDefinitionManager.Static.GetPrefabDefinitions();
						var prefab = MyDefinitionManager.Static.GetPrefabDefinition("SmokeDummy");
						var p_grid = prefab.CubeGrids[0];

						Vector3D pos = grid.GridIntegerToWorld(kpair.Value.Position);

						p_grid.PositionAndOrientation = new VRage.MyPositionAndOrientation(pos, grid.LocalMatrix.Forward, grid.LocalMatrix.Up);
						p_grid.LinearVelocity = Entity.Physics.LinearVelocity;
						MyAPIGateway.Entities.RemapObjectBuilder(p_grid);
						IMyEntity nGrid = MyAPIGateway.Entities.CreateFromObjectBuilder(p_grid);

						nGrid.CastShadows = false;
						nGrid.Flags |= EntityFlags.Visible;
						nGrid.Flags &= ~EntityFlags.Save;//do not save
						nGrid.Flags &= ~EntityFlags.Sync;//do not sync
						nGrid.Physics.Enabled = false;
						MyAPIGateway.Entities.AddEntity(nGrid);
						burn.Add(kpair.Value, nGrid);
						//var bgrid = (IMyCubeGrid)nGrid;
						//var block = bgrid.GetCubeBlock(new Vector3I(0));
						//block.FatBlock.SetDamageEffect(true);
					}
				}
			}
			catch
			{

			}

		}

		private void doDamage()
		{
			tick++;
			if (tick < 50) return;

			tick = 0;
			if (heat.front > 750)
				applyDamage(heat.front, ref m_zmax, m_s_zmax, m_o_zmax,Base6Directions.Direction.Forward);
			if (heat.back > 750)
				applyDamage(heat.back, ref m_zmin, m_s_zmin, m_o_zmin, Base6Directions.Direction.Backward);
			if (heat.left > 750)
				applyDamage(heat.left, ref m_xmin, m_s_xmin, m_o_xmin, Base6Directions.Direction.Left);
			if (heat.right > 750)
				applyDamage(heat.right, ref m_xmax, m_s_xmax, m_o_xmax, Base6Directions.Direction.Right);
			if (heat.up > 750)
				applyDamage(heat.up, ref m_ymin, m_s_ymin, m_o_ymin, Base6Directions.Direction.Up);
			if (heat.down > 750)
				applyDamage(heat.down, ref m_ymax, m_s_ymax, m_o_ymax, Base6Directions.Direction.Down);
		}

		private void applyDamage(double dmg, ref Dictionary<side, IMySlimBlock> blocks, Dictionary<side, string> subtypeCache, Dictionary<side, MyBlockOrientation> oCache, Base6Directions.Direction dir)
		{
			if (!Core.instance.isServer)//server only
				return;

			double min = 0;
			double mult = 1.0;
			HeatData data;
			List<side> keylist = new List<side>();
			//var hit = new Sandbox.Common.ModAPI.MyHitInfo();
			//hit.Position = Entity.Physics.LinearVelocity + Entity.GetPosition();
			//hit.Velocity = -Entity.Physics.LinearVelocity;
			string subtype = "";
            foreach (KeyValuePair<side, IMySlimBlock> kpair in blocks)
			{
				//if (dirty)
				//	break;
				try
				{
					if (grid.Closed) return;
					var block = kpair.Value;
					if(block == null)
					{
						dirty = true;
						keylist.Add(kpair.Key);
						continue;
					}
					min = 750;
					mult = 1.0;
					//Log.DebugWrite(DragSettings.DebugLevel.Custom, "orig:" + dir.ToString());

					if (subtypeCache.TryGetValue(kpair.Key, out subtype))
					{
						MyBlockOrientation ndir;
						if(oCache.TryGetValue(kpair.Key, out ndir))
						{
							//Log.DebugWrite(DragSettings.DebugLevel.Custom, "ndir:" + ndir.ToString());
							Quaternion rot;
							ndir.GetQuaternion(out rot);
							switch (dir)
							{
								case Base6Directions.Direction.Forward:
									dir = Base6Directions.GetForward(rot);
									break;
								case Base6Directions.Direction.Backward:
									dir = Base6Directions.GetFlippedDirection(Base6Directions.GetForward(rot));
									break;
								case Base6Directions.Direction.Up:
									dir = Base6Directions.GetUp(rot);
									break;
								case Base6Directions.Direction.Down:
									dir = Base6Directions.GetFlippedDirection(Base6Directions.GetUp(rot));
									break;
								case Base6Directions.Direction.Right:
									dir = Base6Directions.GetLeft(Base6Directions.GetForward(rot), Base6Directions.GetUp(rot));
									break;
								case Base6Directions.Direction.Left:
									dir = Base6Directions.GetFlippedDirection(Base6Directions.GetLeft(Base6Directions.GetForward(rot), Base6Directions.GetUp(rot)));
									break;
							}
                        }
						//Log.DebugWrite(DragSettings.DebugLevel.Custom, "to - " + dir.ToString());
						if (Core.instance.h_definitions.data.TryGetValue(subtype, out data))
						{
							min = data.getHeatTresh(dir);
							mult = data.getHeatMult(dir);
						}
					}

					/*if (block is IMyOxygenTank && block.CurrentDamage > 0.5f)
					{

						var tank = (IMyOxygenTank)block;
						var inv = tank.GetInventory(0);
						//Log.DebugWrite(DragSettings.DebugLevel.Custom, string.Format("Tank found {0N2} {1:N2}", block.CurrentDamage, inv.CurrentVolume));
						
					}*/
					
					if (block.IsDestroyed)
					{
						dirty = true;
						grid.RemoveDestroyedBlock(block);
						keylist.Add(kpair.Key);
						continue;
					}
					float damage = (float)(dmg - min);
					if (damage <= 0.0d) continue;
					damage /= 100;
					damage += 1;
					damage *= (10 * (float)mult);

					var r_damage = damage * (float)m_rand.NextDouble();

					IMyDestroyableObject damagedBlock = block as IMyDestroyableObject;
					damagedBlock.DoDamage(r_damage, Sandbox.Common.ObjectBuilders.Definitions.MyDamageType.Fire, true/*, hit, 0*/);
				}
				catch
				{
					dirty = true;//need an update
					keylist.Add(kpair.Key);
					continue;
				}
			}
			foreach(side key in keylist)
			{
				blocks.Remove(key);//clear
				
			}
        }



		private struct side : IEquatable<side>
		{
			int a;
			int b;


			public side(int a, int b) : this()
			{
				this.a = a;
				this.b = b;
			}
			public bool Equals(side other)
			{
				return this.a == other.a && this.b == other.b;
			}
			public override bool Equals(Object obj)
			{
				return obj is side && this == (side)obj;
			}
			public override int GetHashCode()
			{
				return a.GetHashCode() ^ b.GetHashCode();
			}
			public static bool operator ==(side x, side y)
			{
				return x.a == y.a && x.b == y.b;
			}
			public static bool operator !=(side x, side y)
			{
				return !(x == y);
			}
		}
		private struct allside : IEquatable<allside>
		{
			int a;
			int b;
			int c;


			public allside(int a, int b, int c) : this()
			{
				this.a = a;
				this.b = b;
				this.c = c;
			}
			public bool Equals(allside other)
			{
				return this.a == other.a && this.b == other.b && this.c == other.c;
			}
			public override bool Equals(Object obj)
			{
				return obj is side && this == (allside)obj;
			}
			public override int GetHashCode()
			{
				return a.GetHashCode() ^ b.GetHashCode() ^ c.GetHashCode();
			}
			public static bool operator ==(allside x, allside y)
			{
				return x.a == y.a && x.b == y.b && x.c == y.c;
			}
			public static bool operator !=(allside x, allside y)
			{
				return !(x == y);
			}
		}

	}
}
