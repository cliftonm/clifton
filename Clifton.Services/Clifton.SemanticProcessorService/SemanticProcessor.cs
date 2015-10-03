using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using MoreLinq;

using Clifton.ExtensionMethods;
using Clifton.Semantics;
using Clifton.SemanticProcessorInterfaces;
using Clifton.ServiceInterfaces;

namespace Clifton.SemanticProcessorService
{
	public class ST_Exception : ISemanticType
	{
		public Exception Exception { get; protected set; }

		public ST_Exception(Exception ex)
		{
			this.Exception = ex;
		}
	}

	public abstract class ProcessCall
	{
		public IReceptor Receptor { get; set; }
		public bool AutoDispose { get; set; }
		public ISemanticType SemanticInstance { get; set; }

		public abstract void MakeCall();
	}

	public class MethodInvokeCall : ProcessCall
	{
		public MethodInfo Method { get; set; }
		public object[] Parameters { get; set; }

		public override void MakeCall()
		{
			Method.Invoke(Receptor, Parameters);
		}
	}

	public class DynamicCall : ProcessCall
	{
		public Action Proc { get; set; }

		public DynamicCall()
		{
			AutoDispose = true;
		}

		public override void MakeCall()
		{
			Proc();
		}
	}

	public struct MembraneReceptor
	{
		public IMembrane Membrane { get; set; }
		public Type ReceptorType { get; set; }
	}

	public class ReceptorInitializer
	{
		public Action<IReceptor> Initializer { get; set; }
	}

	public class SemanticProcessor : ServiceBase, ISemanticProcessor
	{
		public IMembrane Surface { get; protected set; }
		public IMembrane Logger { get; protected set; }

		public IReadOnlyList<IMembrane> Membranes { get { return membranes.Values.ToList(); } }

		protected const int MAX_WORKER_THREADS = 20;
		protected List<ThreadSemaphore<ProcessCall>> threadPool;
		protected ConcurrentDictionary<Type, IMembrane> membranes;
		protected ConcurrentDictionary<IMembrane, List<Type>> membraneReceptorTypes;
		protected ConcurrentDictionary<IMembrane, List<IReceptor>> membraneReceptorInstances;

		protected ConcurrentList<IReceptor> statefulReceptors;
		protected ConcurrentDictionary<Type, List<Type>> typeNotifiers;
		protected ConcurrentDictionary<Type, List<IReceptor>> instanceNotifiers;

		protected Dictionary<MembraneReceptor, ReceptorInitializer> receptorInitializers;

		public SemanticProcessor()
		{
			membranes = new ConcurrentDictionary<Type, IMembrane>();
			membraneReceptorTypes = new ConcurrentDictionary<IMembrane, List<Type>>();
			membraneReceptorInstances = new ConcurrentDictionary<IMembrane, List<IReceptor>>();
			receptorInitializers = new Dictionary<MembraneReceptor, ReceptorInitializer>();

			// Our two hard-coded membranes:
			Surface = RegisterMembrane<SurfaceMembrane>();
			Logger = RegisterMembrane<LoggerMembrane>();

			statefulReceptors = new ConcurrentList<IReceptor>();
			typeNotifiers = new ConcurrentDictionary<Type, List<Type>>();
			instanceNotifiers = new ConcurrentDictionary<Type, List<IReceptor>>();
			threadPool = new List<ThreadSemaphore<ProcessCall>>();

			// Register our two membranes.
			membranes[Surface.GetType()] = Surface;
			membranes[Logger.GetType()] = Logger;

			InitializePoolThreads();
		}

		public IMembrane RegisterMembrane<M>()
			where M : IMembrane, new()
		{
			IMembrane membrane;
			Type m = typeof(M);

			if (!membranes.TryGetValue(m, out membrane))
			{
				membrane = new M();
				membranes[m] = membrane;
				membraneReceptorTypes[membrane] = new List<Type>();
				membraneReceptorInstances[membrane] = new List<IReceptor>();
			}

			return membrane;
		}

