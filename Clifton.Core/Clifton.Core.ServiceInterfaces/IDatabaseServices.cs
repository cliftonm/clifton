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

using System.Collections.Generic;
using System.Data;

using Clifton.Core.Db;
using Clifton.Core.Semantics;
using Clifton.Core.ServiceManagement;

namespace Clifton.Core.ServiceInterfaces
{
	public interface IDatabaseServices : IService
	{
		void SetConnectionString(ConnectionString connectionString);
		UserId Login(UserName username, PlainTextPassword password);
		uint GetRole(UserId id);

		bool Exists(ViewName viewName, Dictionary<string, object> parms, WhereClause where);
		int Insert(ViewName viewName, Dictionary<string, object> parms);
		T QueryScalar<T>(ViewName viewName, string fieldName, Dictionary<string, object> parms, WhereClause where);
		void Update(ViewName viewName, Dictionary<string, object> parms);
		void Delete(ViewName viewName, Dictionary<string, object> parms);
		void Delete(ViewName viewName, Dictionary<string, object> parms, WhereClause where);
		DataTable Query(ViewName viewName);
		DataTable Query(ViewName viewName, Dictionary<string, object> parms, WhereClause where);
		void FixupLookups(ViewName viewName, Dictionary<string, object> parms);
	}
}
