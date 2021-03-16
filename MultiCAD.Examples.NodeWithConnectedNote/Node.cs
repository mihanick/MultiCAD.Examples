//-----------------------------------------------------------------------
// <copyright file="Node.cs" company="ООО Магма-Компьютер">
//     (c) ООО Магма-Компьютер, 2020
// </copyright>
// <author>mihanick@mcad.ru</author>
//-----------------------------------------------------------------------
using Multicad;
using Multicad.AplicationServices;
using Multicad.Constants;
using Multicad.CustomObjectBase;
using Multicad.DatabaseServices;
using Multicad.Geometry;
using Multicad.Runtime;
using Multicad.Symbols;
using System;
using System.Collections;
using System.Collections.Generic;


// Узел представляет собой линию с ручками.
// Узел может маркироваться секущей выноской.
// Node is a line with grips
// Node can be marked with 'node section note'

namespace MultiCAD.Examples.NodeWithConnectedNote
{
	/// <summary>
	/// Действия, запускаемые на загрузке и выгрузке модуля.
	/// Entry point for loading and unloading of the application module
	/// </summary>
	class Main : IExtensionApplication
	{
		/// <summary>
		/// Словарь id выноски -> id объекта, который создал выноску.
		/// Dictionary NoteId->ObjectWithNoteId
		/// </summary>
		public static Dictionary<McObjectId, McObjectId> s_mapMarkId2MakerId = new Dictionary<McObjectId, McObjectId>();

		/// <summary>
		/// Словарь склонированных объектов, sourceID -> copiedID
		/// Dictionary of cloned objects
		/// </summary>
		public static Dictionary<McObjectId, List<McObjectId>> idsCopiedObjects = new Dictionary<McObjectId, List<McObjectId>>();


		/// <summary>
		/// Инициализация. Регистрация команд
		/// Init and register commands
		/// </summary>
		public void Initialize()
		{
			try
			{
				GlobalEvents.ObjectErased += OnObjectErased;

				// Register commands
				McContext.RegisterCommand("NodeExample", NodeAdd, CommandFlags.NoCheck | CommandFlags.NoPrefix);
			}
			catch
			{
				McContext.DebugOutputMessage("Failed initialization of module", true);
			}

			GlobalEvents.EditObjects += OnEditObjects;
			GlobalEvents.CommandStart += CommandStarted;
			GlobalEvents.ChangesStarted += ChangesStartedHandler;
		}

		/// <summary>
		/// Перемещаемые объекты
		/// Transformed objects
		/// <summary>
		public static Dictionary<McObjectId, Matrix3d> transformedObjects = new Dictionary<McObjectId, Matrix3d>();

		void CommandStarted(object sender, StringEventArgs arg)
		{
			transformedObjects.Clear();
		}

		/// <summary>
		/// Обработка выносок на изменениях
		/// Handling of notes on change
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="arg"></param>
		void ChangesStartedHandler(object sender, ChangesEventsArgs arg)
		{
			List<McObjectId> aCopied = new List<McObjectId>();
			McObjectId[] aAppended = arg.getChanged();
			try
			{
				// копирование объектов(создаём выноски для вновь созданных объектов.)
				// Copying of objects create notes for newly created objects
				foreach (var pair in idsCopiedObjects)
				{
					foreach (McObjectId anId in pair.Value)
					{
						McEntity objEntCopied = anId.GetObject() as McEntity;
						if (objEntCopied is Node)
						{
							if (!(objEntCopied is Node node))
								continue;
							node.CorrectLinks(aAppended);
							node.UpdateNote();
						}
					}
				}

				idsCopiedObjects.Clear();

				foreach (var objectId in transformedObjects.Keys)
				{
					if (aCopied.IndexOf(objectId) != -1)
						continue;
					McObject obj = objectId.GetObject();
					if (null == obj)
						continue;

					if (obj is Node)
					{
						if (!(obj is Node node))
							continue;
						node.CorrectLinks(aAppended);
						node.UpdateNote();
					}
				}
			}
			catch { }
		}


		/// <summary>
		/// Обработка выносок на удалении
		/// Handling of notes upon erase
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnObjectErased(object sender, ObjectEventArgs e)
		{
			try
			{
				McObjectId objectId;
				if (!s_mapMarkId2MakerId.TryGetValue(e.ObjectId, out objectId))
					return;
				McObject obj = objectId.GetObject();
				if (null == obj)
					return;

				#region Node notes
				if (obj is Node)
				{
					Node node = obj as Node;

					if (node.NoteType == Node.NodeNoteType.None)
						return;

					if (node.mSNoteID == e.ObjectId)
					{
						node.mSNoteID = McObjectId.Null;
						node.NoteType = Node.NodeNoteType.None;
					}
				}

				#endregion
			}
			catch
			{
				McContext.DebugOutputMessage("Failed OnObjectErased", true);
			}
		}