		/// <summary>
		/// Add the child (second generic type) to the outer (first generic type) membrane.
		/// </summary>
		/// <typeparam name="Outer"></typeparam>
		/// <typeparam name="Inner"></typeparam>
		/// <returns></returns>
		public void AddChild<Outer, Inner>()
			where Outer : IMembrane, new()
			where Inner : IMembrane, new()
		{
			Membrane mOuter = (Membrane)RegisterMembrane<Outer>();
			Membrane mInner = (Membrane)RegisterMembrane<Inner>();
			mOuter.AddChild(mInner);
		}

		public void OutboundPermeableTo<M, T>()
			where M : IMembrane, new()
			where T : ISemanticType
		{
			Membrane m = (Membrane)RegisterMembrane<M>();
			m.OutboundPermeableTo<T>();
		}

		public void InboundPermeableTo<M, T>()
			where M : IMembrane, new()
			where T : ISemanticType
		{
			Membrane m = (Membrane)RegisterMembrane<M>();
			m.InboundPermeableTo<T>();
		}

		/// <summary>
		/// Register a receptor, auto-discovering the semantic types that it processes.
		/// Receptors live in membranes, to we always specify the membrane type.  The membrane
		/// instance is auto-created for us if necessary.
		/// </summary>
		public void Register<M, T>()
			where M : IMembrane, new()
			where T : IReceptor
		{
			Register<T>();
			IMembrane membrane = RegisterMembrane<M>();
			membraneReceptorTypes[membrane].Add(typeof(T));
		}

		public void Register<M, T>(Action<IReceptor> receptorInitializer)
			where M : IMembrane, new()
			where T : IReceptor
		{
			Register<T>();
			Type receptorType = typeof(T);
			IMembrane membrane = RegisterMembrane<M>();
			membraneReceptorTypes[membrane].Add(receptorType);
			receptorInitializers[new MembraneReceptor() { Membrane = membrane, ReceptorType = receptorType }] = new ReceptorInitializer() { Initializer = receptorInitializer };
		}

		/// <summary>
		/// Register an instance receptor living in a membrane type.
		/// </summary>
		public void Register<M>(IReceptor receptor)
			where M : IMembrane, new()
		{
			IMembrane membrane = RegisterMembrane<M>();
			Register(membrane, receptor);
		}

		/// <summary>
		/// Register a stateful receptor contained within the specified membrane.
		/// </summary>
		public void Register(IMembrane membrane, IReceptor receptor)
		{
			statefulReceptors.Add(receptor);
			Type ttarget = receptor.GetType();

			MethodInfo[] methods = ttarget.GetMethods();

			foreach (MethodInfo method in methods)
			{
				// TODO: Use attribute, not specific function name.
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();
					InstanceNotify(receptor, parameters[2].ParameterType);
				}
			}

			membranes[membrane.GetType()] = membrane;
			membraneReceptorInstances[membrane].Add(receptor);
		}

		/// <summary>
		/// Remove a semantic (source) type from a target (receptor).
		/// The target type will no longer receive notifications of source type instances.
		/// </summary>
		public void RemoveTypeNotify<TMembrane, TTarget, TSource>()
			where TMembrane : IMembrane
			where TTarget : IReceptor
			where TSource : ISemanticType
		{
			Type tsource = typeof(TSource);
			IMembrane membrane = membranes[typeof(TMembrane)];
			List<Type> targets = GetReceptors(membrane, tsource);

			// Remove from type list.
			foreach (Type ttarget in targets)
			{
				typeNotifiers[tsource].Remove(ttarget);
			}

			// Remove all receptors in the specified membrane that are processing this semantic type.
			List<IReceptor> instanceReceptors = GetStatefulReceptors(membrane, tsource);

			foreach (IReceptor receptor in instanceReceptors)
			{
				instanceNotifiers[tsource].Remove(receptor);
			}
		}

