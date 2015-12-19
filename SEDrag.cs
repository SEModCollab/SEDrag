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
using ParallelTasks;

namespace SEDrag
{

	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
	public class SEDrag : MyGameLogicComponent
	{
		private MyObjectBuilder_EntityBase objectBuilder;
		private int resolution = 200;
		private IMyCubeGrid grid = null;
		private BoundingBox dragBox;
		private bool init = false;
		private bool dirty = false;//we force an update in Init
		private int lastupdate = 0;
		private Vector3D centerOfLift = Vector3.Zero;


		private Dictionary<allside, IMySlimBlock> parimeterBlocks = new Dictionary<allside, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_xmax = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_ymax = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_zmax = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_xmin = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_ymin = new Dictionary<side, IMySlimBlock>();
		private Dictionary<side, IMySlimBlock> m_zmin = new Dictionary<side, IMySlimBlock>();

		private double heat_f = 0;
		private double heat_b = 0;
		private double heat_l = 0;
		private double heat_r = 0;
		private double heat_u = 0;
		private double heat_d = 0;

		private bool showlight = false;
		private bool dontUpdate = false;
		private double drag = 0;
		private Random m_rand = new Random((int)(DateTime.UtcNow.ToBinary()));
		private IMyEntity lightEntity;
		private int tick = 0;
		private double heatDelta = 0;
		private Task task;