		/// <summary>
		/// Добавляет указанный объект в список склонированных для дальнейшей обработки
		/// Add object to list of cloned ones for consecuent handling
		/// </summary>
		/// <param name="id"></param>
		static public void RegCopiedObject(string srcStrID, McObjectId copiedID)
		{
			if (string.IsNullOrEmpty(srcStrID))
				return;
			McObjectId srcID = McObjectId.Parse(srcStrID);
			if (srcID.IsNull || copiedID.IsNull)
				return;

			if (!idsCopiedObjects.ContainsKey(srcID))
				idsCopiedObjects.Add(srcID, new List<McObjectId>());
			idsCopiedObjects[srcID].Add(copiedID);
		}

		public void Terminate()
		{
			GlobalEvents.ObjectErased -= OnObjectErased;
			GlobalEvents.EditObjects -= OnEditObjects;
			GlobalEvents.CommandStart -= CommandStarted;
			GlobalEvents.ChangesStarted -= ChangesStartedHandler;
		}

		#region Group edit
		/// <summary>
		/// Групповое редактирование объектов - создание диалогов
		/// Group editing (select -> contextmenu -> Edit)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="arg"></param>
		static void OnEditObjects(object sender, EditObjectsArgs arg)
		{

			if (arg.EditComplete)
				return;

			List<McObjectId> nodes = arg.GetObjectsByType(Node.TypeID);

			if (nodes.Count > 0)
			{

				List<Node> list = new List<Node>();
				foreach (McObjectId id in nodes)
				{
					McObject obj = id.GetObject();
					if (obj is Node node)
						list.Add(node);
				}

				if (list.Count != 0)
				{
					foreach (var nodeId in nodes)
					{
						Node node = nodeId.GetObject().Cast<Node>();
						if (!node.TryModify())
							return;
					}
					// TODO: Dialog
					// NodeWindowViewModel wm = new NodeWindowViewModel(list);
					// NodeWindow form = new NodeWindow(wm);

					// form.ShowDialog();
					arg.EditComplete = true;

					return;
				}
			}
		}
		#endregion

		#region Commands
		#region Nodes

		/// <summary>
		/// Command add node to document
		/// </summary>
		public void NodeAdd()
		{
			using (McUndoPoint up = new McUndoPoint())
			{
				up.Start();
				try
				{
					Node obj = new Node();
					obj.PlaceObject(McEntity.PlaceFlags.Normal);
				}
				catch
				{
					up.Undo();
				}
			}
		}
		#endregion
		#endregion
	}

	[CustomEntity("CAC80095-63F0-49EC-86C5-5A1F7255A7B9", "Node", "Узел")]

	/// <summary>
	/// Узел
	/// Node
	/// </summary>
	public class Node : McOverlappedBase, IMcDynamicProperties
	{
		/// <summary>
		/// Тип маркировки для узла
		/// Node marking type
		/// </summary>
		public enum NodeNoteType
		{
			/// <summary>
			/// Нет
			/// No marking
			/// </summary>
			None = 0,

			/// <summary>
			/// Узловая секущая выноска
			/// Node section note
			/// </summary>
			NodeNote = 1,

			/// <summary>
			/// Неопределено
			/// Undefined
			/// </summary>
			Undefined = int.MaxValue
		}

		#region Свойства Properties

		#region Геометрия и маркировка Geometry and marking
		/// <summary>
		/// Форма узла
		/// Node geometry form
		/// </summary>
		private LineSeg3d mNodeGeometry = new LineSeg3d();

		/// <summary>
		/// Местоположение "Номера узла".
		/// Placemet of note
		/// </summary>
		private Point3d mTextPoint = Point3d.Origin;

		/// <summary>
		/// Узловая секущая выноска.
		/// Ref to node section note
		/// </summary>
		internal McObjectId mSNoteID;

		/// <summary>
		/// Наклон полки у выноски. В радианах, принимает два значения 0 и PI.
		/// Note shelf rotation angle  either 0 or PI radians
		/// </summary>
		public double mdNoteAngle = 0;

		/// <summary>
		/// Расстояние от полочки до узла. Может быть отрицательным, если полочка находится справа от узла.
		/// Distance from note shelf to node, negative if shelf is to the right from node
		/// </summary>
		public double mdNoteDistance = 0;
		#endregion

