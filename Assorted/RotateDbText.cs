using Multicad;
using Multicad.DatabaseServices;
using Multicad.DatabaseServices.StandardObjects;
using Multicad.Geometry;
using Multicad.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assorted
{


	public static class RotateDbText
	{
		/// <summary>
		/// Rotates DbText 90 degrees in positive direction
		/// </summary>
		[CommandMethod("rotateText", CommandFlags.NoCheck | CommandFlags.NoPrefix)]
		public static void TestDbText()
		{
			McObjectId selection = McObjectManager.SelectObject("Select Text:");

			var text = (DbText)selection.GetObject();
			var txtGeom = text.Text;

			text.Text = new TextGeom(
				text: txtGeom.Text,
				txtGeom.Origin,
				txtGeom.Direction.RotateBy(Math.PI / 2, new Vector3d(0, 0, 1)),
				txtGeom.TextStyle);

			text.DbEntity.Update();
		}
	}
}
