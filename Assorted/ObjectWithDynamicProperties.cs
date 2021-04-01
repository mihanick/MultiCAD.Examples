using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using Multicad;
using Multicad.CustomObjectBase;
using Multicad.DatabaseServices;
using Multicad.Geometry;
using Multicad.Runtime;
using System.Globalization;
using Multicad.DatabaseServices.StandardObjects;

namespace DotNetSample
{
	[Serializable]
	public class MyProperty
	{
		public MyProperty(String name, object value, String displayName)
		{
			Name = name;
			Value = value;
			IsReadOnly = false;
			DisplayName = displayName;
		}

		public MyProperty(String name, object value, bool readOnly, String displayName)
		{
			Name = name;
			Value = value;
			IsReadOnly = readOnly;
			DisplayName = displayName;
		}

		public String Name { set; get; }
		public String DisplayName { set; get; }
		public Object Value { set; get; }
		public bool IsReadOnly { set; get; }
	}

	public class DynPropertyDescriptor : BasePropertyDescriptor
	{
		MyProperty _property;
		public DynPropertyDescriptor(MyProperty property)
			: base(property.Name, property.Value.GetType(), new Attribute[] { new CategoryAttribute("DynPropEntity"), new DisplayNameAttribute(property.DisplayName) })
		{
			_property = property;
		}

		public override bool IsReadOnly
		{
			get
			{
				return _property.IsReadOnly;
			}
		}

		public override void SetValue(object component, object value)
		{
			DynPropEntity ent = component as DynPropEntity;
			if (ent != null)
				ent.TryModify();
			_property.Value = value;
		}

		public override object GetValue(object component)
		{
			return _property.Value;
		}
	}

	[CustomEntity("9CBD869E-AC25-41a4-B9C1-7C182AFDE4A8", "DynPropEntity", "Объект с динамическим списком свойств")]
	[Serializable]
	[ContainsCommands]
	public class DynPropEntity : McCustomBase, ICustomTypeDescriptor
	{
		/// <summary>
		/// Простой способ получить ID класса, используется в конструкциях ObjectFlter.AddType(class.TypeID) или obj.IsKindOf(class.TypeID)
		/// </summary>
		public static new Guid TypeID
		{
			get
			{
				return GetTypeID(typeof(DynPropEntity));
			}
		}

		protected Point3d _pnt = new Point3d(50, 50, 0);
		protected Vector3d _vec;
		protected String _Text = "Text field";
		internal List<MyProperty> _Properties = new List<MyProperty>();

		public DynPropEntity()
		{
			onCreate(CreateObjectFlags.Default);
		}

		public DynPropEntity(Multicad.DatabaseServices.CreateObjectFlags flags)
		{
			onCreate(flags);
		}

		private void onCreate(Multicad.DatabaseServices.CreateObjectFlags flags)
		{
			//что бы рамка была соразмерна тексту при любом масштабе оформления
			_vec = new Vector3d(100 * DbEntity.Scale, 30 * DbEntity.Scale, 0);
			_Properties.Add(new MyProperty("Text", "Text field", "Текст 1"));
			_Properties.Add(new MyProperty("Text2", "Text field2", "Текст 2"));
		}

		public Point3d Origin
		{
			get
			{
				return _pnt;
			}
			set
			{
				if (!TryModify()) return;//без этого не будет сохранятся Undo и перерисовыватся объект
				_pnt = value;
			}
		}

		public override void OnDraw(GeometryBuilder dc)
		{
			dc.Clear();
			Point3d pnt2 = _pnt + _vec;
			dc.Color = Multicad.Constants.Colors.ByObject;//цвет будет братся из свойств объекта, и при изменении автоматически перерисуется
			dc.DrawPolyline(new Point3d[] { _pnt, new Point3d(_pnt.X, pnt2.Y, 0), pnt2, new Point3d(pnt2.X, _pnt.Y, 0), _pnt });
			dc.TextHeight = 2.5 * DbEntity.Scale;   //Используем масштаб оформления
			dc.Color = Color.Blue;//Текст рисуем синим цветом
			double Offset = 0;
			foreach (MyProperty pr in _Properties)
			{
				dc.DrawMText(new Point3d((pnt2.X + _pnt.X) / 2.0, (pnt2.Y + _pnt.Y) / 2.0 + Offset, 0), Vector3d.XAxis, pr.Value.ToString(), HorizTextAlign.Center, VertTextAlign.Center);
				Offset += 3 * DbEntity.Scale;
			}
		}

		public override void OnTransform(Matrix3d tfm)
		{
			if (!TryModify()) return;
			_pnt = _pnt.TransformBy(tfm);
		}
		public override List<Point3d> OnGetGripPoints()
		{
			List<Point3d> arr = new List<Point3d>();
			arr.Add(_pnt);
			arr.Add(_pnt + _vec);
			return arr;
		}

		public override void OnMoveGripPoints(List<int> indexes, Vector3d offset, bool isStretch)
		{
			if (!TryModify()) return;
			if (indexes.Count == 2)
			{
				_pnt += offset;
				_vec += offset;
			}
			else if (indexes.Count == 1)
			{
				if (indexes[0] == 0)
				{
					_pnt += offset;
				}
				else
				{
					_vec += offset;
				}
			}
		}