		public void RemoveTypeNotify<TMembrane, TSource>(IReceptor receptor)
			where TMembrane : IMembrane
			where TSource : ISemanticType
		{
			Type tsource = typeof(TSource);
			Type treceptor = receptor.GetType();
			IMembrane membrane = membranes[typeof(TMembrane)];
			List<Type> targets = GetReceptors(membrane, tsource);

			// Remove from type list for this membrane this receptor, by its type.
			foreach (Type ttarget in targets)
			{
				typeNotifiers[tsource].Remove(ttarget);
			}

			// Remove from instance list.
			List<IReceptor> instanceReceptors = GetStatefulReceptors(membrane, tsource);

			foreach (IReceptor ireceptor in instanceReceptors)
			{
				// Remove only this instance receptor from its membrane.
				if (ireceptor == receptor)
				{
					instanceNotifiers[tsource].Remove(receptor);
				}
			}
		}
		
		/// <summary>
		/// Process a semantic type, allowing the caller to specify an initializer before processing the instance.
		/// </summary>
		public void ProcessInstance<M, T>(Action<T> initializer, bool processOnCallerThread = false)
			where M : IMembrane, new()
			where T : ISemanticType, new()
		{
			T inst = new T();
			initializer.IfNotNull(i => i(inst));
			ProcessInstance<M, T>(inst, processOnCallerThread);
		}

		public void ProcessInstance<M, T>(bool processOnCallerThread = false)
			where M : IMembrane, new()
			where T : ISemanticType, new()
		{
			T inst = new T();
			ProcessInstance<M, T>(inst, processOnCallerThread);
		}

		/// <summary>
		/// Process an instance in a given membrane type.
		/// </summary>
		public void ProcessInstance<M, T>(T obj, bool processOnCallerThread = false)
			where M : IMembrane, new()
			where T : ISemanticType
		{
			Type mtype = typeof(M);
			IMembrane membrane = RegisterMembrane<M>();
			ProcessInstance(membrane, obj, processOnCallerThread);
		}

		/// <summary>
		/// Process an instance of a specific type immediately.  The type T is determined implicitly from the parameter type, so 
		/// a call can look like: ProcessInstance(t1).  This also allows the code here to use the "dynamic" keyword rather than 
		/// having to obtain the method to call by reflection.
		/// </summary>
		public void ProcessInstance<T>(IMembrane membrane, T obj, bool processOnCallerThread = false)
			where T : ISemanticType
		{
			ProcessInstance(membrane, null, obj, processOnCallerThread);
		}

		protected void ProcessInstance<T>(IMembrane membrane, IMembrane caller, T obj, bool processOnCallerThread)
			where T : ISemanticType
		{
			// ProcessInstance((ISemanticType)obj);

			// We get the source object type.
			Type tsource = obj.GetType();

			// Then, for each target type that is interested in this source type, 
			// we construct the target type, then invoke the correct target's Process method.
			// Constructing the target type provides us with some really interesting abilities.
			// The target type can be treated as an immutable object.  We can, for instance, exceute
			// the Process call on a separate thread.  Constructing the target type ensures that the
			// target is stateless -- state must be managed external of any type!

			// Stateless receptors:

			List<Type> receptors = GetReceptors(membrane, tsource);
			Log(membrane, obj);

			foreach (Type ttarget in receptors)
			{
				// We can use dynamic here because we have a <T> generic to resolve the call parameter.
				// If we instead only have the interface ISemanticType, dynamic does not downcast to the concrete type --
				// therefore it can't locate the call point because it implements the concrete type.
				dynamic target = Activator.CreateInstance(ttarget);

				ReceptorInitializer receptorInitializer;

				if (receptorInitializers.TryGetValue(new MembraneReceptor() { Membrane = membrane, ReceptorType = ttarget }, out receptorInitializer))
				{
					receptorInitializer.Initializer(target);
				}

				// Call immediately?
				if (processOnCallerThread)
				{
					Call(new DynamicCall() { SemanticInstance = obj, Receptor = target, Proc = () => target.Process(this, membrane, obj) });
				}
				else
				{
					// Pick a thread that has the least work to do.
					threadPool.MinBy(tp => tp.Count).Enqueue(new DynamicCall() { SemanticInstance = obj, Receptor = target, Proc = () => target.Process(this, membrane, obj) });
				}
			}

			// Also check stateful receptors
			List<IReceptor> sreceptors = GetStatefulReceptors(membrane, tsource);

			foreach (IReceptor receptor in sreceptors)
			{
				dynamic target = receptor;
				// Call immediately?
				if (processOnCallerThread)
				{
					Call(new DynamicCall() { SemanticInstance = obj, Receptor = target, Proc = () => target.Process(this, membrane, obj), AutoDispose = false });
				}
				else
				{
					threadPool.MinBy(tp => tp.Count).Enqueue(new DynamicCall() { SemanticInstance = obj, Receptor = target, Proc = () => target.Process(this, membrane, obj), AutoDispose = false });
				}
			}

			ProcessInnerTypes(membrane, caller, obj, processOnCallerThread);
			PermeateOut(membrane, caller, obj, processOnCallerThread);
		}

