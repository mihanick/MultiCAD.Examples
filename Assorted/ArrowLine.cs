//-----------------------------------------------------------------------
// <copyright file="ArrowLine.cs" company="ООО Магма-Компьютер">
//     (c) ООО Магма-Компьютер, 2022
// </copyright>
// <author>mihanick@mcad.ru</author>
//-----------------------------------------------------------------------

using Multicad.AplicationServices;
using Multicad.Constants;
using Multicad.CustomObjectBase;
using Multicad.DatabaseServices;
using Multicad.DatabaseServices.StandardObjects;
using Multicad.Geometry;
using Multicad.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;


namespace Multicad.Examples
{

	[CustomEntity("AF320F79-41FC-488E-9793-30D5DA64D06E", "ArrowLine", "Полилиния со стрелкой")]
	public class ArrowLine : McOverlappedBase
	{
		#region Fields
		private Polyline3d pline = new Polyline3d();

		public Polyline3d Polyline
		{
			get => pline;
			set
			{
				if (!TryModify())
					return;

				pline = value;
			}
		}
		#endregion

		#region Constructors
		public ArrowLine() : base(Multicad.Constants.ObjectOverlays.OverlayModeEnum.Overlaping)
		{
			ZOrder = 4262;
		}
		#endregion

		#region Methods
		#endregion

		#region Grips
		public List<McSmartGrip<ArrowLine>> Grips
		{
			get
			{
				List<McSmartGrip<ArrowLine>> result = new List<McSmartGrip<ArrowLine>>();

				int i = 0;
				foreach (var vertex in this.Polyline.Points)
				{
					result.Add(new McSmartGrip<ArrowLine>(i, vertex, MoveVertex));
					i++;
				}

				return result;
			}
		}

		private static void MoveVertex(ArrowLine obj, McBaseGrip grip, Vector3d offset)
		{
			if (offset.IsZeroLength(Tolerance.Global))
				return;

			if (!obj.TryModify())
				return;

			obj.Polyline.Points[grip.Id] += offset;

			obj.DbEntity.Update();
		}

		public override bool GetGripPoints(GripPointsInfo info)
		{
			foreach (var grip in Grips)
				info.AppendGrip(grip);

			return true;
		}

		public override List<Point3d> OnGetGripPoints()
		{
			return Grips.Select(grip => grip.Position).ToList();
		}

		public override void OnMoveGripPoints(List<int> indexes, Vector3d offset, bool Stretch)
		{
			if (!Stretch)
				return;

			foreach (var index in indexes)
				MoveVertex(this, this.Grips[index], offset);
			
		}
		#endregion

		#region Draw
		private Matrix3d TransformationMatrix { get; set; }

		public override void OnTransform(Matrix3d tfm)
		{
			if (!TryModify())
				return;

			this.TransformationMatrix = tfm;


			this.Polyline.TransformBy(tfm);
		}

		public override void OnDraw(GeometryBuilder dc)
		{
			Contours.Clear();
			dc.Clear();
			dc.LineType = LineTypes.ByObject;
			
			Contours.Add(this.Polyline);

			dc.DrawPolyline(this.Polyline);
			// Последняя точка,
			// Last point
			var lastPt = this.Polyline.Points.LastPoint;

			// Point preceeding last
			// Предпоследняя точка
			var butLastPt = this.Polyline.Points[this.Polyline.Points.Count - 2];
			var arrowVec = butLastPt - lastPt;
			dc.DrawArrow(lastPt, arrowVec, Arrows.Open, 5 * this.DbEntity.Scale);
		}

		public override hresult OnUpdate()
		{
			if (!this.ID.IsNull)
			{

			}

			return base.OnUpdate();
		}
		#endregion

		#region Placement
		public override hresult PlaceObject(PlaceFlags placeFlags)
		{
			if (DbEntity.DocumentID.IsNull)
			{
				if (!DbEntity.AddToCurrentDocument())
					return hresult.e_Fail;
			}

			#region Convert pre-selection
			var selection = McObjectManager.SelectionSet.CurrentSelection;
			if (selection.Count == 1)
			{
				var pl = selection[0].GetObject()?.Cast<DbPolyline>();
				if (pl != null)
				{
					this.Polyline = pl.Polyline;
					return hresult.s_Ok;
				}
			}
			#endregion

			#region placement
			int nSkipPosition = (int)placeFlags & (int)PlaceFlags.Wout_Position;
			if (nSkipPosition == 0)
			{
				InputJig jig = new InputJig();
				var points = new List<Point3d>();
				var i = 1;

				// Объектное отслеживание от предыдущей точки
				// Otrack from previous point
				jig.DashLine = true;
				var previousPt = new Point3d();
				bool prevPtIsSet = false;

				do
				{
					InputResult resPoints = null;
					if (prevPtIsSet)
						resPoints = jig.GetPoint("Specify " + i + " point", previousPt);
					else
						resPoints = jig.GetPoint("Specify " + i + " point");

					if (resPoints.Result == InputResult.ResultCode.Cancel)
					{
						DbEntity.Erase();
						break;
					}

					if (resPoints.Result == InputResult.ResultCode.Enter)
						break;

					points.Add(resPoints.Point);

					previousPt = resPoints.Point;
					prevPtIsSet = true;

					TryModify();

					var dbPline = new DbPolyline();
					dbPline.Polyline = new Polyline3d(points);

					this.Polyline = dbPline.Polyline;

					DbEntity.Update();
					i++;
				}
				while (true);
			}
			#endregion

			return hresult.s_Ok;
		}

		public override hresult OnEdit(Point3d pnt, EditFlags lFlag)
		{
			return hresult.s_Ok;
		}
		#endregion

		#region Command
		[Multicad.Runtime.CommandMethod("ArrowLine",CommandFlags.NoCheck|CommandFlags.Redraw)]
		public static void PlaceCommand()
		{
			using (McUndoPoint up = new McUndoPoint())
			{
				up.Start();
				try
				{
					{
						ArrowLine note = new ArrowLine();

						note.DbEntity.Layer = "Polylines";
						note.DbEntity.Color = Multicad.Constants.Colors.ByObject;
						note.DbEntity.LineType = LineTypes.ByObject;
						note.DbEntity.LineWeight = LineWeights.ByObject;
						note.PlaceObject(McEntity.PlaceFlags.Normal);
					}
				}
				catch (Exception e)
				{
					McContext.ShowNotification(e.Message);
					McContext.DebugOutputMessage(e);
					up.Undo();
				}
			}
		}
		#endregion

		#region McSerialization
		public override hresult OnMcSerialization(McSerializationInfo info)
		{
			if (info == null)
				return hresult.e_InvalidArg;

			info.Add("Major", 1);
			info.Add("Minor", 1);

			// v1.1
			info.Add("Contour", this.Polyline);

			info.Add("TransformationMatrix", this.TransformationMatrix);

			return hresult.s_Ok;
		}

		public override hresult OnMcDeserialization(McSerializationInfo info)
		{
			if (info == null)
				return hresult.e_InvalidArg;

			if (!info.GetValue("Major", out int major))
				return hresult.e_MakeMeProxy;
			if (!info.GetValue("Minor", out int minor))
				return hresult.e_MakeMeProxy;
			if (major != 1)
				return hresult.e_MakeMeProxy;

			info.GetObject("Contour", this.Polyline);

			return hresult.s_Ok;
		}
		#endregion
	}
}
