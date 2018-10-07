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

namespace Clifton.Core.ModelTableManagement
{
	public class DisplayFieldAttribute : Attribute { }
	public class UniqueAttribute : Attribute { }
	public class ReadOnlyAttribute : Attribute { }

    public class FormatAttribute : Attribute
    {
        public string Format { get; set; }

        public FormatAttribute(string format)
        {
            Format = format;
        }
    }

	/// <summary>
	/// Add View attribute to the data context to indicate that a Table is a view and should not be created in the database.
	/// </summary>
	public class ViewAttribute : Attribute { }			

	public class DisplayNameAttribute : Attribute
	{
		public string DisplayName { get; set; }

		public DisplayNameAttribute(string name)
			: base()
		{
			DisplayName = name;
		}
	}

    public class ActualTypeAttribute : Attribute
    {
        public string ActualTypeName { get; set; }

        public ActualTypeAttribute(string name) : base()
        {
            ActualTypeName = name;
        }
    }

    public class MaxLengthAttribute : Attribute
    {
        public int MaxLength { get; set; }

        public MaxLengthAttribute(int n) : base()
        {
            MaxLength = n;
        }
    }

    public class MappedColumnAttribute : Attribute
    {
        public string Name { get; set; }

        public MappedColumnAttribute(string name) : base()
        {
            Name = name;
        }
    }

	public class LookupAttribute : Attribute
	{
		public Type ModelType { get; set; }
		public string DisplayField { get; set; }
		public string ValueField { get; set; }

		public LookupAttribute()
			: base()
		{
			DisplayField = "Name";			// Default - we expect the implementing field to be a string in the backing model.
			ValueField = "Id";				// Default - we expect the implementing field to be an int? in the backing model.
		}
	}

    public class ForeignKeyAttribute : Attribute
    {
        public string ForeignKeyTable { get; set; }
        public string ForeignKeyColumn { get; set; }

        public ForeignKeyAttribute(string fkTable, string fkColumn)
        {
            ForeignKeyTable = fkTable;
            ForeignKeyColumn = fkColumn;
        }
    }
}