		#region Attributes
		/// <summary>
		/// Длина, мм
		/// Length, mm
		/// </summary>
		public double Length
		{
			get
			{
				// Округлять длину узла до целых миллиметров
				// Lengths is rounded to 1 mm
				return Math.Round(this.mNodeGeometry.Length, 0, MidpointRounding.AwayFromZero);
			}
			set
			{
				if (!TryModify())
					return;
				double dParam = mNodeGeometry.ParamOf(mTextPoint, Tolerance.Global);
				Vector3d vecDirection = this.mNodeGeometry.Direction;
				this.mNodeGeometry.EndPoint = mNodeGeometry.StartPoint + value * vecDirection;
				mTextPoint = mNodeGeometry.EvalPoint(dParam);
				DbEntity.Update();
			}
		}

		/// <summary>
		/// Номер узла
		/// Name (number) of node
		/// </summary>
		public string Name { get; set; }

		private NodeNoteType mNoteType = NodeNoteType.Undefined;
		/// <summary>
		/// Тип маркировки
		/// Marking type
		/// </summary>
		public NodeNoteType NoteType
		{
			get
			{
				return this.mNoteType;
			}
			set
			{
				this.mNoteType = value;
				if (value == NodeNoteType.None)
				{
					McObjectManager.Erase(this.mSNoteID);
					mSNoteID = McObjectId.Null;
				}
			}
		}
		#endregion
		#endregion

		#region Constructors
		/// <summary>
		/// Создает узел
		/// Creates a node
		/// </summary>
		public Node()
				: base(ObjectOverlays.OverlayModeEnum.OnlyOverlap)
		{
			ZOrder = 2222;
			DbEntity.Color = Colors.ByObject;

			mdNoteDistance = 20 * McObjectManager.CurrentStyle.Scale;

			SetDefaultValues();

			// "непечатный слой"
			// Node is placed on unplotted layer
			DbEntity.Layer = McContext.GetUnplottedLayerName();
		}
		#endregion

		#region Attribute copying
		/// <summary>
		/// Копирует атрибуты с другого узла
		/// Copies attributes from the other node
		/// </summary>
		public void Copy(Node other)
		{
			this.Name = other.Name;
			this.NoteType = other.NoteType;
		}
		#endregion

		#region Grips
		/// <summary>
		/// Выполняется при таскании начальной вершины.
		/// Start grip movement
		/// </summary>
		internal static void MoveStartPoint(Node obj, McBaseGrip grip, Vector3d offset)
		{
			double dParam = obj.mNodeGeometry.ParamOf(obj.mTextPoint, Tolerance.Global);
			Point3d newSp = obj.mNodeGeometry.StartPoint + offset;
			Point3d newEp = obj.mNodeGeometry.EndPoint;
			if (!obj.TryModify()) return;
			obj.mNodeGeometry.Set(newSp, newEp);
			obj.mTextPoint = obj.mNodeGeometry.EvalPoint(dParam);
			obj.DbEntity.Update();
		}

		/// <summary>
		/// Выполняется при таскании конечной вершины.
		/// End grip movement
		/// </summary>
		internal static void MoveEndPoint(Node obj, McBaseGrip grip, Vector3d offset)
		{
			double dParam = obj.mNodeGeometry.ParamOf(obj.mTextPoint, Tolerance.Global);
			Point3d newSp = obj.mNodeGeometry.StartPoint;
			Point3d newEp = obj.mNodeGeometry.EndPoint + offset;
			if (!obj.TryModify()) return;
			obj.mNodeGeometry.Set(newSp, newEp);
			obj.mTextPoint = obj.mNodeGeometry.EvalPoint(dParam);
			obj.DbEntity.Update();
		}

		/// <summary>
		/// Выполняется при перетаскивании ручки положения обозначения
		/// Note grip movement
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="grip"></param>
		/// <param name="offset"></param>
		internal static void MoveTextGrip(Node obj, McBaseGrip grip, Vector3d offset)
		{
			if (!obj.TryModify()) return;
			obj.mTextPoint = obj.mNodeGeometry.ClosestPointTo(obj.mTextPoint + offset);
			obj.UpdateNote();
			obj.DbEntity.Update();
		}

