using Multicad.AplicationServices;
using Multicad.DatabaseServices;
using Multicad.Runtime;
using Multicad.Symbols;
using System.Collections.Generic;

// Данный пример будет работать только в рамках одной сессии
// С помощью команды мы запомним в оперативной памяти
// какие форматы следует заполнить автоматически
// и при открытии документа сохраненные форматы будут 
// заполнены

// This example will work only within one nanoCAD session
// Command SelectTitleToFill will remember title blocks
// and later on document opening fields of title blocks 
// will be automatically filled in with pre-stored values
// thus imitating the work with document management system

namespace Multicad.Examples
{
	class Main : IExtensionApplication
	{
		/// <summary>
		/// Инициализация. Регистрация команд
		/// Init and register commands
		/// </summary>
		public void Initialize()
		{
			// Нам нужна команда которая будет запоминать какие форматы следует запомнить
			// We need a command to specify which Titles to fill automatically
			McContext.RegisterCommand("SelectTitleToFill", SelectTitleToFill, CommandFlags.NoCheck | CommandFlags.NoPrefix);

			// Атрибуты форматов будут заполняться при открытии документа
			GlobalEvents.DocumentOpened += GlobalEvents_DocumentOpened;
		}

		/// <summary>
		/// Обработка на открытии документа
		/// Document opened event handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void GlobalEvents_DocumentOpened(object sender, DocumentEventArgs e)
		{
			// Filter to get objects
			ObjectFilter filter = new ObjectFilter();

			// Set filter to find objects only in opened document
			filter.AddDoc(e.Document);

			// Set filter to search only title blocks
			filter.AddType(McFormat.TypeID);

			// Do get TitleBlocks ids from drawing
			foreach (var id in filter.GetObjects())
			{
				// Almost any Multicad object can be casted to PropertySource
				// I.e. list of properties user sees in inspector
				var propertySource = id.GetObject()?.Cast<McPropertySource>();
				if (propertySource == null)
					continue;

				// We will fill "Designation" property of titleBlock with pre-stored value
				if (Main.TitleBlockExternalValues.TryGetValue(id, out string externalValue))
					propertySource.ObjectProperties["Designation"] = externalValue;
			}
		}

		/// <summary>
		/// Словарь идентификатор формата - заполняемое значение.
		/// TitleBlock id - filled value dictionary
		/// </summary>
		public static Dictionary<McObjectId, string> TitleBlockExternalValues = new Dictionary<McObjectId, string>();

		/// <summary>
		/// Команда
		/// </summary>
		public void SelectTitleToFill()
		{
			var selectedIds = McObjectManager.SelectObjects("Выберите форматы");

			// Номера форматов
			// title block numbers
			int i = 0;
			foreach (var id in selectedIds)
			{
				// Если выбранный объект - формат
				// If selected object is TitleBlock
				var drawingTitle = id.GetObject()?.Cast<McFormat>();
				if (drawingTitle == null)
					continue;

				// То запомним его id в приложении, попутно назначим заполняемое значение
				// Than we store its id in application along with the automatically
				// (supposedly externally) generated value to fill.
				if (!Main.TitleBlockExternalValues.ContainsKey(id))
					Main.TitleBlockExternalValues.Add(id, "TitleBlock" + i++);
			}
		}

		public void Terminate()
		{
			// pass
		}
	}

}
