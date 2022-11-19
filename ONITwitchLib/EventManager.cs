using System;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;

namespace ONITwitchLib;

public class EventManager
{
	// the EventManager instance from the event lib, but type erased as an object
	private readonly object eventManagerInstance;

	// delegates created to wrap various methods on the event manager without needing to use reflection
	// and Invoke every time
	private readonly Func<string, string, object> registerEventDelegate;
	private readonly Func<string, object> getEventByIdDelegate;
	private readonly Action<object, Action<string>> addListenerForEventDelegate;
	private readonly Action<object, Action<object>> removeListenerForEventDelegate;
	private readonly Action<object, object> triggerEventDelegate;

	internal EventManager(object instance)
	{
		eventManagerInstance = instance;
		var eventType = eventManagerInstance.GetType();
		var registerInfo = AccessTools.DeclaredMethod(eventType, "RegisterEvent", new[] { typeof(string), typeof(string) });
		registerEventDelegate = DelegateUtil.CreateDelegate<Func<string, string, object>>(registerInfo, eventManagerInstance);
		var getByIdInfo = AccessTools.DeclaredMethod(eventType, "GetEventByID", new[] { typeof(string) });
		getEventByIdDelegate = DelegateUtil.CreateDelegate<Func<string, object>>(getByIdInfo, eventManagerInstance);
		var addListenerForEventInfo = AccessTools.DeclaredMethod(
			eventType,
			"AddListenerForEvent",
			new[] { EventInterface.EventInfoType, typeof(Action<object>) }
		);
		addListenerForEventDelegate = DelegateUtil.CreateRuntimeTypeActionDelegate(
			addListenerForEventInfo,
			eventManagerInstance,
			EventInterface.EventInfoType,
			typeof(Action<object>)
		);
		var removeListenerForEventInfo = AccessTools.DeclaredMethod(
			eventType,
			"RemoveListenerForEvent",
			new[] { EventInterface.EventInfoType, typeof(Action<object>) }
		);
		removeListenerForEventDelegate = DelegateUtil.CreateRuntimeTypeActionDelegate(
			removeListenerForEventInfo,
			eventManagerInstance,
			EventInterface.EventInfoType,
			typeof(Action<object>)
		);
		var triggerEventInfo = AccessTools.DeclaredMethod(
			eventType,
			"TriggerEvent",
			new[] { EventInterface.EventInfoType, typeof(object) }
		);
		triggerEventDelegate = DelegateUtil.CreateRuntimeTypeActionDelegate(
			triggerEventInfo,
			eventManagerInstance,
			EventInterface.EventInfoType,
			typeof(object)
		);
	}

	[NotNull]
	[ContractAnnotation("id:null => halt")]
	public EventInfo RegisterEvent([NotNull] string id, [CanBeNull] string friendlyName = null)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new ArgumentException("ID must not be null or whitespace", nameof(id));
		}

		var output = registerEventDelegate(id, friendlyName);
		if (output.GetType() != EventInterface.EventInfoType)
		{
			throw new Exception("event register type");
		}

		return new EventInfo(output);
	}

	[CanBeNull]
	[ContractAnnotation("id:null => halt")]
	public EventInfo GetEventById([NotNull] string id)
	{
		if (string.IsNullOrWhiteSpace(id))
		{
			throw new ArgumentException("ID must not be null or whitespace", nameof(id));
		}

		var output = getEventByIdDelegate(id);
		if (output.GetType() != EventInterface.EventInfoType)
		{
			throw new Exception("event by id type");
		}

		return new EventInfo(output);
	}

	public void AddListenerForEvent([NotNull] EventInfo eventInfo, [NotNull] Action<object> listener)
	{
		addListenerForEventDelegate(eventInfo.EventInfoInstance, listener);
	}

	public void RemoveListenerForEvent([NotNull] EventInfo eventInfo, [NotNull] Action<object> listener)
	{
		removeListenerForEventDelegate(eventInfo.EventInfoInstance, listener);
	}

	public void TriggerEvent([NotNull] EventInfo eventInfo, object data)
	{
		triggerEventDelegate(eventInfo.EventInfoInstance, data);
	}
}