		public void ProcessInstance<M>(ISemanticType obj, bool processOnCallerThread = false)
			where M : IMembrane, new()
		{
			IMembrane m = RegisterMembrane<M>();
			ProcessInstance(m, null, obj, processOnCallerThread);
		}

		/// <summary>
		/// Traverse permeable membranes without calling back into the caller.  While membranes should not be bidirectionally
		/// permeable, this does stop infinite recursion if the user accidentally (or intentionally) configured the membranes thusly.
		/// </summary>
		protected void PermeateOut<T>(IMembrane membrane, IMembrane caller, T obj, bool processOnCallerThread)
			where T : ISemanticType
		{
			List<IMembrane> pmembranes = ((Membrane)membrane).PermeateTo(obj);
			pmembranes.Where(m=>m != caller).ForEach((m) => ProcessInstance(m, membrane, obj, processOnCallerThread));
		}

		/// <summary>
		/// Traverse permeable membranes without calling back into the caller.  While membranes should not be bidirectionally
		/// permeable, this does stop infinite recursion if the user accidentally (or intentionally) configured the membranes thusly.
		/// </summary>
		protected void PermeateOut(IMembrane membrane, IMembrane caller, ISemanticType obj, bool processOnCallerThread)
		{
			List<IMembrane> pmembranes = ((Membrane)membrane).PermeateTo(obj);
			pmembranes.Where(m => m != caller).ForEach((m) => ProcessInstance(m, membrane, obj, processOnCallerThread));
		}

		/// <summary>
		/// Process an instance where we only know that it implements ISemanticType as opposed the the concrete type in the generic method above.
		/// We cannot use "dynamic" in this case, therefore we have to use Method.Invoke.
		/// </summary>
		protected void ProcessInstance(IMembrane membrane, IMembrane caller, ISemanticType obj, bool processOnCallerThread = false)
		{
			// We get the source object type.
			Type tsource = obj.GetType();

			// Stateless receptors:

			List<Type> receptors = GetReceptors(membrane, tsource);
			Log(membrane, obj);

			foreach (Type ttarget in receptors)
			{
				// We can use dynamic here because we have a <T> generic to resolve the call parameter.
				// If we instead only have the interface ISemanticType, dynamic does not downcast to the concrete type --
				// therefore it can't locate the call point because it implements the concrete type.
				IReceptor target = (IReceptor)Activator.CreateInstance(ttarget);

				ReceptorInitializer receptorInitializer;

				if (receptorInitializers.TryGetValue(new MembraneReceptor() { Membrane = membrane, ReceptorType = ttarget }, out receptorInitializer))
				{
					receptorInitializer.Initializer(target);
				}

				// Call immediately?
				MethodInfo method = GetProcessMethod(target, tsource);

				if (processOnCallerThread)
				{
					method.Invoke(target, new object[] { this, membrane, obj });
				}
				else
				{
					// Pick a thread that has the least work to do.
					threadPool.MinBy(tp => tp.Count).Enqueue(new MethodInvokeCall() { Method = method, SemanticInstance = obj, Receptor = target, Parameters = new object[] { this, membrane, obj } });
				}
			}

			// Also check stateful receptors
			List<IReceptor> sreceptors = GetStatefulReceptors(membrane, tsource);

			foreach (IReceptor receptor in sreceptors)
			{
				MethodInfo method = GetProcessMethod(receptor, tsource);

				// Call immediately?
				if (processOnCallerThread)
				{
					method.Invoke(receptor, new object[] { this, membrane, obj });
				}
				else
				{
					threadPool.MinBy(tp => tp.Count).Enqueue(new MethodInvokeCall() { Method = method, SemanticInstance = obj, Receptor = receptor, Parameters = new object[] { this, membrane, obj }, AutoDispose = false });
				}
			}

			ProcessInnerTypes(membrane, caller, obj, processOnCallerThread);
			PermeateOut(membrane, caller, obj, processOnCallerThread);
		}

