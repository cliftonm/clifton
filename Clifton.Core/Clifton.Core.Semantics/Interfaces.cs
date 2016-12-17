/* The MIT License (MIT)
* 
* Copyright (c) 2015 Marc Clifton
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/

using System;

using Clifton.Core.ServiceManagement;

namespace Clifton.Core.Semantics
{
	public enum ProcStates
	{
		NotProcessed = 0,
		OK = 1,
		Exception = 2,
		Timeout = 4,
	}

	public interface ISemanticProcessor : IService
	{
		IMembrane Surface { get; }
		IMembrane Logger { get; }

		void Register<M, T>()
			where M : IMembrane, new()
			where T : IReceptor;

		void Register<M>(IReceptor receptor)
			where M : IMembrane, new();

		void Register(IMembrane membrane, IReceptor receptor);

        void RegisterQualifier<T>(IReceptor receptor, Func<ISemanticQualifier, bool> qualifier) where T : ISemanticQualifier;

        void ProcessInstance<M, T>(Action<T> initializer, bool processOnCallerThread = false)
			where M : IMembrane, new()
			where T : ISemanticType, new();

		ProcStates ProcessInstance<M, T>(Action<T> initializer, int msTimeout)
			where M : IMembrane, new()
			where T : ISemanticType, new();

		void ProcessInstance<M, T>(bool processOnCallerThread = false)
			where M : IMembrane, new()
			where T : ISemanticType, new();

		void ProcessInstance<M, T>(T obj, bool processOnCallerThread = false)
			where M : IMembrane, new()
			where T : ISemanticType;

		void ProcessInstance<T>(IMembrane membrane, T obj, bool processOnCallerThread = false)
			where T : ISemanticType;

		void ProcessInstance<M>(ISemanticType obj, bool processOnCallerThread = false)
			where M : IMembrane, new();
	}

	public interface IMembrane
	{
		// Do not any members here.  This is an interface helper.
	}

	public interface IReceptor
	{
		// Do not any members here.  This is an interface helper.
	}

	public interface IReceptor<T> : IReceptor
	{
		void Process(ISemanticProcessor pool, IMembrane membrane, T obj);
	}
}