		public float small_max
		{
			get
			{
				return Core.instance.small_max;
			}
		}
		public float large_max
		{
			get
			{
				return Core.instance.large_max;
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
			Vector3D _centerOfLift = Vector3D.Zero;

			double xadj = 0;
			double yadj = 0;
			double zadj = 0;

			Vector3I center = Vector3I.Zero;

			Dictionary<allside, IMySlimBlock> parim = new Dictionary<allside, IMySlimBlock>();
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

					if (e is IMyInteriorLight)
					{
						var block = (IMyInteriorLight)e;
						if (block.BlockDefinition.SubtypeName == "lightDummy")
						{
						//Log.Info("IGNORING GRID!");
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
							if (t.Position.X > e.Position.X)
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
					});
					return;
				}

				center = grid.WorldToGridInteger(Entity.Physics.CenterOfMassWorld);

				xadj = center.X;
				yadj = center.Y;
				zadj = center.Z;
				//get parimeter blocks


				generateParimeter(o_xmax, ref parim);
				generateParimeter(o_xmin, ref parim);
				generateParimeter(o_ymax, ref parim);
				generateParimeter(o_ymin, ref parim);
				generateParimeter(o_zmax, ref parim);
				generateParimeter(o_zmin, ref parim);



				var bb = new BoundingBox(Vector3.Zero, new Vector3(Math.Sqrt(lx.Count), Math.Sqrt(ly.Count), Math.Sqrt(lz.Count)) * (grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f));// * (grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f)
				dragBox = new BoundingBox(-bb.Center, bb.Center);//center the box

				foreach (KeyValuePair<allside, IMySlimBlock> entry in parim)
				{
					//add them up

					t_x += entry.Value.Position.X - xadj;
					t_y += entry.Value.Position.Y - yadj;
					t_z += entry.Value.Position.Z - zadj;

				}
				_centerOfLift = new Vector3D(calcCenter(t_x, lx.Count), calcCenter(t_y, ly.Count), calcCenter(t_z, lz.Count));

				_centerOfLift = Vector3D.Multiply(_centerOfLift, (grid.GridSizeEnum == MyCubeSize.Small ? 0.5d : 2.5d));
				//centerOfLift += new Vector3D((grid.GridSizeEnum == MyCubeSize.Small ? 0.5f : 2.5f));
				if (Math.Abs(centerOfLift.X) < 1.5) _centerOfLift.X = 0;
				if (Math.Abs(centerOfLift.Y) < 1.5) _centerOfLift.Y = 0;
				if (Math.Abs(centerOfLift.Z) < 1.5) _centerOfLift.Z = 0;
			}
			catch (Exception ex)
			{
				MyAPIGateway.Utilities.InvokeOnGameThread(() =>
				{
					dirty = true;//failed update
				});

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
					parimeterBlocks = parim;

					//centerOfLift = -centerOfLift;//invert
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

		void refreshLightGrid()
		{
			try
			{
				if (showlight )
				{
					if (lightEntity == null || lightEntity.Closed)
					{
						var def = MyDefinitionManager.Static.GetPrefabDefinitions();
						var prefab = MyDefinitionManager.Static.GetPrefabDefinition("LightDummy");
						var p_grid = prefab.CubeGrids[0];
						p_grid.PositionAndOrientation = new VRage.MyPositionAndOrientation(grid.Physics.CenterOfMassWorld + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 20), -Entity.WorldMatrix.Forward, Entity.WorldMatrix.Up);
						p_grid.LinearVelocity = Entity.Physics.LinearVelocity;
                        MyAPIGateway.Entities.RemapObjectBuilder(p_grid);
                        lightEntity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(p_grid);
						lightEntity.CastShadows = false;
						lightEntity.Flags |= EntityFlags.Visible;
					}
					else
					{
						
						if (lightEntity is IMyCubeGrid)
						{
							var lgrid = (IMyCubeGrid)lightEntity;
							List<IMySlimBlock> l = new List<IMySlimBlock>();
							Vector3 pos = (grid.Physics.CenterOfMassWorld + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 20));
							var block = grid.RayCastBlocks(pos, (grid.GetPosition() - Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 200)));
							if(block.HasValue)
							{
								pos = grid.GridIntegerToWorld(block.Value) + Vector3.Multiply(Vector3.Normalize(Entity.Physics.LinearVelocity), 6);
							}
							lgrid.SetPosition(pos);
							if (lgrid.Physics != null)
								lgrid.Physics.LinearVelocity = Entity.Physics.LinearVelocity;
							var mat = lgrid.LocalMatrix;
							mat.Forward = Vector3.Normalize(Entity.Physics.LinearVelocity);
							lgrid.LocalMatrix = mat;
							lgrid.GetBlocks(l, delegate (IMySlimBlock e) {

								if (e.FatBlock is IMyReflectorLight)
								{

									//var color = MyMath.VectorFromColor(255,50,0);
									int delta = (int)(heatDelta/4 > 25 ? 25 : heatDelta/4);
									Color color = MyMath.VectorFromColor(255, (byte)(delta), 0, 100);
									var light = (IMyReflectorLight)e.FatBlock;
									/*if(e.FatBlock is Sandbox.Game.Entities.Blocks.MyLightingBlock)
									{
										var fatBlock = (Sandbox.Game.Entities.Blocks.MyLightingBlock)e.FatBlock;
										fatBlock.Intensity = Entity.Physics.LinearVelocity.Length();
										fatBlock.Falloff = 2;
									}*/
									//Log.Info("set intensity");
									light.SetValueFloat("Intensity", (float)heatDelta/2);
									light.SetValueFloat("Radius", grid.LocalAABB.Extents.Length() + 6);

									light.SetValue("Color", color);
									//light.SetColorMaskForSubparts(color);

								}
								return false;
							});
						}
						else
							lightEntity.Close();

					}
				}
				else
				{
					if (lightEntity != null && !lightEntity.Closed)
						lightEntity.Close();
				}
			}
			catch (Exception ex)
			{
				MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("{0}", ex.Message));
				//Log.Info("Error");
			}
		}

		private double calcCenter(double t, int cnt)
		{
			if (cnt == 0) return 0.0f;
			return Math.Sqrt(Math.Abs(t / cnt)) * ( t > 0 ? 1 : -1) ;
		}

		private void generateParimeter(Dictionary<side, IMySlimBlock> edge, ref Dictionary<allside, IMySlimBlock> parim)
		{
			foreach (KeyValuePair<side, IMySlimBlock> entry in edge)
			{
				allside sides = new allside(entry.Value.Position.X, entry.Value.Position.Y, entry.Value.Position.Z);
				if (!parim.ContainsKey(sides))
				{
					parim.Add(sides, entry.Value);
				}
			}
		}

		private void refreshBoxParallel()
		{
			//Log.DebugWrite(DragSettings.DebugLevel.Custom, Entity.EntityId + " " + task.IsComplete);

			if(dirty && task.IsComplete && lastupdate <= 0)
			{
				lastupdate = 60;
				dirty = false;
				task = MyAPIGateway.Parallel.Start(refreshDragBox, calcComplete);
				//MyAPIGateway.Parallel.Start()  or MyAPIGateway.Parallel.StartBackground()
            }
			else
			{
				if(dirty)
					lastupdate--;
			}
		}
		public void calcComplete()
		{
			//Log.DebugWrite(DragSettings.DebugLevel.Custom, "Completed! " + parimeterBlocks.Count.ToString());
		}
		private void blockChange(IMySlimBlock obj)
		{
			dirty = true;
		}
		private void init_grid()
		{
			Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Init Grid", Entity.EntityId));
			if (!init)
			{

				init = true;
				grid.OnBlockAdded += blockChange;
				grid.OnBlockRemoved += blockChange;
				grid.OnClosing += onClose;
				dirty = true;
				dragBox = grid.LocalAABB;

				refreshBoxParallel();
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

			if (grid.Physics == null) {
				//if (!Core.instance.isDedicated) Log.Info("Attempting to enable.");
				//Entity.Physics.Enabled = true;//attempt to enable?
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0} has no physics!", Entity.EntityId));
				return;
			};

			if (!init) init_grid();
			refreshBoxParallel();

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

				if (atmosphere < 0.05f)
					return;
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Calculating Drag", Entity.EntityId));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: {1}", Entity.EntityId, grid.Physics.LinearVelocity.ToString()));
				if (grid.Physics.LinearVelocity == Vector3.Zero)
					return;

				dragForce = -Entity.Physics.LinearVelocity;
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
				double up =      getLiftCI(dragBox.Height, Vector3.Normalize(dragMatrix.Forward).Y);
				double right =   getLiftCI(dragBox.Width,  Vector3.Normalize(dragMatrix.Forward).X);
				double forward = getLiftCI(dragBox.Depth,  Vector3.Normalize(dragMatrix.Forward).Z);
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: CI up: {1}, right: {2}, forward {3}", Entity.EntityId, up, right, forward));
				float c = (float)(0.25d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() * a);

				float u = (float)(up *      0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() );
				float l = (float)(right *   0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() );
				float f = (float)(forward * 0.5d * ((double)atmosphere * 1.225d) * (double)dragForce.LengthSquared() );
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
				Vector3 liftright = Vector3.Multiply(Entity.WorldMatrix.Right,   l * Core.instance.settings.mult / 100 * adj);
				Vector3 liftforw  = Vector3.Multiply(Entity.WorldMatrix.Forward, f * Core.instance.settings.mult / 100 * adj);


				if (Core.instance.settings.advancedlift)
				{
					
					MatrixD c_lift = MatrixD.CreateTranslation(centerOfLift);
					c_lift *= grid.LocalMatrix.GetOrientation();
					var lift_adj = c_lift.Translation;
					Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Adv Lift: {1}", Entity.EntityId, (liftforw + liftright + liftup).ToString()));
					if ((liftforw + liftright + liftup).Length() > 10.0f)
						Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -(liftforw + liftright + liftup), (Entity.WorldMatrix.Translation + c_lift.Translation), Vector3.Zero);

				}
				else
				{
					Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Lift: {1}", Entity.EntityId, (liftforw + liftright + liftup).ToString()));
					if ((liftforw + liftright + liftup).Length() > 10.0f)
						Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, -(liftforw + liftright + liftup), Entity.Physics.CenterOfMassWorld, Vector3.Zero);
				}

				//if (dragForce.Length() > grid.Physics.Mass * 100 && grid.Physics.Mass > 0)
				//	spin = Vector3.Multiply(MyUtils.GetRandomVector3Normalized(), dragForce.Length() / (grid.Physics.Mass * 100));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("Entity {0}: Drag: {1}", Entity.EntityId, dragForce.ToString()));
				if (dragForce.Length() > 10.0f)//if force is too small, forget it. 
					Entity.Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, dragForce, Entity.Physics.CenterOfMassWorld, Vector3.Zero);
				
				applyHeat(-Vector3D.Multiply(Vector3D.Normalize(dragMatrix.Forward), c * Core.instance.settings.mult / 100 * adj), aw, ah, ad);



			
			}
			catch(Exception ex)
			{
				Log.DebugWrite(DragSettings.DebugLevel.Error, string.Format("Exception in drag update: {0}", ex.ToString()));

			}
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
            if (x < 0)
			{
				//left
				heat_l += Math.Abs(x);
			}
			if (x > 0)
			{
				//right
				heat_r += Math.Abs(x);
			}
			if (y > 0)
			{
				//up
				heat_u += Math.Abs(y);
			}
			if (y < 0)
			{
				//down
				heat_d += Math.Abs(y);
			}
			if (z < 0)
			{
				//forward
				heat_f += Math.Abs(z);
			}
			if (z > 0)
			{
				//backward
				heat_b += Math.Abs(z);
			}
			overheatCheck();
        }

		private void heatLoss(float _atmosphere)
		{
			Log.DebugWrite(DragSettings.DebugLevel.Verbose, "heatLoss()");
            if (_atmosphere < 0.05f) _atmosphere = 0.05f;//good enough for space
			disappate(ref heat_f, _atmosphere, dragBox.Depth);
			disappate(ref heat_b, _atmosphere, dragBox.Depth);
			disappate(ref heat_l, _atmosphere, dragBox.Width);
			disappate(ref heat_r, _atmosphere, dragBox.Width);
			disappate(ref heat_u, _atmosphere, dragBox.Height);
			disappate(ref heat_d, _atmosphere, dragBox.Height);
		}

		private void disappate(ref double heat, float atmo, double area)
		{
			heat -= (heat * 0.001f * atmo * Core.instance.settings.radMult/50);//area should be removed this is for now. 
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
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat f: {1:N4}", Entity.EntityId, heat_f));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat b: {1:N4}", Entity.EntityId, heat_b));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat u: {1:N4}", Entity.EntityId, heat_u));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat d: {1:N4}", Entity.EntityId, heat_d));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat l: {1:N4}", Entity.EntityId, heat_l));
				Log.DebugWrite(DragSettings.DebugLevel.Verbose, string.Format("EntityID: {0} Heat r: {1:N4}", Entity.EntityId, heat_r));
			}
		}
		private void overheatCheck()
		{
			//MyAPIGateway.Utilities.ShowMessage(Core.NAME, String.Format("Heat: {0:N0}", heatDelta));

			
			bool critical = false;
			bool warn = false;
			double heat = heat_f;

			if (heat_b > heat) heat = heat_b;
			if (heat_u > heat) heat = heat_u;
			if (heat_l > heat) heat = heat_l;
			if (heat_r > heat) heat = heat_r;
			if (heat_d > heat) heat = heat_d;

			if (heat > 750) critical = true;

			if (!critical)
			{
				if (heat > 500)
				{
					warn = true;
				}
			}
			if (warn || critical || heatDelta > 4.0 && heat > 400)
			{
				showlight = true;
			}
			else
				showlight = false;
			refreshLightGrid();
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
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: Warning {0:N0}", heat), 20, Sandbox.Common.MyFontEnum.White);
				else if (critical)
				{
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: Critical {0:N0}", heat), 20, Sandbox.Common.MyFontEnum.Red);
					
				}
				else if (heat > 250)
					MyAPIGateway.Utilities.ShowNotification(String.Format("Heat Level: {0:N0}", heat), 20, Sandbox.Common.MyFontEnum.White);
			}
		}

		private void doDamage()
		{
			tick++;
			if (tick < 50) return;

			tick = 0;
			if (heat_f > 750)
			{
				applyDamage(heat_f, m_zmax);
			}
			if (heat_b > 750)
			{

				applyDamage(heat_b, m_zmin);
			}
			if (heat_l > 750)
			{

				applyDamage(heat_l, m_xmin);
			}
			if (heat_r > 750)
			{

				applyDamage(heat_r, m_xmax);
			}
			if (heat_u > 750)
			{

				applyDamage(heat_u, m_ymax);
			}
			if (heat_d > 750)
			{

				applyDamage(heat_d, m_ymin);
			}
		}

		private void applyDamage(double dmg, Dictionary<side, IMySlimBlock> blocks)
		{
			if (!Core.instance.isServer)//server only
				return;
			float damage = (float)(dmg - 750);
			damage /= 100;
			damage += 1;
			damage *= 3;
			damage *= (float)m_rand.NextDouble();
			if (damage < 0) return;
			List<side> keylist = new List<side>();
			//var hit = new Sandbox.Common.ModAPI.MyHitInfo();
			//hit.Position = Entity.Physics.LinearVelocity + Entity.GetPosition();
			//hit.Velocity = -Entity.Physics.LinearVelocity;

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
						keylist.Add(kpair.Key);
						continue;
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
					}
					grid.ApplyDestructionDeformation(block);
                    IMyDestroyableObject damagedBlock = block as IMyDestroyableObject;
					damagedBlock.DoDamage(damage, Sandbox.Common.ObjectBuilders.Definitions.MyDamageType.Fire, true/*, hit, 0*/);
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

		private double getLiftCI(float width, float x)
		{
			double _ret = Math.Pow(width, 2) * x;
			return _ret * Math.Pow(Math.Cos(Math.Abs(x) * Math.PI)*-1+1,2)/32;
		}

		private double getArea(BoundingBox dragBox, Vector3 _v, ref double areawidth, ref double areaheight, ref double areadepth)
		{
			areawidth = dragBox.Width * Math.Abs(_v.X);
			areaheight = dragBox.Height * Math.Abs(_v.Y);
			areadepth = dragBox.Depth * Math.Abs(_v.Z);
            return Math.Pow(areawidth + areaheight + areadepth, 2);
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