		public override bool GetGripPoints(GripPointsInfo info)
		{
			try
			{
				if (mNodeGeometry.StartPoint.IsEqualTo(mNodeGeometry.EndPoint))
				{
					// случай, когда все три ручки лежат в одной точке.
					info.AppendGrip(new McSmartGrip<Node>(mNodeGeometry.StartPoint, MoveStartPoint));
					return true;
				}

				info.AppendGrip(new McSmartGrip<Node>(mTextPoint, MoveTextGrip));

				if (!mNodeGeometry.StartPoint.IsEqualTo(mTextPoint))
					info.AppendGrip(new McSmartGrip<Node>(mNodeGeometry.StartPoint, MoveStartPoint));

				if (!mNodeGeometry.EndPoint.IsEqualTo(mTextPoint))
					info.AppendGrip(new McSmartGrip<Node>(mNodeGeometry.EndPoint, MoveEndPoint));
				return true;
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}

			return false;
		}

		/// <summary>
		/// for stretch
		/// </summary>
		public override List<Point3d> OnGetGripPoints()
		{
			List<Point3d> result = new List<Point3d>
			{
				mNodeGeometry.StartPoint,
				mNodeGeometry.EndPoint
			};
			return result;
		}

		/// <summary>
		/// for stretch
		/// </summary>
		public override void OnMoveGripPoints(List<int> indexes, Vector3d offset, bool bStretch)
		{
			if (!bStretch)
				return;

			if (indexes.Count == 2)
			{
				double dParam = mNodeGeometry.ParamOf(mTextPoint, Tolerance.Global);
				Point3d newSp = mNodeGeometry.StartPoint + offset;
				Point3d newEp = mNodeGeometry.EndPoint + offset;
				if (!TryModify()) return;
				mNodeGeometry.Set(newSp, newEp);
				mTextPoint = mNodeGeometry.EvalPoint(dParam);
				DbEntity.Update();
				return;
			}

			if (indexes.Count != 1)
				return;

			McBaseGrip virtualGrip = new McBaseGrip();
			if (indexes[0] == 0)
			{
				virtualGrip.Position = mNodeGeometry.StartPoint;
				MoveStartPoint(this, virtualGrip, offset);
			}
			else if (indexes[0] == 1)
			{
				virtualGrip.Position = mNodeGeometry.EndPoint;
				MoveEndPoint(this, virtualGrip, offset);
			}
		}

		#endregion

		#region Drawing graphics
		/// <summary>
		/// Отрисовка объекта
		/// Object drawing
		/// </summary>
		public override void OnDraw(GeometryBuilder dc)
		{
			try
			{
				Contours.Clear();
				dc.Clear();

				dc.LineType = 5;
				dc.Color = Multicad.Constants.Colors.ByObject;
				dc.DrawLine(mNodeGeometry);
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}
		}

		/// <summary>
		/// Размещение выноски в точке mTextPoint
		/// Placement of note in mTextPoint
		/// </summary>
		public void UpdateNote()
		{
			try
			{
				McNoteSecant note = GetNote();
				if (note == null)
					return;

				if (!note.FirstPnt.IsEqualTo(mTextPoint))
					note.FirstPnt = mTextPoint;
				if (!note.SecondPnt.IsEqualTo(mTextPoint))
					note.SecondPnt = mTextPoint;
				// Rack
				Point3d pnt = mTextPoint;
				Vector3d normal = mNodeGeometry.Direction.GetPerpendicularVector();
				pnt += normal * mdNoteDistance;
				if (!note.Rack.IsEqualTo(pnt))
					note.Rack = pnt;
				SetNoteText();
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}
		}
		public override hresult OnEventEx(EventsEx evEx, object param1, object param2)
		{

			if (evEx == EventsEx.Transfered)
				if (!ID.IsNull)
				{
					string strID = ID.ToString();
					Main.RegCopiedObject(strID, ID);
				}

			return base.OnEventEx(evEx, param1, param2);
		}
		public override hresult OnEvent(Events ev, Object param)
		{
			switch (ev)
			{
				case Events.AfterAdd:
					break;

				case Events.Duplicated:
					if (!ID.IsNull)
					{
						string strID = ID.ToString();
						Main.RegCopiedObject(strID, ID);
					}
					break;
			}

			return base.OnEvent(ev, param);
		}

		public override hresult OnUpdate()
		{
			try
			{
				UpdateNote();
			}
			catch
			{
				return hresult.e_Fail;
			}
			return hresult.s_Ok;
		}