		public override hresult PlaceObject(PlaceFlags lInsertType)
		{
			InputJig jig = new InputJig();
			InputResult res = jig.GetPoint("Select first point:");
			if (res.Result != InputResult.ResultCode.Normal)
				return hresult.e_Fail;
			_pnt = res.Point;
			DbEntity.AddToCurrentDocument();
			//Исключаем себя из привязки, что бы osnap точки не липли к самому себе
			jig.ExcludeObject(ID);
			//Мониторинг движения мышкой
			jig.MouseMove = (s, a) => { TryModify(); _vec = a.Point - _pnt; DbEntity.Update(); };
			res = jig.GetPoint("Select second point:");
			if (res.Result != InputResult.ResultCode.Normal)
			{
				DbEntity.Erase();
				return hresult.e_Fail;
			}
			_vec = res.Point - _pnt;
			return hresult.s_Ok;
		}

		public override hresult OnEdit(Point3d pnt, EditFlags lInsertType)
		{
			return hresult.s_Ok;
		}

		[CommandMethod("smpl_DynPropObject", CommandFlags.NoCheck | CommandFlags.NoPrefix)]
		static public void smpl_CustomObjectCmd()
		{
			DynPropEntity obj = new DynPropEntity();
			obj.PlaceObject();
		}

		public System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Attribute[] attributes)
		{
			return new System.ComponentModel.PropertyDescriptorCollection(_Properties.ConvertAll<DynPropertyDescriptor>(a => new DynPropertyDescriptor(a)).ToArray());
		}

		#region Implementation of ICustomTypeDescriptor
		public object GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd)
		{
			return this;
		}
		public string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		public System.ComponentModel.TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		public System.ComponentModel.EventDescriptorCollection GetEvents(System.Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		public System.ComponentModel.EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}

		public System.ComponentModel.AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}

		public System.ComponentModel.PropertyDescriptorCollection GetProperties()
		{
			return GetProperties(new Attribute[0]);
		}
		virtual public object GetEditor(System.Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}
		public System.ComponentModel.PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}
		public System.ComponentModel.EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}
		public string GetClassName()
		{
			return TypeDescriptor.GetClassName(this, true);
		}
		#endregion

		static int iCut = 0;
		public override bool SetAsPrimitive(EntityGeometry ent, List<McObjectId> idsOnPrimitive)
		{

			if (ent.GeometryType == EntityGeomType.kPolyline)
			{
				var pline = ent.Polyline;
				if (pline == null)
					return false;

				//if (pline.Points.Count == 2)
				//	return false;

				DbPolyline p = new DbPolyline();
				p.Geometry = pline;
				p.DbEntity.LineWeight = 10;
				p.DbEntity.Color = Color.Red;
				p.DbEntity.AddToCurrentDocument();

				DbText t = new DbText();
				var tg = new TextGeom(iCut++.ToString(), pline.Points.FirstPoint, new Vector3d(1, 0, 0), "Standard");
				tg.Height = 250;
				t.Geometry = tg;
				t.DbEntity.AddToCurrentDocument();

				var pntmin = pline.Points.FirstPoint;
				var pntmax = pline.Points.FirstPoint;
				foreach (var pp in pline.Points) 
				{
					if (pp.X < pntmin.X) pntmin.X = pp.X;
					if (pp.Y < pntmin.Y) pntmin.Y = pp.Y;
					if (pp.X > pntmax.X) pntmax.X = pp.X;
					if (pp.Y > pntmax.Y) pntmax.Y = pp.Y;
				}

				var b = new BoundBlock(pntmin, pntmax);

				if (!this.TryModify())
					return false;

				this._vec.X = b.SizeByX;
				this.Origin = pntmin;
				

				return true;

			}
			else if (ent.GeometryType == EntityGeomType.kLine)
			{
				var line = ent.LineSeg;

				DbLine l = new DbLine();
				l.Geometry = new LineSeg3d(line.StartPoint, line.EndPoint);
				l.DbEntity.Color = Color.Red;
				l.DbEntity.AddToCurrentDocument();

				DbText t = new DbText();
				var tg = new TextGeom(this.DbEntity.DocumentID.ToString() + iCut++.ToString(), line.StartPoint, new Vector3d(1, 0, 0), "Standard");
				tg.Height = 250;
				t.Geometry = tg;
				t.DbEntity.AddToCurrentDocument();

				if (line == null)
					return false;

				if (!TryModify())
					return false;
				this.Origin = line.StartPoint;
				this._vec.X = line.EndPoint.X - line.StartPoint.X;
				return true;
			}
			return false;
		}

		public override EntityGeometry GetAsPrimitive()
		{


			Point3d pnt2 = _pnt + _vec;
			var pnts = new Point3d[] { _pnt, new Point3d(_pnt.X, pnt2.Y, 0), pnt2, new Point3d(pnt2.X, _pnt.Y, 0), _pnt };
			var pline = new Polyline3d(pnts);
			return pline;

			// Работает
			//var line = new LineSeg3d(_pnt, new Point3d(pnt2.X, _pnt.Y, 0));
			//return line;
		}
	}
}