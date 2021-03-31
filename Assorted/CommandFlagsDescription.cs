/*
public enum CommandFlags
{
	/// <summary>
	/// Команда не может быть запущена во время работы другой команды.
	/// Команда при запуске сбрасывает текущую селекцию.
	/// 
	/// Command could not be invoked while other command is in progress
	/// Command on run will clear current selection when invoked
	/// </summary>
	Modal,
	/// <summary>
	/// Команда может быть запущена во время работы другой команды.
	/// Command could be invoked while other command is in progress
	/// </summary>
	Transparent,
	/// <summary>
	/// When the pickfirst set is retrieved, it is cleared within AutoCAD.
	/// Command is able to retrieve the pickfirst set via the functions ads_ssgetfirst() or ads_ssget("I.").
	/// Command can set the pickfirst set via ads_sssetfirst(), but it only stays set until the command ends.
	/// Command cannot retrieve nor set grips.
	/// </summary>
	UsePickSet,
	/// <summary>
	/// Команда принимает на вход текущую селекцию.
	/// Command will not clear and accept current selection
	/// </summary>
	Redraw,
	/// <summary>
	/// Команду нельзя запустить в пространстве Модели.
	/// Command is not allowed to run in model space
	/// </summary>
	NoTileMode,
	/// <summary>
	/// Команду нельзя запустить в пространстве Листа.
	/// Command is not allowed to run in paper space
	/// </summary>
	NoPaperSpace,
	/// <summary>
	/// For internal use only (Command can only be invoked via the cmdGroupName.cmdGlobalName syntax.)
	/// </summary>
	Undefined = 0x00000200,
	/// <summary>
	/// For internal use only
	/// </summary>
	InProgress = 0x00000400,

	/// <summary>
	/// The command will be run in the application execution context rather than the current document context,
	/// with the different capabilities and limitations that entails. It should be used sparingly.
	/// </summary>
	Session,
	/// <summary>
	/// Позволяет во время работы команды отображать свойства в обозревателе свойств.
	/// Allows to change properties while command is in progress
	/// </summary>
	Properties,
	/// <summary>
	/// В команде есть пользовательский ввод, во время которого команду можно прервать.
	/// Command contains user input and can be interrupted
	/// </summary>
	Interruptible = Properties,
	/// <summary>
	/// На время работы команды отключается Undo, т.е. все изменения необратимы.
	/// Undo is disabled and changes of this command is irreveersible
	/// </summary>
	NoHistory,
	/// <summary>
	/// Команда без префикса (sp, ms, ...).
	/// Command is registered without application prefix
	/// </summary>
	NoPrefix,
	/// <summary>
	/// Команда доступна только в DEBUG-конфигурации.
	/// Command is only available in DEBUG
	/// </summary>
	Debug,
	/// <summary>
	/// Команда не проверяется к конфигурации, т.е. доступна всегда и везде.
	/// Command is not checked against application licensing and always available
	/// </summary>
	NoCheck,
	/// <summary>
	/// Только для nanoCAD. Команда будет регистрироваться в 2-х вариантах: с префиксом и без.
	/// in nanocad command will be registered with AND without application prefix
	/// </summary>
	Platform,
};
*/