		/// <summary>
		/// Получение ссылки на существующую выноску. При необходимости выноска создаётся.
		/// Getting ref to existing note. Note is created when needed
		/// </summary>
		private McNoteSecant GetNote()
		{
			try
			{
				// виртуальный объект, для него не надо обновлять выноску.
				// Edited virtual object, no need to update note
				if (ID.IsNull)
					return null;

				// объект не вставлен в документ с помощью placeobject.
				// Object is not placed to document
				if (DbEntity.DocumentID.IsNull)
					return null;

				if (NoteType == NodeNoteType.None)
					return null;

				McNoteSecant note = null;
				if (!mSNoteID.IsNull)
					note = mSNoteID.GetObject() as McNoteSecant;
				if (note != null)
					return note;

				// выноска отсутсвует - создаём её.
				// Create note
				note = new McNoteSecant();
				if (!McObjectManager.Add2Document(note.DbEntity, this.DbEntity.Document))
					return null;
				mSNoteID = note.ID;
				mSNoteID.AddReactor(ID, false);
				Main.s_mapMarkId2MakerId[mSNoteID] = this.ID;

				return note;
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}
			return null;
		}

		/// <summary>
		/// Размещение объекта по двум точкам.
		/// Placement of node by two points
		/// </summary>
		void SetSegment(Point3d pt1, Point3d pt2)
		{
			mNodeGeometry.Set(pt1, pt2);
			mTextPoint = pt1 + (pt2 - pt1) / 2.0;
		}

		/// <summary>
		/// Обновляем угол наклона у выносок до 0 или до PI. 
		/// Запоминаем новый угол наклона выноски в узле. 
		/// Запоминаем новое расстояние полочки выноски относительно узла.
		/// 
		/// Update note shelf angle to 0 or PI
		/// Record new note rotation angle in node.
		/// Record new distance of shelf relating to node
		/// </summary>
		public void GetNotePosition(McObjectId id)
		{
			try
			{
				if (this.mSNoteID != id)
					return;
				McNoteSecant note = GetNote();

				// angle
				bool bUpdateNote = (Math.Abs(Math.Sin(note.Angle)) > Tolerance.Global.EqualPoint);
				double dAngle = note.Angle;
				if (bUpdateNote)// Угол не равен 0 и не равен PI;
				{
					if (Math.Cos(note.Angle) > 0)
						dAngle = 0;
					else
						dAngle = Math.PI;
					note.Angle = dAngle;
				}
				this.mdNoteAngle = dAngle;

				// distance
				Line3d line = new Line3d();
				line.Set(mNodeGeometry.StartPoint, mNodeGeometry.EndPoint);
				Point3d projectPt = line.ClosestPointTo(note.Rack);
				Vector3d toRack = note.Rack - projectPt;

				mdNoteDistance = toRack.Length;
				if (!toRack.IsCodirectionalTo(mNodeGeometry.Direction.GetPerpendicularVector()))
					mdNoteDistance = -mdNoteDistance;
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}
		}

		public void CorrectLinks(McObjectId[] aAppended)
		{
			if (!mSNoteID.IsNull)
			{
				McEntity entNote = mSNoteID.GetObject() as McEntity;
				List<McObjectId> rs = entNote.DbEntity.GetReactors();
				if (!rs.Contains(this.ID))
					mSNoteID = McObjectId.Null;
			}
			else
			{
				foreach (var CopObjId in aAppended)
				{
					if (!(CopObjId.GetObject() is McNoteSecant note))
						continue;
					if (note.FirstPnt.IsEqualTo(mTextPoint, Tolerance.Global))
					{
						mSNoteID = CopObjId;
						CopObjId.AddReactor(ID, false);
						Main.s_mapMarkId2MakerId[mSNoteID] = this.ID;
						break;
					}
				}
			}
		}

		public override void OnPersistentReactor(McDbObject ent, bool Erased)
		{
			try
			{
				if (!Erased)
				{
					// при изменении позиции выноски -> запоминаем новую позицию.
					// Record new position when note position changed
					GetNotePosition(ent.ID);
					UpdateNote();
				}
				else
				{
					this.DbEntity.RemoveReactor(ent.ID, true);
				}
			}
			catch { }
		}
		#endregion

