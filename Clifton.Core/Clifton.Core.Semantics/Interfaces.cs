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

    public class ProcessEventArgs : EventArgs
    {
        public IMembrane FromMembrane { get; protected set; }
        public IReceptor FromReceptor { get; protected set; }
        public IMembrane ToMembrane { get; protected set; }
        public IReceptor ToReceptor { get; protected set; }
        public ISemanticType SemanticType { get; protected set; }

        public ProcessEventArgs(IMembrane fromMembrane, IReceptor fromReceptor, IMembrane toMembrane, IReceptor toReceptor, ISemanticType st)
        {
            FromMembrane = fromMembrane;
            FromReceptor = fromReceptor;
            ToMembrane = toMembrane;
            ToReceptor = toReceptor;
            SemanticType = st;
        }
    }

    public interface ISemanticProcessor : IService
    {
        event EventHandler<ProcessEventArgs> Processing;

        /// <summary>
        /// This flag forces single threaded processing, particularly necessary for handling IIS HttpApplication EndRequest.
        /// </summary>
        bool ForceSingleThreaded { get; set; }

        IMembrane Surface { get; }
        IMembrane Logger { get; }

        void Stop();

        void Register<M, T>()
            where M : IMembrane, new()
            where T : IReceptor;

        void Register<M>(IReceptor receptor)
            where M : IMembrane, new();

        void Register(IMembrane membrane, IReceptor receptor);

        void Unregister<M>(IReceptor receptor)
            where M : IMembrane, new();

        void Unregister(IMembrane membrane, IReceptor receptor);

        void RegisterQualifier<T>(IReceptor receptor, Func<ISemanticQualifier, bool> qualifier) where T : ISemanticQualifier;

        void ProcessInstance<M, T>(Action<T> initializer, bool processOnCallerThread = false, int msTimeout = 0)
            where M : IMembrane, new()
            where T : ISemanticType, new();

        ProcStates ProcessInstance<M, T>(Action<T> initializer, int msTimeout)
            where M : IMembrane, new()
            where T : ISemanticType, new();

        void ProcessInstance<M, T>(bool processOnCallerThread = false)
            where M : IMembrane, new()
            where T : ISemanticType, new();

        void ProcessInstance<M, T>(T obj, bool processOnCallerThread = false, int msTimeout = 0)
            where M : IMembrane, new()
            where T : ISemanticType;

        void ProcessInstance<T>(IMembrane membrane, T obj, bool processOnCallerThread = false, int msTimeout = 0)
            where T : ISemanticType;

        void ProcessInstance<M>(ISemanticType obj, bool processOnCallerThread = false)
            where M : IMembrane, new();

        // For processing events, where we want to know the caller membrane and ST.
        // We could create these as overloads, but appending "From" to the method name makes it
        // clearer that this the intent of the caller is to provide as much information as possible
        // about the publishing of the ST onto to pub-sub bus.

        void ProcessInstanceFrom<M, T>(IMembrane fromMembrane, IReceptor fromReceptor, Action<T> initializer, bool processOnCallerThread = false)
            where M : IMembrane, new()
            where T : ISemanticType, new();

        ProcStates ProcessInstanceFrom<M, T>(IMembrane fromMembrane, IReceptor fromReceptor, Action<T> initializer, int msTimeout)
            where M : IMembrane, new()
            where T : ISemanticType, new();

        void ProcessInstanceFrom<M, T>(IMembrane fromMembrane, IReceptor fromReceptor, bool processOnCallerThread = false)
            where M : IMembrane, new()
            where T : ISemanticType, new();

        void ProcessInstanceFrom<M, T>(IMembrane fromMembrane, IReceptor fromReceptor, T obj, bool processOnCallerThread = false)
            where M : IMembrane, new()
            where T : ISemanticType;

        void ProcessInstanceFrom<T>(IMembrane fromMembrane, IReceptor fromReceptor, IMembrane membrane, T obj, bool processOnCallerThread = false)
            where T : ISemanticType;

        void ProcessInstanceFrom<M>(IMembrane fromMembrane, IReceptor fromReceptor, ISemanticType obj, bool processOnCallerThread = false)
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
