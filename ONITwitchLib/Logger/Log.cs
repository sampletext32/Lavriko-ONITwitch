using System.Diagnostics;

namespace ONITwitchLib.Logger;

public static class Log
{
	/// <summary>
	/// Prints a debug message to the log if the DEBUG symbol is set.
	/// </summary>
	/// <param name="msg">The object to print.</param>
	[Conditional("DEBUG")]
	public static void Debug(object msg)
	{
		global::Debug.Log($"[DEBUG] [Twitch Integration] {msg}");
	}

	/// <summary>
	/// Prints a message at the INFO level
	/// </summary>
	/// <param name="msg">The object to print.</param>
	public static void Info(object msg)
	{
		global::Debug.Log($"[Twitch Integration] {msg}");
	}

	/// <summary>
	/// Prints a message at the WARN level
	/// </summary>
	/// <param name="msg">The object to print.</param>
	public static void Warn(object msg)
	{
		global::Debug.LogWarning($"[Twitch Integration] {msg}");
	}
}