		#region Placement on the drawing
		/// <summary>
		/// Вставка объекта в документ
		/// Placement on the drawing
		/// </summary>
		public override hresult PlaceObject(PlaceFlags lInsertType)
		{
			try
			{
				if (DbEntity.DocumentID.IsNull)
				{
					if (!DbEntity.AddToCurrentDocument())
						return hresult.e_Fail;
				}

				// вызов диалога редактирования.
				// Show edit dialog
				int nSkipDialog = (int)lInsertType & (int)PlaceFlags.Silent;
				if (nSkipDialog == 0)
				{
					hresult hrDialog = OnEdit(new Point3d(), EditFlags.EditByDialog);
					if (hrDialog != hresult.s_Ok)
					{
						DbEntity.Erase();
						return hresult.e_Abort;
					}
				}

				// выбор позиции
				// position selection
				int nSkipPosition = (int)lInsertType & (int)PlaceFlags.Wout_Position;
				if (nSkipPosition == 0)
				{
					InputJig jig = new InputJig();
					InputJig.PropertyInpector.SetSource(this);

					// Исключаем себя из привязки, что бы osnap точки не липли к самому себе
					// exclude this from osnap to oneself
					jig.ExcludeObject(ID);
					jig.MouseMove = (s, a) =>
					{
						TryModify();
						SetSegment(a.Point, a.Point);
						DbEntity.Update();
						InputJig.PropertyInpector.UpdateProperties();
					};
					InputResult res = jig.GetPoint("Выберите первую точку:");
					if (res.Result != InputResult.ResultCode.Normal)
					{
						DbEntity.Erase();
						return hresult.e_Fail;
					}
					Point3d firstPt = new Point3d();
					firstPt.Set(res.Point.X, res.Point.Y, res.Point.Z);

					jig.MouseMove = (s, a) =>
					{
						TryModify();
						SetSegment(firstPt, a.Point);
						DbEntity.Update();
						InputJig.PropertyInpector.UpdateProperties();
					};

					// Oбъектное отслеживание от первой точки при нанесении линии узла
					// Object snap tracking from first point of node
					res = jig.GetPoint("Выберите вторую точку:", firstPt);
					if (res.Result != InputResult.ResultCode.Normal)
					{
						DbEntity.Erase();
						return hresult.e_Fail;
					}
					SetSegment(firstPt, res.Point);
					InputJig.PropertyInpector.SetSource(null);
				}
			}
			catch
			{
				return hresult.e_Fail;
			}
			return hresult.s_Ok;
		}
		#endregion

		#region Saving and reading from the document

		/// <summary>
		/// Сохранение объекта в документ
		/// Serialization of object to the document
		/// </summary>
		public override hresult OnMcSerialization(McSerializationInfo info)
		{
			try
			{
				if (null == info)
					return hresult.e_InvalidArg;

				info.Add("Major", 1);
				info.Add("Minor", 8);

				// Object history

				// 1.1
				info.Add("NodeGeometry", mNodeGeometry);
				info.Add("TextPoint", mTextPoint);
				info.Add("SNoteID", mSNoteID);

				info.Add("Name", this.Name);

				// 1.2
				info.Add("NoteAngle", this.mdNoteAngle);
				info.Add("NoteDistance", this.mdNoteDistance);

				// 1.5 Добавлен тип маркироввки
				info.Add("NoteType", (int)this.NoteType);
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);

				return hresult.e_Fail;
			}

			return hresult.s_Ok;
		}

		/// <summary>
		/// Зачитывание объекта из документа
		/// Reading of the object from the document
		/// </summary>
		public override hresult OnMcDeserialization(McSerializationInfo info)
		{
			try
			{
				if (null == info)
					return hresult.e_InvalidArg;

				int major, minor;
				if (!info.GetValue("Major", out major)) return hresult.e_MakeMeProxy;
				if (!info.GetValue("Minor", out minor)) return hresult.e_MakeMeProxy;
				if (major != 1)
					return hresult.e_MakeMeProxy;

				// Вдруг не нашли линию узла, пожалуй можно не создавать узел
				// No need to create object if geometry not found
				if (!info.GetObject("NodeGeometry", mNodeGeometry)) return hresult.e_Fail;
				if (!info.GetValue("TextPoint", out mTextPoint))
					mTextPoint = mNodeGeometry.MidlePoint;
				if (!info.GetValue("SNoteID", out mSNoteID))
					mSNoteID = McObjectId.Null;

				string name;
				if (info.GetValue("Name", out name))
					Name = name;

				if (minor >= 2)
				{
					if (!info.GetValue("NoteAngle", out mdNoteAngle))
						mdNoteAngle = 0;
					if (!info.GetValue("NoteDistance", out mdNoteDistance))
						mdNoteDistance = 20 * McObjectManager.CurrentStyle.Scale;
				}

				if (minor >= 5)
				{
					int noteType = 1;
					if (!info.GetValue("NoteType", out noteType))
						this.NoteType = NodeNoteType.NodeNote;

					this.NoteType = (NodeNoteType)noteType;
				}
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
				return hresult.e_Fail;
			}

			return hresult.s_Ok;
		}
		#endregion