		/// <summary>
		/// Any public properties that are of ISemanticType type and not null are also emitted into the membrane.
		/// </summary>
		protected void ProcessInnerTypes(IMembrane membrane, IMembrane caller, ISemanticType obj, bool processOnCallerThread)
		{
			var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(pi => pi.PropertyType.GetInterfaces().Contains(typeof(ISemanticType)));

			properties.ForEach(pi =>
				{
					ISemanticType prop = (ISemanticType)pi.GetValue(obj);

					if (prop != null)
					{
						ProcessInstance(membrane, caller, prop, processOnCallerThread);
					}
				});
		}

		protected void Register<T>()
			where T : IReceptor
		{
			Type ttarget = typeof(T);
			MethodInfo[] methods = ttarget.GetMethods();

			foreach (MethodInfo method in methods)
			{
				// TODO: Use attribute, not specific function name.
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();

					// Semantic types are always the third parameter
					// Types can either be concrete or interfaces.
					TypeNotify<T>(parameters[2].ParameterType);
				}
			}
		}

		/// <summary>
		/// Add a type notifier for a source type.  The source type can be either a concrete class type or an interface type.
		/// As a result, the list of targets will, in the dictionary, be distinct.  This is also the case for derived types.
		/// </summary>
		protected void TypeNotify<TTarget>(Type tsource)
			where TTarget : IReceptor
		{
			// The source type is the key, containing a list of target types that get notified of source type instances.
			List<Type> targets;

			if (!typeNotifiers.TryGetValue(tsource, out targets))
			{
				targets = new List<Type>();
				typeNotifiers[tsource] = targets;
			}

			targets.Add(typeof(TTarget));
		}

		protected void InstanceNotify(IReceptor receptor, Type tsource)
		{
			List<IReceptor> targets;

			if (!instanceNotifiers.TryGetValue(tsource, out targets))
			{
				targets = new List<IReceptor>();
				instanceNotifiers[tsource] = targets;
			}

			targets.Add(receptor);
		}

		protected List<Type> GetReceptors(IMembrane membrane, Type tsource)
		{
			List<Type> receptors = new List<Type>();
			List<Type> baseList;

			// Get the type notifiers for the provided type.
			if (typeNotifiers.TryGetValue(tsource, out baseList))
			{
				// Add only receptors that are in the membrane for the semantic instance being processed.
				// TODO: This is where we could put in the rules for moving up/down the membrane hierarchy.
				receptors.AddRange(membraneReceptorTypes[membrane].Where(t => baseList.Contains(t)));
			}
			
			// Check interfaces and base types of the source type as well to see if there are receptors handling the interfaces.

			foreach (Type t in tsource.GetParentTypes())
			{
				List<Type> tReceptors;

				if (typeNotifiers.TryGetValue(t, out tReceptors))
				{
					receptors.AddRange(membraneReceptorTypes[membrane].Where(tr => tReceptors.Contains(tr)));
				}
			}

			return receptors;
		}

		protected List<IReceptor> GetStatefulReceptors(IMembrane membrane, Type tsource)
		{
			List<IReceptor> receptors = new List<IReceptor>();
			List<IReceptor> baseList;

			if (instanceNotifiers.TryGetValue(tsource, out baseList))
			{
				// Add only receptors that are in the membrane for the semantic instance being processed.
				// TODO: This is where we could put in the rules for moving up/down the membrane hierarchy.
				receptors.AddRange(membraneReceptorInstances[membrane].Where(t => baseList.Contains(t)));
			}

			// Check interfaces and base types of the source type as well to see if there are receptors handling the interfaces.

			foreach (Type t in tsource.GetParentTypes())
			{
				List<IReceptor> tReceptors;

				if (instanceNotifiers.TryGetValue(t, out tReceptors))
				{
					receptors.AddRange(membraneReceptorInstances[membrane].Where(tr => tReceptors.Contains(tr)));
				}
			}

			return receptors;
		}

		/// <summary>
		/// Setup thread pool to for calling receptors to process semantic types.
		/// Why do we use our own thread pool?  Because .NET's implementation (and
		/// particularly Task) is crippled and non-functional for long running threads.
		/// </summary>
		protected void InitializePoolThreads()
		{
			for (int i = 0; i < MAX_WORKER_THREADS; i++)
			{
				Thread thread = new Thread(new ParameterizedThreadStart(ProcessPoolItem));
				thread.IsBackground = true;
				ThreadSemaphore<ProcessCall> ts = new ThreadSemaphore<ProcessCall>();
				threadPool.Add(ts);
				thread.Start(ts);
			}
		}

		/// <summary>
		/// Invoke the action that we want to run on a thread.
		/// </summary>
		protected void ProcessPoolItem(object state)
		{
			ThreadSemaphore<ProcessCall> ts = (ThreadSemaphore<ProcessCall>)state;

			while (true)
			{
				ts.WaitOne();
				ProcessCall rc;

				if (ts.TryDequeue(out rc))
				{
					Call(rc);
				}
			}
		}

		protected void Call(ProcessCall rc)
		{
			try
			{
				rc.MakeCall();
			}
			catch (Exception ex)
			{
				Exception ex2 = ex;

				while (ex2.InnerException != null)
				{
					ex2 = ex2.InnerException;
				}

				// System.Diagnostics.Debug.WriteLine(ex.Message + "\r\n" + ex2.StackTrace);
				// Prevent recursion if the exception process itself throws an exception.
				if (!(rc.SemanticInstance is ST_Exception))
				{
					ProcessInstance(Logger, new ST_Exception(ex2), true);
				}
			}
			finally
			{
				if ( (rc.Receptor is IDisposable) && (rc.AutoDispose) )
				{
					((IDisposable)rc.Receptor).Dispose();
				}
			}
		}

		/// <summary>
		/// Get the Process method that implements, in its parameters, the source type.
		/// Only one process method is allowed for a specific type -- the compiler would tell us if there's duplicates.
		/// However, we can have different process methods for interfaces and base classes of a given type, as these
		/// each are maintained in unique receptor target lists, since they are, technically, different types!
		/// </summary>
		protected MethodInfo GetProcessMethod(IReceptor target, Type tsource)
		{
			// TODO: Cache the (target type, source type) MethodInfo
			MethodInfo[] methods = target.GetType().GetMethods();

			// Also check interfaces implemented by the source.
			Type[] interfaces = tsource.GetInterfaces();

			foreach (MethodInfo method in methods)
			{
				if (method.Name == "Process")
				{
					ParameterInfo[] parameters = method.GetParameters();

					foreach (ParameterInfo parameter in parameters)
					{
						// Do we have a match for the concrete source type?
						if (parameter.ParameterType == tsource)
						{
							return method;
						}

						// Do we have a match for any interfaces the concrete source type implements?
						foreach (Type iface in interfaces)
						{
							if (parameter.ParameterType == iface)
							{
								return method;
							}
						}
					}
				}
			}

			return null;
		}

		protected void Log<T>(IMembrane membrane, T obj)
			where T : ISemanticType
		{
			// Prevent recursion, don't log exceptions, as these get handled by the exception type.
			if ( (!(membrane is LoggerMembrane)) && (!(obj is ST_Exception)) )
			{
				ProcessInstance(Logger, obj);
			}
		}
	}
}