		#region Inspector properties

		/// <summary>
		/// Получение списка атрибутов.
		/// Get list of properties
		/// </summary>
		public ICollection<McDynamicProperty> GetProperties(out bool exclusive)
		{
			List<McDynamicProperty> listProps = new List<McDynamicProperty>();
			exclusive = true;
			try
			{
				List<string> aList = new List<string>
				{
					"NodeName",
					"NodeLength",
					"NodeNoteType"
				};
				foreach (string sAtrName in aList)
				{
					McDynamicProperty prop = GetProperty(sAtrName);
					if (null != prop)
						listProps.Add(prop);
				}
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}

			return listProps;
		}

		public void SetDefaultValues()
		{
			Name = "2";
			NoteType = NodeNoteType.NodeNote;
		}


		/// <summary>
		/// Получение атрибута по внутреннему имени.
		/// Get property by name
		/// </summary>
		public McDynamicProperty GetProperty(String name)
		{
			try
			{
				if (null == name)
					return null;
				PropertyImpl prop = null;
				object val = null;
				string sDispName = "";

				int nReadOnly = -1;
				if (name == "NodeName")
				{
					sDispName = "Номер узла";
					val = Name;
				}
				else if (name == "NodeLength")
				{
					sDispName = "Длина, мм";
					val = Length;
				}
				else if (name == "NodeNoteType")
				{
					sDispName = "Маркировка";
					if (NoteType == NodeNoteType.None)
						val = "Нет";
					else if (NoteType == NodeNoteType.NodeNote)
						val = "Узловая выноска";
				}

				prop = new PropertyImpl(this, name, sDispName, val);
				prop.SetReadOnly(nReadOnly);

				if (name == "NodeNoteType")
				{
					List<string> aList = new List<string> { "Нет", "Узловая выноска" };
					prop.SetValues(aList);
				}

				return prop;
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}

			return null;
		}

		/// <summary>
		/// Действия производимые при изменении названия узла. Обновление текста у выносок.
		/// Text update on changing node name
		/// </summary>
		private void SetNoteText()
		{
			try
			{
				McNoteSecant note = GetNote();
				if (note == null)
					return;
				note.Knot = Name;
			}
			catch { }
		}

		/// <summary>
		/// Присвоение атрибуту(с данным внутренним именем) новое значение.
		/// Setting property with new value
		/// </summary>
		public bool SetPropertyValue(string name, object value, ref object extStoredVal)
		{
			bool bres = false;
			try
			{
				if (!TryModify())
					return false;

				if (name == null || value == null)
					return false;
				if (name == "NodeName")
				{
					Name = Convert.ToString(value);
					extStoredVal = Name;
				}
				else if (name == "NodeLength")
				{
					double dValue = PropertyImpl.CvtToDouble(value, false);
					if (dValue > 0)
					{
						Length = dValue;
						extStoredVal = Length;
					}
				}
				else if (name == "NodeNoteType")
				{
					string sType = Convert.ToString(value);
					NodeNoteType newValue = NodeNoteType.None;
					if (sType.Equals("Нет"))
						newValue = NodeNoteType.None;
					else if (sType.Equals("Узловая выноска"))
						newValue = NodeNoteType.NodeNote;

					if (NoteType != newValue)
					{
						NoteType = newValue;
						UpdateNote();
					}
				}
				else
					return false;

				return true;
			}
			catch (Exception e)
			{
				McContext.DebugOutputMessage(e);
			}

			return bres;
		}
		#endregion

		#region Transform

		/// <summary>
		/// Действия, совершаемые при перемещении объекта
		/// Handling trainsformation of object
		/// </summary>
		public override void OnTransform(Matrix3d tfm)
		{
			try
			{
				mNodeGeometry = mNodeGeometry.TransformBy(tfm) as LineSeg3d;
				mTextPoint = mTextPoint.TransformBy(tfm);

				if (ID.IsNull)
					return;
				if (this.DbEntity.DocumentID.IsNull)
					return;

				// обновление выносок переезжает на конец команды.
				Main.transformedObjects.Add(ID, tfm);
			}
			catch { }
		}


		/// <summary>
		/// Действия при удалении объекта.
		/// Handling object erase
		/// </summary>
		public override void OnErase()
		{
			// Удаление выносок при удалении объекта.
			if (mSNoteID.IsNull)
				return;
			if (McObjectManager.GetOperationGroupForModify().Contains(mSNoteID))
				return;
			McEntity entNote = mSNoteID.GetObject() as McEntity;
			if (entNote == null)
				return;
			entNote.DbEntity.Erase();
		}
		#endregion

		#region Dialog

		/// <summary>
		/// Редактирование узла
		/// </summary>
		public override hresult OnEdit(Point3d pnt, EditFlags lFlag)
		{
			using (McUndoPoint UndoPoint = new McUndoPoint())
			{
				UndoPoint.Start(this);

				if (!this.TryModify()) return hresult.s_False;

				// TODO: Dialog VM and dialog
				// NodeWindowViewModel viewModel = new NodeWindowViewModel(this);
				// NodeWindow nodeWindow = new NodeWindow(viewModel);

				// bool? bNullableResult = nodeWindow.ShowDialog() as bool?;


				// if (bNullableResult != true)
				// {
				//	UndoPoint.Undo();
				//	return hresult.s_False;
				// }

				return hresult.s_Ok;
			}

		}
		#endregion

		public override string ToString()
		{
			return string.Format("{0}", this.Name);
		}
	}

	/// <summary>
	/// Класс свойств объектов
	/// Object property class
	/// </summary>
	class PropertyImpl : McDynamicProperty
	{
		/// <summary>
		/// Owner of property
		/// </summary>
		private object mOwner = null;

		/// <summary>
		/// Internal unique name
		/// </summary>
		private string sysName = null;

		/// <summary>
		/// User visible property name
		/// </summary>
		private string dispName = null;

		/// <summary>
		/// Property value
		/// </summary>
		private object val = null;

		/// <summary>
		/// Property category
		/// </summary>
		private string categoryName = string.Empty;

		/// <summary>
		/// Is readonly
		/// </summary>
		private int iRO = 1; // +1: yes, -1:no

		/// <summary>
		/// List of possible values
		/// </summary>
		List<string> sValuesList = null;
		ExAttributesProperties exAttr = ExAttributesProperties.None;

		public PropertyImpl(object Owner, string sysName, string dispName, object val)
		{
			mOwner = Owner;
			this.sysName = sysName;
			this.dispName = dispName ?? sysName;
			this.val = val;
		}

		public override bool IsReadOnly
		{
			get
			{
				if (iRO > 0)
					return true;
				return false;
			}
		}

		public override object GetValue()
		{
			return val;
		}

		/// <summary>
		/// Преобразовать объект к double.
		/// Convert obj to double
		/// </summary>
		public static double CvtToDouble(object val, bool bRound = false)
		{
			double dValue = 0;
			try
			{
				if (val is string)
				{
					if (!double.TryParse((string)val, out dValue))
						return 0.0;
				}
				else
					dValue = Convert.ToDouble(val);

				// round to 1mm
				if (bRound)
				{

					dValue = Math.Round(dValue, MidpointRounding.AwayFromZero);
				}
			}
			catch { }
			return dValue;
		}

		public override bool SetValue(object val)
		{
			try
			{
				if (mOwner is Node)
				{
					Node node = mOwner as Node;
					node.SetPropertyValue(Name, val, ref this.val);
				}
			}
			catch { }
			return true;
		}

		public override string DisplayName
		{
			get
			{
				if (dispName != null)
					return dispName;
				return "";
			}
		}

		public override string Category
		{
			get
			{
				if (categoryName != null)
					return categoryName;
				return "";
			}
		}

		public override Type PropertyType
		{
			get
			{
				if (val == null)
					return null;
				return val.GetType();
			}
		}

		public override string Name
		{
			get
			{
				if (sysName != null)
					return sysName;
				return "";
			}
		}

		public void SetReadOnly(int iRO)
		{
			this.iRO = iRO;
		}

		public void SetValues(List<string> vals)
		{
			sValuesList = vals;
		}

		public override ICollection GetStandardValues()
		{
			if (null != sValuesList)
				return sValuesList;
			return null;
		}

		public override McDynamicProperty.StandardValueTypeEnum GetStandardValueType()
		{
			if (sValuesList != null)
				return McDynamicProperty.StandardValueTypeEnum.Exclusive;
			return McDynamicProperty.StandardValueTypeEnum.None;
		}

		public void SetExAttributes(ExAttributesProperties exAttr)
		{
			this.exAttr = exAttr;
		}

		public override ExAttributesProperties ExtendedAtributes
		{
			get
			{
				if (IsBrowsable)
					return exAttr | ExAttributesProperties.CommandTimeProperty;
				return exAttr;
			}
		}
	}

}
